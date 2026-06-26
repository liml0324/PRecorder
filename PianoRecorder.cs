using NAudio.Wave;

namespace PRecorder;

/// <summary>录音器状态快照（线程安全读取）</summary>
public readonly record struct RecorderStatus(
    bool IsRecording,
    TimeSpan RecordingDuration,
    TimeSpan BufferDuration,
    int BufferFillPercent
);

/// <summary>
/// 电钢琴预录音核心类：基于环形缓冲区的后台录音器。
/// 持续采集音频到固定大小环形缓冲区，支持随时导出最近 N 分钟的 WAV 文件。
/// </summary>
class PianoRecorder : IDisposable
{
    // ---- 音频参数 ----
    private const int SampleRate = 44100;
    private const int BitsPerSample = 16;
    private const int Channels = 2;

    // ---- 内部状态 ----
    private WaveInEvent? _waveIn;
    private byte[]? _circularBuffer;      // 固定大小的环形缓冲区
    private byte[]? _saveBuffer;          // 预分配导出缓冲区（与环形区等大，避免 Save 时在锁内分配内存）
    private int _writePos;                // 下一次写入的起始位置
    private long _totalBytesWritten;      // 启动以来累计写入字节数（用于判断是否回绕）
    private int _bufferSize;              // 缓冲区总大小（字节）
    private int _bufferDurationSeconds;   // 可配置的缓冲区时长（秒）
    private WaveFormat? _recordingFormat;
    private readonly object _bufferLock = new();
    private bool _disposed;

    /// <summary>启动录音</summary>
    /// <param name="deviceId">音频输入设备 ID</param>
    /// <param name="bufferDurationSeconds">循环缓冲区时长（秒），默认 300（5 分钟）</param>
    public void StartRecording(int deviceId, int bufferDurationSeconds = 300)
    {
        _bufferDurationSeconds = bufferDurationSeconds;
        _recordingFormat = new WaveFormat(SampleRate, BitsPerSample, Channels);
        _bufferSize = _recordingFormat.AverageBytesPerSecond * _bufferDurationSeconds;
        _circularBuffer = new byte[_bufferSize];
        _saveBuffer = new byte[_bufferSize];   // 预分配导出缓冲，避免 Save 时在锁内分配大数组
        _writePos = 0;
        _totalBytesWritten = 0;

        _waveIn = new WaveInEvent
        {
            DeviceNumber = deviceId,
            WaveFormat = _recordingFormat,
            BufferMilliseconds = 100
        };

        _waveIn.DataAvailable += OnDataAvailable;
        _waveIn.StartRecording();
    }

    /// <summary>音频数据到达回调 —— 写入环形缓冲区</summary>
    private void OnDataAvailable(object? sender, WaveInEventArgs e)
    {
        lock (_bufferLock)
        {
            if (_circularBuffer == null || _disposed)
                return;

            try
            {
                int bytesToWrite = e.BytesRecorded;

                // 极端情况：单次数据块 ≥ 整个缓冲区
                if (bytesToWrite >= _bufferSize)
                {
                    int offset = bytesToWrite - _bufferSize;
                    Array.Copy(e.Buffer, offset, _circularBuffer, 0, _bufferSize);
                    _writePos = 0;
                    _totalBytesWritten = _bufferSize;
                    return;
                }

                int spaceToEnd = _bufferSize - _writePos;

                if (bytesToWrite <= spaceToEnd)
                {
                    // 无需回绕，直接拷贝
                    Array.Copy(e.Buffer, 0, _circularBuffer, _writePos, bytesToWrite);
                }
                else
                {
                    // 环形回绕：分两段拷贝
                    Array.Copy(e.Buffer, 0, _circularBuffer, _writePos, spaceToEnd);
                    Array.Copy(e.Buffer, spaceToEnd, _circularBuffer, 0, bytesToWrite - spaceToEnd);
                }

                _writePos = (_writePos + bytesToWrite) % _bufferSize;
                _totalBytesWritten += bytesToWrite;
            }
            catch (Exception ex)
            {
                // 回调内的异常不会中断程序，但应输出以便排查
                Console.WriteLine($"[录音警告] 数据处理异常: {ex.Message}");
            }
        }
    }

    /// <summary>线程安全地获取录音器当前状态</summary>
    public RecorderStatus GetStatus()
    {
        lock (_bufferLock)
        {
            if (_recordingFormat == null)
                return new RecorderStatus(false, TimeSpan.Zero, TimeSpan.Zero, 0);

            double bytesPerSecond = _recordingFormat.AverageBytesPerSecond;
            int validBytes = (int)Math.Min(_totalBytesWritten, _bufferSize);

            return new RecorderStatus(
                IsRecording: _waveIn != null && !_disposed,
                RecordingDuration: TimeSpan.FromSeconds(_totalBytesWritten / bytesPerSecond),
                BufferDuration: TimeSpan.FromSeconds(validBytes / bytesPerSecond),
                BufferFillPercent: (int)((double)validBytes / _bufferSize * 100)
            );
        }
    }

    /// <summary>将缓冲区中的音频保存为 WAV 文件（锁内零分配，磁盘 I/O 在锁外执行）</summary>
    public void SaveHistory(string filePath)
    {
        WaveFormat? format;
        int validBytes;
        byte[] saveBuffer;

        // 第一步：在锁内将环形数据线性化拷贝到预分配的 _saveBuffer（只有 Array.Copy，零内存分配）
        lock (_bufferLock)
        {
            if (_circularBuffer == null || _saveBuffer == null || _totalBytesWritten == 0 || _recordingFormat == null)
            {
                Console.WriteLine("保存失败：缓冲区内目前没有音频数据！");
                return;
            }

            validBytes = (int)Math.Min(_totalBytesWritten, _bufferSize);
            format = _recordingFormat;
            saveBuffer = _saveBuffer;  // 本地引用，防止 Stop() 并发置空

            if (_totalBytesWritten <= _bufferSize)
            {
                // 尚未回绕：数据从位置 0 开始连续存放
                Array.Copy(_circularBuffer, 0, saveBuffer, 0, validBytes);
            }
            else
            {
                // 已回绕：旧数据在 [_writePos .. 末尾)，新数据在 [0 .. _writePos)
                int firstPart = _bufferSize - _writePos;
                Array.Copy(_circularBuffer, _writePos, saveBuffer, 0, firstPart);
                Array.Copy(_circularBuffer, 0, saveBuffer, firstPart, _writePos);
            }
        }

        // 第二步：在锁外执行磁盘写入，使用本地引用的 saveBuffer，不受 Stop() 影响
        try
        {
            using (var writer = new WaveFileWriter(filePath, format))
            {
                writer.Write(saveBuffer, 0, validBytes);
            }
            Console.WriteLine($"【成功】音频已保存至: {filePath}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"【失败】保存音频时出错: {ex.Message}");
        }
    }

    /// <summary>停止录音并释放底层资源</summary>
    public void Stop()
    {
        if (_waveIn != null)
        {
            _waveIn.DataAvailable -= OnDataAvailable;   // 取消事件订阅，防止泄漏
            _waveIn.StopRecording();
            _waveIn.Dispose();
            _waveIn = null;
        }

        lock (_bufferLock)
        {
            _circularBuffer = null;
        }
    }

    /// <summary>IDisposable 实现，确保资源被释放</summary>
    public void Dispose()
    {
        if (!_disposed)
        {
            Stop();
            _disposed = true;
        }
    }
}
