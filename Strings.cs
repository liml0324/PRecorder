using System.Globalization;
using System.Resources;

namespace PRecorder;

/// <summary>
/// Accessor for Strings.resx / Strings.en-US.resx
/// </summary>
internal static class Strings
{
    private static readonly ResourceManager rm = new(
        "PRecorder.Strings", typeof(Strings).Assembly);

    internal static string Get(string key)
    {
        return rm.GetString(key, CultureInfo.CurrentUICulture) ?? key;
    }
}
