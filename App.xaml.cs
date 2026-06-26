using System.Drawing;
using System.Drawing.Drawing2D;

namespace PRecording;

public partial class App : System.Windows.Application
{
    private System.Windows.Forms.NotifyIcon? _trayIcon;
    private MainWindow? _mainWindow;

    protected override void OnStartup(System.Windows.StartupEventArgs e)
    {
        base.OnStartup(e);

        _mainWindow = new MainWindow();

        CreateTrayIcon();

        // 初始显示主窗口
        _mainWindow.Show();
    }

    /// <summary>创建系统托盘图标和右键菜单</summary>
    private void CreateTrayIcon()
    {
        _trayIcon = new System.Windows.Forms.NotifyIcon
        {
            Icon = CreateTrayIcon(System.Drawing.Color.DodgerBlue),
            Visible = true,
            Text = "电钢琴预录制 — 录音中"
        };

        var menu = new System.Windows.Forms.ContextMenuStrip();

        var showItem = new System.Windows.Forms.ToolStripMenuItem("显示主窗口");
        showItem.Click += (_, _) => ShowMainWindow();
        menu.Items.Add(showItem);

        var saveItem = new System.Windows.Forms.ToolStripMenuItem("保存最近 5 分钟");
        saveItem.Click += (_, _) => _mainWindow?.SaveRecording();
        menu.Items.Add(saveItem);

        menu.Items.Add(new System.Windows.Forms.ToolStripSeparator());

        var exitItem = new System.Windows.Forms.ToolStripMenuItem("退出程序");
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

    /// <summary>程序化生成托盘图标（蓝色圆形）</summary>
    private static System.Drawing.Icon CreateTrayIcon(System.Drawing.Color color)
    {
        int size = 32;
        using var bitmap = new System.Drawing.Bitmap(size, size);
        using var g = System.Drawing.Graphics.FromImage(bitmap);
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.Clear(System.Drawing.Color.Transparent);
        using var brush = new SolidBrush(color);
        g.FillEllipse(brush, 2, 2, size - 4, size - 4);
        return System.Drawing.Icon.FromHandle(bitmap.GetHicon());
    }

    protected override void OnExit(System.Windows.ExitEventArgs e)
    {
        _trayIcon?.Dispose();
        base.OnExit(e);
    }
}
