using System.Diagnostics;
using System.IO;
using System.Text.Json;

namespace PRecorder;

/// <summary>
/// 应用设置，自动持久化到 %AppData%/PRecorder/settings.json
/// </summary>
public static class AppSettings
{
    private static readonly string SettingsDir;
    private static readonly string SettingsFile;
    private static bool? _ffmpegAvailable;

    private static SettingsData _data = new();

    static AppSettings()
    {
        SettingsDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "PRecorder");
        SettingsFile = Path.Combine(SettingsDir, "settings.json");
        Load();
    }

    /// <summary>音频文件保存目录</summary>
    public static string SavePath
    {
        get => _data.SavePath;
        set { _data.SavePath = value; Save(); }
    }

    /// <summary>保存格式标签（wav/mp3/flac/aac/ogg）</summary>
    public static string SaveFormat
    {
        get => _data.SaveFormat;
        set { _data.SaveFormat = value; Save(); }
    }

    /// <summary>循环缓冲区时长（分钟），默认 5</summary>
    public static int BufferDurationMinutes
    {
        get => _data.BufferDurationMinutes;
        set { _data.BufferDurationMinutes = Math.Clamp(value, 1, 60); Save(); }
    }

    /// <summary>关闭窗口时是否最小化到托盘（false = 直接退出）</summary>
    public static bool MinimizeToTray
    {
        get => _data.MinimizeToTray;
        set { _data.MinimizeToTray = value; Save(); }
    }

    /// <summary>界面语言（zh-CN 或 en-US），默认 en-US</summary>
    public static string Language
    {
        get => _data.Language;
        set { _data.Language = value; Save(); }
    }

    /// <summary>FFmpeg 是否可用（懒加载，仅检测一次）</summary>
    public static bool FfmpegAvailable
    {
        get
        {
            _ffmpegAvailable ??= CheckFfmpeg();
            return _ffmpegAvailable.Value;
        }
    }

    /// <summary>支持的导出格式列表</summary>
    public static readonly (string Tag, string Label)[] Formats =
    [
        ("wav",  "WAV (无损)"),
        ("mp3",  "MP3 (320 kbps)"),
        ("flac", "FLAC (无损压缩)"),
        ("aac",  "AAC (256 kbps)"),
        ("ogg",  "OGG Vorbis"),
    ];

    private static bool CheckFfmpeg()
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
            return process.ExitCode == 0;
        }
        catch
        {
            return false;
        }
    }

    private static void Load()
    {
        try
        {
            if (File.Exists(SettingsFile))
            {
                string json = File.ReadAllText(SettingsFile);
                _data = JsonSerializer.Deserialize<SettingsData>(json) ?? new();
            }
        }
        catch
        {
            _data = new SettingsData();
        }
    }

    private static void Save()
    {
        try
        {
            Directory.CreateDirectory(SettingsDir);
            string json = JsonSerializer.Serialize(_data, new JsonSerializerOptions
            {
                WriteIndented = true
            });
            File.WriteAllText(SettingsFile, json);
        }
        catch
        {
            // 静默失败，不影响主流程
        }
    }

    private class SettingsData
    {
        public string SavePath { get; set; } =
            Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
        public string SaveFormat { get; set; } = "wav";
        public string Language { get; set; } = "en-US";
        public int BufferDurationMinutes { get; set; } = 5;
        public bool MinimizeToTray { get; set; } = true;
    }
}
