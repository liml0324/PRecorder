using NAudio.Wave;
using System.Diagnostics;
using System.Windows.Threading;

namespace PRecording;

public partial class MainWindow : System.Windows.Window
{
    private PianoRecorder? _recorder;
    private DispatcherTimer? _statusTimer;
    private bool _isExiting;

    // 保存设置
    private string _savePath = ""; // 在 Window_Loaded 中初始化为桌面路径
    private bool _ffmpegAvailable;

    // 支持的导出格式
    private static readonly (string Tag, string Label)[] _formats =
    [
        ("wav",  "WAV (无损)"),
        ("mp3",  "MP3 (320 kbps)"),
        ("flac", "FLAC (无损压缩)"),
        ("aac",  "AAC (256 kbps)"),
        ("ogg",  "OGG Vorbis"),
    ];

    public MainWindow()
    {
        InitializeComponent();
    }

    /// <summary>窗口加载：初始化设备列表、保存设置和状态刷新定时器</summary>
    private void Window_Loaded(object sender, System.Windows.RoutedEventArgs e)
    {
        RefreshDeviceList();

        // 保存路径默认为桌面
        _savePath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
        txtSavePath.Text = _savePath;

        // 检测 ffmpeg 是否可用
        CheckFfmpeg();
        PopulateFormatList();

        // 每 500ms 刷新一次状态显示（DispatcherTimer 在 UI 线程触发，无需 Invoke）
        _statusTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(500)
        };
        _statusTimer.Tick += StatusTimer_Tick;
        _statusTimer.Start();
    }

    /// <summary>窗口关闭 → 隐藏到托盘，而非退出</summary>
    private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
    {
        if (!_isExiting)
        {
            e.Cancel = true;
            Hide();
        }
    }

    // ==================== FFmpeg 检测 ====================

    /// <summary>检测系统是否安装了 ffmpeg（通过执行 ffmpeg -version）</summary>
    private void CheckFfmpeg()
    {
        try
        {
            using var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "ffmpeg",
                    Arguments = "-version",
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                }
            };
            process.Start();
            process.WaitForExit(3000);
            _ffmpegAvailable = process.ExitCode == 0;
        }
        catch
        {
            _ffmpegAvailable = false;
        }
    }

    /// <summary>根据 ffmpeg 是否可用来填充格式下拉框</summary>
    private void PopulateFormatList()
    {
        cmbFormat.Items.Clear();
        foreach (var (tag, label) in _formats)
        {
            var item = new System.Windows.Controls.ComboBoxItem
            {
                Tag = tag,
                Content = tag == "wav"
                    ? label
                    : _ffmpegAvailable ? label : $"{label} (FFmpeg 未安装)",
                IsEnabled = tag == "wav" || _ffmpegAvailable
            };
            cmbFormat.Items.Add(item);
        }
        cmbFormat.SelectedIndex = 0;
    }

    // ==================== 保存路径选择 ====================

    private void BtnBrowsePath_Click(object sender, System.Windows.RoutedEventArgs e)
    {
        using var dialog = new System.Windows.Forms.FolderBrowserDialog
        {
            Description = "选择音频文件保存目录",
            SelectedPath = _savePath,
            ShowNewFolderButton = true
        };

        if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
        {
            _savePath = dialog.SelectedPath;
            txtSavePath.Text = _savePath;
        }
    }

    // ==================== 设备管理 ====================

    private void RefreshDeviceList()
    {
        cmbDevice.Items.Clear();

        for (int i = 0; i < WaveInEvent.DeviceCount; i++)
        {
            var cap = WaveInEvent.GetCapabilities(i);
            cmbDevice.Items.Add($"[{i}] {cap.ProductName}");
        }

        if (cmbDevice.Items.Count > 0)
            cmbDevice.SelectedIndex = 0;
    }

    private void BtnRefreshDevices_Click(object sender, System.Windows.RoutedEventArgs e)
    {
        RefreshDeviceList();
    }

    // ==================== 录音控制 ====================

    private void BtnRecord_Click(object sender, System.Windows.RoutedEventArgs e)
    {
        if (_recorder == null)
        {
            // 开始录音
            int deviceId = cmbDevice.SelectedIndex;
            if (deviceId < 0)
            {
                System.Windows.MessageBox.Show("请先选择输入设备。", "提示",
                    System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
                return;
            }

            _recorder = new PianoRecorder();
            try
            {
                _recorder.StartRecording(deviceId);
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"启动录音失败:\n{ex.Message}", "错误",
                    System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                _recorder.Dispose();
                _recorder = null;
                return;
            }

            btnRecord.Content = "⏹  停止录音";
            cmbDevice.IsEnabled = false;
            btnRefreshDevices.IsEnabled = false;
            btnSave.IsEnabled = true;
            SetStatus("录音中", System.Windows.Media.Brushes.Crimson);
        }
        else
        {
            // 停止录音
            _recorder.Stop();
            _recorder.Dispose();
            _recorder = null;

            btnRecord.Content = "▶  开始录音";
            cmbDevice.IsEnabled = true;
            btnRefreshDevices.IsEnabled = true;
            btnSave.IsEnabled = false;
            SetStatus("已停止", System.Windows.Media.Brushes.Gray);
            txtDuration.Text = "已录制: --:--:--";
            txtBuffer.Text = "缓冲区: 空";
            bufferBar.Value = 0;
            txtBufferPct.Text = "0%";
        }
    }

    // ==================== 保存音频 ====================

    /// <summary>供托盘菜单调用的公开保存方法</summary>
    public void SaveRecording()
    {
        if (_recorder == null)
        {
            System.Windows.MessageBox.Show("当前没有在录音。", "提示",
                System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
            return;
        }

        // 获取当前选中的格式
        var selectedItem = cmbFormat.SelectedItem as System.Windows.Controls.ComboBoxItem;
        string ext = selectedItem?.Tag?.ToString() ?? "wav";

        if (ext != "wav" && !_ffmpegAvailable)
        {
            System.Windows.MessageBox.Show(
                "未检测到 FFmpeg，无法保存为非 WAV 格式。\n\n" +
                "请安装 FFmpeg 后重试，或选择 WAV 格式保存。\n" +
                "下载地址: https://ffmpeg.org/download.html",
                "FFmpeg 不可用",
                System.Windows.MessageBoxButton.OK,
                System.Windows.MessageBoxImage.Warning);
            return;
        }

        // 确保保存目录存在
        try { System.IO.Directory.CreateDirectory(_savePath); } catch { }

        string fileName = $"Piano_{DateTime.Now:yyyyMMdd_HHmmss}.{ext}";
        string fullPath = System.IO.Path.Combine(_savePath, fileName);

        if (ext == "wav")
        {
            // WAV 格式：直接保存
            _recorder.SaveHistory(fullPath);
        }
        else
        {
            // 其他格式：先保存为临时 WAV，再用 ffmpeg 转码
            string tempWav = System.IO.Path.Combine(
                System.IO.Path.GetTempPath(),
                $"~prec_{Guid.NewGuid():N}.wav");

            _recorder.SaveHistory(tempWav);

            try
            {
                ConvertWithFfmpeg(tempWav, fullPath, ext);
            }
            finally
            {
                // 清理临时 WAV 文件
                try { System.IO.File.Delete(tempWav); } catch { }
            }
        }

        txtLastSave.Text = $"上次保存: {fileName}";
    }

    /// <summary>使用 ffmpeg 将 WAV 转为目标格式</summary>
    private void ConvertWithFfmpeg(string inputWav, string outputPath, string format)
    {
        string codecArgs = format switch
        {
            "mp3"  => "-codec:a libmp3lame -b:a 320k",
            "flac" => "-codec:a flac",
            "aac"  => "-codec:a aac -b:a 256k",
            "ogg"  => "-codec:a libvorbis -q:a 6",
            _      => throw new ArgumentException($"不支持的格式: {format}")
        };

        using var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "ffmpeg",
                Arguments = $"-y -i \"{inputWav}\" {codecArgs} \"{outputPath}\"",
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardError = true
            }
        };

        process.Start();
        process.WaitForExit(15000); // 最多等待 15 秒

        if (process.ExitCode != 0)
        {
            string error = process.StandardError.ReadToEnd();
            throw new Exception($"FFmpeg 退出码 {process.ExitCode}:\n{error}");
        }
    }

    private void BtnSave_Click(object sender, System.Windows.RoutedEventArgs e)
    {
        SaveRecording();
    }

    // ==================== 退出 ====================

    private void BtnExit_Click(object sender, System.Windows.RoutedEventArgs e)
    {
        _isExiting = true;
        Cleanup();
        // 通过 App 退出（会清理托盘图标）
        (System.Windows.Application.Current as App)?.ExitApplication();
    }

    /// <summary>释放录音资源</summary>
    public void Cleanup()
    {
        _statusTimer?.Stop();
        _recorder?.Stop();
        _recorder?.Dispose();
        _recorder = null;
    }

    // ==================== 状态刷新 ====================

    private void StatusTimer_Tick(object? sender, EventArgs e)
    {
        if (_recorder == null) return;

        var status = _recorder.GetStatus();

        txtDuration.Text = $"已录制: {status.RecordingDuration:hh\\:mm\\:ss}";

        if (status.BufferDuration > TimeSpan.Zero)
        {
            txtBuffer.Text = $"缓冲区: {status.BufferDuration:mm\\:ss} (最近 5 分钟)";
        }
        else
        {
            txtBuffer.Text = $"缓冲区: {status.BufferDuration:mm\\:ss}";
        }

        bufferBar.Value = status.BufferFillPercent;
        txtBufferPct.Text = $"{status.BufferFillPercent}%";
    }

    private void SetStatus(string text, System.Windows.Media.Brush color)
    {
        txtStatus.Text = text;
        statusDot.Fill = color;
    }
}
