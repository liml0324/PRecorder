using NAudio.Wave;
using System.Diagnostics;
using System.Windows.Threading;

namespace PRecorder;

public partial class MainWindow : System.Windows.Window
{
    private PianoRecorder? _recorder;
    private DispatcherTimer? _statusTimer;
    private bool _isExiting;

    public MainWindow()
    {
        InitializeComponent();
    }

    /// <summary>窗口加载：初始化设备列表和状态刷新定时器</summary>
    private void Window_Loaded(object sender, System.Windows.RoutedEventArgs e)
    {
        RefreshDeviceList();

        _statusTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(500)
        };
        _statusTimer.Tick += StatusTimer_Tick;
        _statusTimer.Start();
    }

    /// <summary>窗口关闭 → 根据设置决定隐藏到托盘还是直接退出</summary>
    private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
    {
        if (_isExiting) return;

        if (AppSettings.MinimizeToTray)
        {
            e.Cancel = true;
            Hide();
            ShowTrayBalloon("PRecorder 已最小化到系统托盘", "双击托盘图标可重新打开，右键可退出程序。");
        }
        else
        {
            _isExiting = true;
            Cleanup();
            (System.Windows.Application.Current as App)?.ExitApplication();
        }
    }

    /// <summary>在系统托盘弹出气泡提示</summary>
    private static void ShowTrayBalloon(string title, string text)
    {
        var app = System.Windows.Application.Current as App;
        app?.ShowBalloonTip(title, text);
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

    private void BtnSettings_Click(object sender, System.Windows.RoutedEventArgs e)
    {
        var settingsWindow = new SettingsWindow
        {
            Owner = this
        };
        settingsWindow.ShowDialog();
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
                int bufferSec = AppSettings.BufferDurationMinutes * 60;
                _recorder.StartRecording(deviceId, bufferSec);
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

        string ext = AppSettings.SaveFormat;
        string savePath = AppSettings.SavePath;

        if (ext != "wav" && !AppSettings.FfmpegAvailable)
        {
            System.Windows.MessageBox.Show(
                "未检测到 FFmpeg，无法保存为非 WAV 格式。\n\n" +
                "请安装 FFmpeg 后重试，或将格式改为 WAV。\n" +
                "下载地址: https://ffmpeg.org/download.html",
                "FFmpeg 不可用",
                System.Windows.MessageBoxButton.OK,
                System.Windows.MessageBoxImage.Warning);
            return;
        }

        // 确保保存目录存在
        try { System.IO.Directory.CreateDirectory(savePath); } catch { }

        string fileName = $"PRecorder_{DateTime.Now:yyyyMMdd_HHmmss}.{ext}";
        string fullPath = System.IO.Path.Combine(savePath, fileName);

        if (ext == "wav")
        {
            _recorder.SaveHistory(fullPath);
        }
        else
        {
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
