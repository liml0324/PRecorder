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
        SetWindowIcon();
        PopulateSaveDuration();
        ApplyLanguage();

        _statusTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(500)
        };
        _statusTimer.Tick += StatusTimer_Tick;
        _statusTimer.Start();
    }

    /// <summary>应用当前语言到所有 UI 控件</summary>
    private void ApplyLanguage()
    {
        // 按钮
        btnRecord.Content = _recorder == null ? L("BtnStart") : L("BtnStop");
        btnSave.Content = L("BtnSave");
        btnSettings.Content = L("BtnSettings");
        btnExit.Content = L("BtnExit");
        btnRefreshDevices.Content = L("RefreshDevices");

        // 设备标签
        var deviceBlock = cmbDevice.Parent as System.Windows.Controls.Grid;
        if (deviceBlock?.Parent is System.Windows.Controls.StackPanel sp && sp.Children[0] is System.Windows.Controls.TextBlock tb)
            tb.Text = L("DeviceLabel");

        // 状态
        if (_recorder != null) SetStatus(L("StatusRecording"), System.Windows.Media.Brushes.Crimson);
        else SetStatus(L("StatusIdle"), System.Windows.Media.Brushes.Gray);
    }

    /// <summary>快捷获取本地化字符串</summary>
    private static string L(string key) => LocalizationService.Get(key);
    private static string L(string key, params object[] args) => LocalizationService.Get(key, args);

    /// <summary>窗口关闭 → 根据设置决定隐藏到托盘还是直接退出</summary>
    private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
    {
        if (_isExiting) return;

        if (AppSettings.MinimizeToTray)
        {
            e.Cancel = true;
            Hide();
            ShowTrayBalloon(L("TrayBalloonTitle"), L("TrayBalloonText"));
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

    /// <summary>安全加载窗口图标（缺失时静默跳过，不影响启动）</summary>
    private void SetWindowIcon()
    {
        try
        {
            string iconPath = System.IO.Path.Combine(
                System.AppDomain.CurrentDomain.BaseDirectory, "icon.png");
            if (System.IO.File.Exists(iconPath))
            {
                Icon = new System.Windows.Media.Imaging.BitmapImage(
                    new Uri(iconPath));
            }
        }
        catch
        {
            // icon.png not found - window uses default icon, app still works
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

    private void BtnSettings_Click(object sender, System.Windows.RoutedEventArgs e)
    {
        var settingsWindow = new SettingsWindow { Owner = this };
        settingsWindow.ShowDialog();
        PopulateSaveDuration(); // 缓冲区时长可能变了，刷新下拉
        ApplyLanguage();
    }

    /// <summary>根据缓冲区时长动态填充保存时长下拉框</summary>
    private void PopulateSaveDuration()
    {
        int bufferMin = AppSettings.BufferDurationMinutes;
        int current = AppSettings.SaveDurationMinutes;
        if (current > bufferMin) { current = bufferMin; AppSettings.SaveDurationMinutes = bufferMin; }

        cmbSaveDuration.Items.Clear();
        foreach (int m in new[] { 1, 2, 3, 5, 10, 15, 30, 60 })
        {
            if (m > bufferMin) break;
            cmbSaveDuration.Items.Add(new System.Windows.Controls.ComboBoxItem
            {
                Tag = m,
                Content = m == 1 ? "1 min" : $"{m} min"
            });
            if (m == current) cmbSaveDuration.SelectedItem = cmbSaveDuration.Items[^1];
        }
        if (cmbSaveDuration.SelectedItem == null && cmbSaveDuration.Items.Count > 0)
            cmbSaveDuration.SelectedIndex = cmbSaveDuration.Items.Count - 1;
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
                System.Windows.MessageBox.Show(L("MsgSelectDevice"), L("MsgTitle"),
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
                System.Windows.MessageBox.Show(L("MsgStartFailed", ex.Message), L("MsgError"),
                    System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                _recorder.Dispose();
                _recorder = null;
                return;
            }

            btnRecord.Content = L("BtnStop");
            cmbDevice.IsEnabled = false;
            btnRefreshDevices.IsEnabled = false;
            btnSave.IsEnabled = true;
            cmbSaveDuration.IsEnabled = true;
            SetStatus(L("StatusRecording"), System.Windows.Media.Brushes.Crimson);
        }
        else
        {
            // 停止录音
            _recorder.Stop();
            _recorder.Dispose();
            _recorder = null;

            btnRecord.Content = L("BtnStart");
            cmbDevice.IsEnabled = true;
            btnRefreshDevices.IsEnabled = true;
            btnSave.IsEnabled = false;
            cmbSaveDuration.IsEnabled = false;
            SetStatus(L("StatusStopped"), System.Windows.Media.Brushes.Gray);
            txtDuration.Text = L("DurationEmpty");
            txtBuffer.Text = L("BufferEmpty");
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
            System.Windows.MessageBox.Show(L("MsgNotRecording"), L("MsgTitle"),
                System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
            return;
        }

        string ext = AppSettings.SaveFormat;
        string savePath = AppSettings.SavePath;

        if (ext != "wav" && !AppSettings.FfmpegAvailable)
        {
            System.Windows.MessageBox.Show(L("MsgNoFfmpeg"), L("MsgFfmpegTitle"),
                System.Windows.MessageBoxButton.OK,
                System.Windows.MessageBoxImage.Warning);
            return;
        }

        // 确保保存目录存在
        try { System.IO.Directory.CreateDirectory(savePath); } catch { }

        string fileName = $"PRecorder_{DateTime.Now:yyyyMMdd_HHmmss}.{ext}";
        string fullPath = System.IO.Path.Combine(savePath, fileName);

        int saveSec = AppSettings.SaveDurationMinutes * 60;
        var durItem = cmbSaveDuration.SelectedItem as System.Windows.Controls.ComboBoxItem;
        if (durItem?.Tag != null && int.TryParse(durItem.Tag.ToString(), out int min))
        {
            saveSec = min * 60;
            AppSettings.SaveDurationMinutes = min;
        }

        if (ext == "wav")
        {
            _recorder.SaveHistory(fullPath, saveSec);
        }
        else
        {
            string tempWav = System.IO.Path.Combine(
                System.IO.Path.GetTempPath(),
                $"~prec_{Guid.NewGuid():N}.wav");

            _recorder.SaveHistory(tempWav, saveSec);

            try
            {
                ConvertWithFfmpeg(tempWav, fullPath, ext);
            }
            finally
            {
                try { System.IO.File.Delete(tempWav); } catch { }
            }
        }

        txtLastSave.Text = L("LastSaved", fileName);
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

        txtDuration.Text = $"{L("DurationLabel")} {status.RecordingDuration:hh\\:mm\\:ss}";

        int bufferMin = AppSettings.BufferDurationMinutes;
        if (status.BufferDuration > TimeSpan.Zero)
        {
            txtBuffer.Text = L("BufferRecent", status.BufferDuration.ToString(@"hh\:mm\:ss"), bufferMin);
        }
        else
        {
            txtBuffer.Text = L("BufferRecentShort", status.BufferDuration.ToString(@"hh\:mm\:ss"));
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
