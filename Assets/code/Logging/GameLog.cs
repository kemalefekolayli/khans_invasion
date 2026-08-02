using UnityEngine;

public static class GameLog
{
    private static GameLogProfile activeProfile = GameLogProfile.AIWarOnly;
    private static GameLogCategory customCategories = GameLogCategory.AIWar;
    private static bool showWarnings = true;
    private static bool showErrors = true;

    public static GameLogProfile ActiveProfile => activeProfile;
    public static GameLogCategory ActiveCategories => GetActiveCategories();

    public static void Configure(GameLogProfile profile, GameLogCategory customMask, bool warningsEnabled, bool errorsEnabled)
    {
        activeProfile = profile;
        customCategories = customMask;
        showWarnings = warningsEnabled;
        showErrors = errorsEnabled;
    }

    public static bool IsEnabled(GameLogCategory category)
    {
        return (GetActiveCategories() & category) != 0;
    }

    public static void Log(GameLogCategory category, object message, Object context = null)
    {
        if (!IsEnabled(category)) return;

        string line = Format(category, message);
        if (context != null) Debug.Log(line, context);
        else Debug.Log(line);
        GameLogFileSink.Write(line);
    }

    public static void Warning(GameLogCategory category, object message, Object context = null)
    {
        if (!showWarnings || !IsEnabled(category)) return;

        string line = Format(category, message);
        if (context != null) Debug.LogWarning(line, context);
        else Debug.LogWarning(line);
        GameLogFileSink.Write(line);
    }

    public static void Error(GameLogCategory category, object message, Object context = null)
    {
        if (!showErrors || !IsEnabled(category)) return;

        string line = Format(category, message);
        if (context != null) Debug.LogError(line, context);
        else Debug.LogError(line);
        GameLogFileSink.Write(line);
    }

    private static GameLogCategory GetActiveCategories()
    {
        switch (activeProfile)
        {
            case GameLogProfile.Silent:
                return GameLogCategory.None;
            case GameLogProfile.AIWarOnly:
                return GameLogCategory.AIWar;
            case GameLogProfile.ProvinceDebug:
                return GameLogCategory.Province | GameLogCategory.Economy | GameLogCategory.Raid | GameLogCategory.Siege | GameLogCategory.Fog;
            case GameLogProfile.CombatDebug:
                return GameLogCategory.Army | GameLogCategory.Battle | GameLogCategory.Raid | GameLogCategory.Siege | GameLogCategory.AIWar;
            case GameLogProfile.EconomyDebug:
                return GameLogCategory.Economy | GameLogCategory.Province | GameLogCategory.Nation | GameLogCategory.Turn;
            case GameLogProfile.FullDebug:
                return GameLogCategory.All;
            case GameLogProfile.Custom:
                return customCategories;
            default:
                return GameLogCategory.AIWar;
        }
    }

    private static string Format(GameLogCategory category, object message)
    {
        return $"[{category}] {message}";
    }
}
