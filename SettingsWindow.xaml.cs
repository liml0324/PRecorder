using System.Windows;

namespace PRecorder;

public partial class SettingsWindow : Window
{
    public SettingsWindow()
    {
        InitializeComponent();
        LoadCurrentSettings();
        ApplyLanguage();
    }

    private static string L(string key) => LocalizationService.Get(key);
    private static string L(string key, params object[] args) => LocalizationService.Get(key, args);

    private void ApplyLanguage()
    {
        Title = L("SettingsTitle");
        lblSavePath.Text = L("SavePathLabel");
        lblFormat.Text = L("FormatLabel");
        lblBufferDuration.Text = L("BufferDurationLabel");
        lblLanguage.Text = L("LanguageLabel");
        lblCloseBehavior.Text = L("CloseBehaviorLabel");
        rbTray.Content = L("MinimizeToTray");
        rbExit.Content = L("ExitDirectly");
        btnBrowse.Content = L("BrowseBtn");
        btnCancel.Content = L("BtnCancel");
        btnSave.Content = L("BtnSaveSettings");
        PopulateBufferDurations();
        cmbLanguage.SelectedIndex = AppSettings.Language == "zh-CN" ? 1 : 0;
    }

    private void PopulateBufferDurations()
    {
        cmbBufferDuration.Items.Clear();
        int[] values = { 1, 2, 3, 5, 10, 15, 30, 60 };
        string[] keys = { "Duration1Min", "Duration2Min", "Duration3Min", "Duration5Min",
                          "Duration10Min", "Duration15Min", "Duration30Min", "Duration60Min" };
        for (int i = 0; i < values.Length; i++)
        {
            cmbBufferDuration.Items.Add(new System.Windows.Controls.ComboBoxItem
            {
                Tag = values[i],
                Content = L(keys[i])
            });
        }
    }

    private void LoadCurrentSettings()
    {
        txtSavePath.Text = AppSettings.SavePath;
        PopulateFormatList();
        SelectFormat(AppSettings.SaveFormat);
        PopulateBufferDurations();

        int minutes = AppSettings.BufferDurationMinutes;
        foreach (System.Windows.Controls.ComboBoxItem item in cmbBufferDuration.Items)
        {
            if (item.Tag?.ToString() == minutes.ToString())
            { cmbBufferDuration.SelectedItem = item; break; }
        }
        cmbBufferDuration.SelectedItem ??= cmbBufferDuration.Items[3];

        if (AppSettings.MinimizeToTray) rbTray.IsChecked = true;
        else rbExit.IsChecked = true;

        cmbLanguage.SelectedIndex = AppSettings.Language == "zh-CN" ? 1 : 0;
    }

    private void PopulateFormatList()
    {
        cmbFormat.Items.Clear();
        foreach (var (tag, label) in AppSettings.Formats)
        {
            var item = new System.Windows.Controls.ComboBoxItem
            {
                Tag = tag,
                Content = tag == "wav" ? label :
                    AppSettings.FfmpegAvailable ? label : L("FormatNoFfmpeg", label),
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
            { cmbFormat.SelectedItem = item; return; }
        }
        cmbFormat.SelectedIndex = 0;
    }

    private void BtnBrowsePath_Click(object sender, RoutedEventArgs e)
    {
        using var dialog = new System.Windows.Forms.FolderBrowserDialog
        {
            Description = L("SavePathLabel"),
            SelectedPath = AppSettings.SavePath,
            ShowNewFolderButton = true
        };
        if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            txtSavePath.Text = dialog.SelectedPath;
    }

    private void BtnSave_Click(object sender, RoutedEventArgs e)
    {
        AppSettings.SavePath = txtSavePath.Text;
        var fmtItem = cmbFormat.SelectedItem as System.Windows.Controls.ComboBoxItem;
        AppSettings.SaveFormat = fmtItem?.Tag?.ToString() ?? "wav";

        var durItem = cmbBufferDuration.SelectedItem as System.Windows.Controls.ComboBoxItem;
        if (durItem?.Tag != null && int.TryParse(durItem.Tag.ToString(), out int minutes))
            AppSettings.BufferDurationMinutes = minutes;

        AppSettings.MinimizeToTray = rbTray.IsChecked == true;

        string newLang = cmbLanguage.SelectedIndex == 1 ? "zh-CN" : "en-US";
        if (newLang != AppSettings.Language)
        {
            AppSettings.Language = newLang;
            LocalizationService.SetLanguage(newLang);
            System.Windows.MessageBox.Show(L("MsgRestartNeeded"), L("MsgTitle"),
                MessageBoxButton.OK, MessageBoxImage.Information);
        }

        DialogResult = true;
        Close();
    }

    private void BtnCancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}
