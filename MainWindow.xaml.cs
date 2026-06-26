using NAudio.Wave;
using System.Windows.Threading;

namespace PRecording;

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

        string fileName = $"Piano_{DateTime.Now:yyyyMMdd_HHmmss}.wav";
        string fullPath = System.IO.Path.Combine(
            System.AppDomain.CurrentDomain.BaseDirectory, fileName);

        _recorder.SaveHistory(fullPath);

        txtLastSave.Text = $"上次保存: {fileName}";
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
