namespace BanchoNET.Utils;

public static class AppSettings
{
    static AppSettings()
    {
        Domain = Environment.GetEnvironmentVariable("DOMAIN")!;
        BanchoNETVersion = string.IsNullOrEmpty(Environment.GetEnvironmentVariable("BANCHONET_VERSION")) ? "0.2" : Environment.GetEnvironmentVariable("BANCHONET_VERSION")!;
        DisallowOldClients = string.IsNullOrEmpty(Environment.GetEnvironmentVariable("DISALLOW_OLD_CLIENTS")) || bool.Parse(Environment.GetEnvironmentVariable("DISALLOW_OLD_CLIENTS")!);
        SortLeaderboardByPP = string.IsNullOrEmpty(Environment.GetEnvironmentVariable("SORT_LEADERBOARD_BY_PP")) || bool.Parse(Environment.GetEnvironmentVariable("SORT_LEADERBOARD_BY_PP")!);
        DisplayPPOnLeaderboard = string.IsNullOrEmpty(Environment.GetEnvironmentVariable("DISPLAY_PP_ON_LEADERBOARD")) || bool.Parse(Environment.GetEnvironmentVariable("DISPLAY_PP_ON_LEADERBOARD")!);
        DisplayScoreInNotification = string.IsNullOrEmpty(Environment.GetEnvironmentVariable("DISPLAY_SCORE_IN_NOTIFICATION")) || bool.Parse(Environment.GetEnvironmentVariable("DISPLAY_SCORE_IN_NOTIFICATION")!);
	    DisplayPPInNotification = string.IsNullOrEmpty(Environment.GetEnvironmentVariable("DISPLAY_PP_IN_NOTIFICATION")) || bool.Parse(Environment.GetEnvironmentVariable("DISPLAY_PP_IN_NOTIFICATION")!);
        SubmitByPP = string.IsNullOrEmpty(Environment.GetEnvironmentVariable("SUBMIT_BY_PP")) || bool.Parse(Environment.GetEnvironmentVariable("SUBMIT_BY_PP")!);
        MenuIconUrl = string.IsNullOrEmpty(Environment.GetEnvironmentVariable("MENU_ICON_URL")) ? "" : Environment.GetEnvironmentVariable("MENU_ICON_URL")!;
        MenuOnclickUrl = string.IsNullOrEmpty(Environment.GetEnvironmentVariable("MENU_ONCLICK_URL")) ? "" : Environment.GetEnvironmentVariable("MENU_ONCLICK_URL")!;
        VersionFetchHoursDelay = string.IsNullOrEmpty(Environment.GetEnvironmentVariable("VERSION_FETCH_HOURS_DELAY")) ? 1 : int.Parse(Environment.GetEnvironmentVariable("VERSION_FETCH_HOURS_DELAY")!);
        CommandPrefix = string.IsNullOrEmpty(Environment.GetEnvironmentVariable("COMMAND_PREFIX")) ? "!" : Environment.GetEnvironmentVariable("COMMAND_PREFIX")!;
        OsuApiKey = string.IsNullOrEmpty(Environment.GetEnvironmentVariable("OSU_API_KEY")) ? "" : Environment.GetEnvironmentVariable("OSU_API_KEY")!;
    }

    public static readonly string Domain;
    public static readonly string BanchoNETVersion;
    public static readonly bool DisallowOldClients;
    public static readonly bool SortLeaderboardByPP;
    public static readonly bool SubmitByPP;
    public static readonly bool DisplayPPOnLeaderboard;
    public static readonly bool DisplayPPInNotification;
    public static readonly bool DisplayScoreInNotification;
    public static readonly string MenuIconUrl;
    public static readonly string MenuOnclickUrl;
    public static readonly int VersionFetchHoursDelay;
    public static readonly string CommandPrefix;
    public static readonly string OsuApiKey;
}