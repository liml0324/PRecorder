using System.Drawing;
using System.Drawing.Drawing2D;

namespace PRecorder;

public partial class App : System.Windows.Application
{
    private System.Windows.Forms.NotifyIcon? _trayIcon;
    private MainWindow? _mainWindow;

    protected override void OnStartup(System.Windows.StartupEventArgs e)
    {
        base.OnStartup(e);

        // 检测语言：Inno Setup 写入 > 系统语言 > 默认 en-US
        InitLanguage();

        _mainWindow = new MainWindow();

        CreateTrayIcon();

        _mainWindow.Show();
    }

    /// <summary>初始化语言：检查 Inno Setup 写入的 language.txt</summary>
    private static void InitLanguage()
    {
        try
        {
            string langFile = System.IO.Path.Combine(
                System.AppDomain.CurrentDomain.BaseDirectory, "language.txt");
            if (System.IO.File.Exists(langFile))
            {
                string lang = System.IO.File.ReadAllText(langFile).Trim();
                if (lang == "zh-CN" || lang == "en-US")
                {
                    AppSettings.Language = lang;
                    System.IO.File.Delete(langFile); // 仅首次生效
                }
            }
        }
        catch { }

        LocalizationService.ApplyLanguage(AppSettings.Language);
    }

    /// <summary>创建系统托盘图标和右键菜单</summary>
    private void CreateTrayIcon()
    {
        _trayIcon = new System.Windows.Forms.NotifyIcon
        {
            Icon = LoadAppIcon(),
            Visible = true,
            Text = LocalizationService.Get("TrayText")
        };

        var menu = new System.Windows.Forms.ContextMenuStrip();

        var showItem = new System.Windows.Forms.ToolStripMenuItem(LocalizationService.Get("TrayShow"));
        showItem.Click += (_, _) => ShowMainWindow();
        menu.Items.Add(showItem);

        var saveItem = new System.Windows.Forms.ToolStripMenuItem(LocalizationService.Get("TraySave"));
        saveItem.Click += (_, _) => _mainWindow?.SaveRecording();
        menu.Items.Add(saveItem);

        menu.Items.Add(new System.Windows.Forms.ToolStripSeparator());

        var exitItem = new System.Windows.Forms.ToolStripMenuItem(LocalizationService.Get("TrayExit"));
        exitItem.Click += (_, _) => ExitApplication();
        menu.Items.Add(exitItem);

        _trayIcon.ContextMenuStrip = menu;
        _trayIcon.DoubleClick += (_, _) => ShowMainWindow();
    }

    /// <summary>显示主窗口</summary>
    public void ShowMainWindow()
    {
        if (_mainWindow == null) return;

        _mainWindow.Show();
        _mainWindow.WindowState = System.Windows.WindowState.Normal;
        _mainWindow.Activate();
    }

    /// <summary>退出应用程序</summary>
    public void ExitApplication()
    {
        _mainWindow?.Cleanup();
        _trayIcon?.Dispose();
        _trayIcon = null;
        Shutdown();
    }

    /// <summary>弹出托盘气泡提示</summary>
    public void ShowBalloonTip(string title, string text)
    {
        _trayIcon?.ShowBalloonTip(3000, title, text,
            System.Windows.Forms.ToolTipIcon.Info);
    }

    /// <summary>从 logo.png 加载托盘图标</summary>
    private static System.Drawing.Icon LoadAppIcon()
    {
        string iconPath = System.IO.Path.Combine(
            System.AppDomain.CurrentDomain.BaseDirectory, "icon_32.png");

        try
        {
            using var bitmap = new System.Drawing.Bitmap(iconPath);
            // 转为 32x32 的图标
            using var resized = new System.Drawing.Bitmap(bitmap, 32, 32);
            IntPtr hIcon = resized.GetHicon();
            var icon = (System.Drawing.Icon)System.Drawing.Icon.FromHandle(hIcon).Clone();
            NativeMethods.DestroyIcon(hIcon);
            return icon;
        }
        catch
        {
            // 回退：生成蓝色圆形图标
            return CreateFallbackIcon();
        }
    }

    /// <summary>回退图标（蓝色圆形）</summary>
    private static System.Drawing.Icon CreateFallbackIcon()
    {
        int size = 32;
        using var bitmap = new System.Drawing.Bitmap(size, size);
        using var g = System.Drawing.Graphics.FromImage(bitmap);
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.Clear(System.Drawing.Color.Transparent);
        using var brush = new SolidBrush(System.Drawing.Color.DodgerBlue);
        g.FillEllipse(brush, 2, 2, size - 4, size - 4);
        return System.Drawing.Icon.FromHandle(bitmap.GetHicon());
    }

    /// <summary>销毁原生图标句柄</summary>
    private static class NativeMethods
    {
        [System.Runtime.InteropServices.DllImport("user32.dll")]
        public static extern bool DestroyIcon(IntPtr hIcon);
    }

    protected override void OnExit(System.Windows.ExitEventArgs e)
    {
        _trayIcon?.Dispose();
        base.OnExit(e);
    }
}
