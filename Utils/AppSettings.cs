using System.Collections.Immutable;

namespace BanchoNET.Utils;

public static class AppSettings
{
    static AppSettings()
    {
        Domain = Environment.GetEnvironmentVariable("DOMAIN")!;
        
        var banchoNetVersion = Environment.GetEnvironmentVariable("BANCHONET_VERSION");
        BanchoNETVersion = string.IsNullOrEmpty(banchoNetVersion)
            ? "0.2"
            : banchoNetVersion;
        
        var disallowOldClients = Environment.GetEnvironmentVariable("DISALLOW_OLD_CLIENTS");
        DisallowOldClients = string.IsNullOrEmpty(disallowOldClients)
                             || bool.Parse(disallowOldClients);
        
        var sortLeaderboardByPP = Environment.GetEnvironmentVariable("SORT_LEADERBOARD_BY_PP");
        SortLeaderboardByPP = string.IsNullOrEmpty(sortLeaderboardByPP)
                              || bool.Parse(sortLeaderboardByPP);
        
        var scoresOnLeaderboard = Environment.GetEnvironmentVariable("SCORES_ON_LEADERBOARD");
        ScoresOnLeaderboard = string.IsNullOrEmpty(scoresOnLeaderboard)
            ? 50
            : int.Parse(scoresOnLeaderboard);
        
        var submitByPP = Environment.GetEnvironmentVariable("SUBMIT_BY_PP");
        SubmitByPP = string.IsNullOrEmpty(submitByPP)
                     || bool.Parse(submitByPP);

        var scoreInNotification = Environment.GetEnvironmentVariable("DISPLAY_SCORE_IN_NOTIFICATION");
        DisplayScoreInNotification = string.IsNullOrEmpty(scoreInNotification)
                                     || bool.Parse(scoreInNotification);
	    
        var ppInNotification = Environment.GetEnvironmentVariable("DISPLAY_PP_IN_NOTIFICATION");
        DisplayPPInNotification = string.IsNullOrEmpty(ppInNotification)
                                  || bool.Parse(ppInNotification);
        
        var menuIconUrl = Environment.GetEnvironmentVariable("MENU_ICON_URL");
        MenuIconUrl = string.IsNullOrEmpty(menuIconUrl)
            ? ""
            : menuIconUrl;
        
        var menuOnclickUrl = Environment.GetEnvironmentVariable("MENU_ONCLICK_URL");
        MenuOnclickUrl = string.IsNullOrEmpty(menuOnclickUrl)
            ? ""
            : menuOnclickUrl;

        var banchoBotName = Environment.GetEnvironmentVariable("BANCHO_BOT_NAME");
        BanchoBotName = string.IsNullOrEmpty(banchoBotName)
            ? "Bancho Bot"
            : banchoBotName;
        
        var disallowedNames = Environment.GetEnvironmentVariable("DISALLOWED_NAMES");
        DisallowedNames =
            ImmutableList.Create(string.IsNullOrEmpty(disallowedNames) ? [""] : disallowedNames.Split(","));

        var searchEndpoints = Environment.GetEnvironmentVariable("OSU_DIRECT_SEARCH_ENDPOINTS");
        OsuDirectSearchEndpoints =
            ImmutableList.Create(string.IsNullOrEmpty(searchEndpoints) ? [""] : searchEndpoints.Split(","));

        var downloadEndpoints = Environment.GetEnvironmentVariable("OSU_DIRECT_DOWNLOAD_ENDPOINTS");
        OsuDirectDownloadEndpoints =
            ImmutableList.Create(string.IsNullOrEmpty(downloadEndpoints) ? [""] : downloadEndpoints.Split(","));
        
        var storeLocally = Environment.GetEnvironmentVariable("STORE_BEATMAPS_LOCALLY");
        StoreBeatmapsLocally = string.IsNullOrEmpty(storeLocally)
            || bool.Parse(storeLocally);
        
        //TODO make it more customizable (use string.Format or something to allow usage of some variables)
        var welcomeMessage = Environment.GetEnvironmentVariable("WELCOME_MESSAGE");
        WelcomeMessage = string.IsNullOrEmpty(welcomeMessage)
            ? $"Welcome to {Domain}!"
            : welcomeMessage;
        
        var firstLoginMessage = Environment.GetEnvironmentVariable("FIRST_LOGIN_MESSAGE");
        FirstLoginMessage = string.IsNullOrEmpty(firstLoginMessage)
            ? $"Welcome to {Domain}!"
            : firstLoginMessage;
        
        var restrictedMessage = Environment.GetEnvironmentVariable("RESTRICTED_MESSAGE");
        RestrictedMessage = string.IsNullOrEmpty(restrictedMessage)
            ? "You've been restricted. You can't access any online features on this server until restrictions are lifted. Please contact support for more information"
            : restrictedMessage;

        var versionFetchHours = Environment.GetEnvironmentVariable("VERSION_FETCH_HOURS_DELAY");
        VersionFetchHoursDelay = string.IsNullOrEmpty(versionFetchHours)
            ? 1
            : int.Parse(versionFetchHours);

        var commandPrefix = Environment.GetEnvironmentVariable("COMMAND_PREFIX");
        CommandPrefix = string.IsNullOrEmpty(commandPrefix)
            ? "!"
            : commandPrefix;

        var osuApiKey = Environment.GetEnvironmentVariable("OSU_API_KEY");
        OsuApiKey = string.IsNullOrEmpty(osuApiKey)
            ? ""
            : osuApiKey;
    }

    public static readonly string Domain;
    public static readonly string BanchoNETVersion;
    public static readonly bool DisallowOldClients;
    public static readonly bool SortLeaderboardByPP;
    public static readonly int ScoresOnLeaderboard;
    public static readonly bool SubmitByPP;
    public static readonly bool DisplayPPInNotification;
    public static readonly bool DisplayScoreInNotification;
    public static readonly string MenuIconUrl;
    public static readonly string MenuOnclickUrl;
    public static readonly string BanchoBotName;
    public static readonly ImmutableList<string> DisallowedNames;
    public static readonly ImmutableList<string> OsuDirectSearchEndpoints;
    public static readonly ImmutableList<string> OsuDirectDownloadEndpoints;
    public static readonly bool StoreBeatmapsLocally;
    public static readonly string WelcomeMessage;
    public static readonly string FirstLoginMessage;
    public static readonly string RestrictedMessage;
    public static readonly int VersionFetchHoursDelay;
    public static readonly string CommandPrefix;
    public static readonly string OsuApiKey;
}