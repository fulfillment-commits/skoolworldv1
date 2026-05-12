// TimezoneHelper.cs
using System.Runtime.InteropServices;

public static class TimezoneHelper
{
    [DllImport("__Internal")]
    private static extern string GetUserTimezone();

    public static string GetCurrentTimezone()
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        try
        {
            return GetUserTimezone();
        }
        catch
        {
            return "Asia/Karachi"; // Fallback
        }
#else
        // In Unity Editor, use a default (you can change this)
        return "Asia/Karachi";
#endif
    }
}