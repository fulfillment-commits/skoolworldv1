using System;

public static class ChatIdentityUtility
{
    public const char PunNicknameSeparator = '$';

    public static string BuildPunNickname(string displayName, string firebaseUid)
    {
        return $"{CleanDisplayName(displayName)}{PunNicknameSeparator}{firebaseUid}";
    }

    public static bool TryParsePunNickname(string nickname, out string displayName, out string firebaseUid)
    {
        displayName = "";
        firebaseUid = "";

        if (string.IsNullOrWhiteSpace(nickname))
        {
            return false;
        }

        string[] parts = nickname.Split(PunNicknameSeparator);
        if (parts.Length < 2 || string.IsNullOrWhiteSpace(parts[1]))
        {
            displayName = nickname.Trim();
            return false;
        }

        displayName = CleanDisplayName(parts[0]);
        firebaseUid = parts[1].Trim();
        return !string.IsNullOrEmpty(firebaseUid);
    }

    public static string GetChatId(string uidA, string uidB)
    {
        string cleanA = SanitizeFirestoreId(uidA);
        string cleanB = SanitizeFirestoreId(uidB);

        return string.CompareOrdinal(cleanA, cleanB) <= 0
            ? $"{cleanA}_{cleanB}"
            : $"{cleanB}_{cleanA}";
    }

    public static string SanitizeFirestoreId(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return "";
        }

        return value.Trim()
            .Replace("/", "_")
            .Replace("\\", "_");
    }

    private static string CleanDisplayName(string displayName)
    {
        if (string.IsNullOrWhiteSpace(displayName))
        {
            return "User";
        }

        return displayName.Trim().Replace(PunNicknameSeparator.ToString(), "");
    }
}
