using System.Windows;

namespace PRecorder;

public partial class SettingsWindow : Window
{
    public SettingsWindow()
    {
        InitializeComponent();
        LoadCurrentSettings();
    }

    private void LoadCurrentSettings()
    {
        // 保存路径
        txtSavePath.Text = AppSettings.SavePath;

        // 保存格式
        PopulateFormatList();
        SelectFormat(AppSettings.SaveFormat);

        // 缓冲区时长
        int minutes = AppSettings.BufferDurationMinutes;
        foreach (System.Windows.Controls.ComboBoxItem item in cmbBufferDuration.Items)
        {
            if (item.Tag?.ToString() == minutes.ToString())
            {
                cmbBufferDuration.SelectedItem = item;
                break;
            }
        }
        cmbBufferDuration.SelectedItem ??= cmbBufferDuration.Items[3];

        // 关闭行为
        if (AppSettings.MinimizeToTray)
            rbTray.IsChecked = true;
        else
            rbExit.IsChecked = true;
    }

    private void PopulateFormatList()
    {
        cmbFormat.Items.Clear();
        foreach (var (tag, label) in AppSettings.Formats)
        {
            var item = new System.Windows.Controls.ComboBoxItem
            {
                Tag = tag,
                Content = tag == "wav"
                    ? label
                    : AppSettings.FfmpegAvailable ? label : $"{label} (FFmpeg 未安装)",
                IsEnabled = tag == "wav" || AppSettings.FfmpegAvailable
            };
            cmbFormat.Items.Add(item);
        }
    }

    private void SelectFormat(string tag)
    {
        foreach (System.Windows.Controls.ComboBoxItem item in cmbFormat.Items)
        {
            if (item.Tag?.ToString() == tag && item.IsEnabled)
            {
                cmbFormat.SelectedItem = item;
                return;
            }
        }
        cmbFormat.SelectedIndex = 0;
    }

    private void BtnBrowsePath_Click(object sender, RoutedEventArgs e)
    {
        using var dialog = new System.Windows.Forms.FolderBrowserDialog
        {
            Description = "选择音频文件保存目录",
            SelectedPath = AppSettings.SavePath,
            ShowNewFolderButton = true
        };

        if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
        {
            txtSavePath.Text = dialog.SelectedPath;
        }
    }

    private void BtnSave_Click(object sender, RoutedEventArgs e)
    {
        // 保存路径
        AppSettings.SavePath = txtSavePath.Text;

        // 保存格式
        var fmtItem = cmbFormat.SelectedItem as System.Windows.Controls.ComboBoxItem;
        AppSettings.SaveFormat = fmtItem?.Tag?.ToString() ?? "wav";

        // 缓冲区时长
        var durItem = cmbBufferDuration.SelectedItem as System.Windows.Controls.ComboBoxItem;
        if (durItem?.Tag != null && int.TryParse(durItem.Tag.ToString(), out int minutes))
        {
            AppSettings.BufferDurationMinutes = minutes;
        }

        // 关闭行为
        AppSettings.MinimizeToTray = rbTray.IsChecked == true;

        DialogResult = true;
        Close();
    }

    private void BtnCancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}
