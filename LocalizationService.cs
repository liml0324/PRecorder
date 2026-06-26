using System.Globalization;

namespace PRecorder;

/// <summary>
/// 多语言服务：从资源文件读取字符串，支持运行时切换语言
/// </summary>
public static class LocalizationService
{
    static LocalizationService()
    {
        ApplyLanguage(AppSettings.Language);
    }

    /// <summary>当前语言代码（zh-CN 或 en-US）</summary>
    public static string CurrentLanguage => AppSettings.Language;

    /// <summary>获取字符串</summary>
    public static string Get(string key)
    {
        return Strings.Get(key) ?? key;
    }

    /// <summary>格式化字符串</summary>
    public static string Get(string key, params object[] args)
    {
        return string.Format(Get(key), args);
    }

    /// <summary>切换语言并持久化</summary>
    public static void SetLanguage(string lang)
    {
        AppSettings.Language = lang;
        ApplyLanguage(lang);
    }

    /// <summary>应用语言到当前线程</summary>
    public static void ApplyLanguage(string lang)
    {
        try
        {
            var culture = CultureInfo.GetCultureInfo(lang);
            Thread.CurrentThread.CurrentUICulture = culture;
            CultureInfo.DefaultThreadCurrentUICulture = culture;
        }
        catch
        {
            // fallback to neutral
            Thread.CurrentThread.CurrentUICulture = CultureInfo.InvariantCulture;
        }
    }
}
