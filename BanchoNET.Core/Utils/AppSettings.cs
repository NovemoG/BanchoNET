using BanchoNET.Core.Models.Players;
using Novelog;

namespace BanchoNET.Core.Utils;

public static class AppSettings
{
    static AppSettings()
    {
        Domain = Environment.GetEnvironmentVariable("DOMAIN")!;
        
        var dataPath = Environment.GetEnvironmentVariable("DATA_PATH");
        DataPath = string.IsNullOrEmpty(dataPath)
            ? "/home/user/BanchoNET/.data"
            : dataPath;
        
        var banchoNetVersion = Environment.GetEnvironmentVariable("BANCHONET_VERSION");
        BanchoNETVersion = string.IsNullOrEmpty(banchoNetVersion)
            ? "0.11.0"
            : banchoNetVersion;
        
        var debug = Environment.GetEnvironmentVariable("DEBUG");
        Debug = string.IsNullOrEmpty(debug)
                             || bool.Parse(debug);
        
        var disallowOldClients = Environment.GetEnvironmentVariable("DISALLOW_OLD_CLIENTS");
        DisallowOldClients = string.IsNullOrEmpty(disallowOldClients)
                             || bool.Parse(disallowOldClients);
        
        var banchoBotName = Environment.GetEnvironmentVariable("BANCHO_BOT_NAME");
        BanchoBotName = string.IsNullOrEmpty(banchoBotName)
            ? "BanchoBot"
            : banchoBotName;
        
        var disallowedNames = Environment.GetEnvironmentVariable("DISALLOWED_NAMES");
        DisallowedNames =
        [
            ..string.IsNullOrEmpty(disallowedNames)
                ? [""]
                : disallowedNames.ToLower().Split(",")
        ];
        
        //TODO make it more customizable (use string.Format or something to allow usage of some variables)
        var welcomeMessage = Environment.GetEnvironmentVariable("WELCOME_MESSAGE");
        WelcomeMessage = string.IsNullOrEmpty(welcomeMessage)
            ? $"Welcome to {Domain}!\nRunning BanchoNET v{BanchoNETVersion}"
            : welcomeMessage;
        
        var firstLoginMessage = Environment.GetEnvironmentVariable("FIRST_LOGIN_MESSAGE");
        FirstLoginMessage = string.IsNullOrEmpty(firstLoginMessage)
            ? $"Welcome to {Domain}!"
            : firstLoginMessage;
        
        var restrictedMessage = Environment.GetEnvironmentVariable("RESTRICTED_MESSAGE");
        RestrictedMessage = string.IsNullOrEmpty(restrictedMessage)
            ? "You've been restricted. You can't access any online features on this server until restrictions are lifted.\nPlease contact support for more information"
            : restrictedMessage;
        
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

        var ppInNotification = Environment.GetEnvironmentVariable("DISPLAY_PP_IN_NOTIFICATION");
        DisplayPPInNotification = string.IsNullOrEmpty(ppInNotification)
                                  || bool.Parse(ppInNotification);
        
        var scoreInNotification = Environment.GetEnvironmentVariable("DISPLAY_SCORE_IN_NOTIFICATION");
        DisplayScoreInNotification = string.IsNullOrEmpty(scoreInNotification)
                                     || bool.Parse(scoreInNotification);
        
        var menuIconUrl = Environment.GetEnvironmentVariable("MENU_ICON_URL");
        MenuIconUrl = string.IsNullOrEmpty(menuIconUrl)
            ? ""
            : menuIconUrl;
        
        var menuOnclickUrl = Environment.GetEnvironmentVariable("MENU_ONCLICK_URL");
        MenuOnclickUrl = string.IsNullOrEmpty(menuOnclickUrl)
            ? ""
            : menuOnclickUrl;

        var searchEndpoints = Environment.GetEnvironmentVariable("OSU_DIRECT_SEARCH_ENDPOINTS");
        OsuDirectSearchEndpoints =
        [
            ..string.IsNullOrEmpty(searchEndpoints)
                ? ["https://osu.direct/api/search"]
                : searchEndpoints.Split(",")
        ];

        var downloadEndpoint = Environment.GetEnvironmentVariable("OSU_DIRECT_DOWNLOAD_ENDPOINT");
        OsuDirectDownloadEndpoint = string.IsNullOrEmpty(downloadEndpoint)
            ? "https://osu.direct/d"
            : downloadEndpoint;

        #region BotStatuses

        var botStatuses = Environment.GetEnvironmentVariable("BOT_STATUSES");
        BotStatuses =
        [
            ..string.IsNullOrEmpty(botStatuses)
                ? [
                    (Activity.Afk, "looking for source.."),
                    (Activity.Editing, "the source code.."),
                    (Activity.Editing, "server's website.."),
                    (Activity.Modding, "your requests.."),
                    (Activity.Watching, "over all of you.."),
                    (Activity.Watching, "over the server.."),
                    (Activity.Testing, "my will to live.."),
                    (Activity.Testing, "your patience.."),
                    (Activity.Submitting, "scores to database.."),
                    (Activity.Submitting, "a pull request.."),
                    (Activity.OsuDirect, "updating maps..")
                ]
                : botStatuses.Split(",").Select(status =>
                {
                    var statusParts = status.Split(":");
                    return (Enum.Parse<Activity>(statusParts[0]), statusParts[1]);
                }).ToArray()
        ];
        
        #endregion
        
        var commandPrefix = Environment.GetEnvironmentVariable("COMMAND_PREFIX");
        if (string.IsNullOrEmpty(commandPrefix) || commandPrefix.Contains(' '))
        {
            Logger.Shared.LogWarning("No command prefix provided or it contains spaces. Using default prefix: !", caller: "Init");
            CommandPrefix = "!";
        }
        else CommandPrefix = commandPrefix;
        
        var statusInterval = Environment.GetEnvironmentVariable("BOT_STATUS_UPDATE_INTERVAL");
        BotStatusUpdateInterval = string.IsNullOrEmpty(statusInterval)
            ? 5
            : int.Parse(statusInterval);
        
        var versionFetchHours = Environment.GetEnvironmentVariable("VERSION_FETCH_INTERVAL_IN_HOURS");
        VersionFetchHoursInterval = string.IsNullOrEmpty(versionFetchHours)
            ? 1
            : int.Parse(versionFetchHours);

        var osuApiKey = Environment.GetEnvironmentVariable("OSU_API_KEY");
        OsuApiKey = string.IsNullOrEmpty(osuApiKey)
            ? ""
            : osuApiKey;
    }

    public static readonly string Domain;
    public static readonly bool Debug;
    public static readonly string BanchoNETVersion;
    public static readonly bool DisallowOldClients;
    public static readonly bool SortLeaderboardByPP;
    public static readonly int ScoresOnLeaderboard;
    public static readonly bool SubmitByPP;
    public static readonly bool DisplayPPInNotification;
    public static readonly bool DisplayScoreInNotification;
    public static readonly string MenuIconUrl;
    public static readonly string MenuOnclickUrl;
    public static readonly string DataPath;
    public static readonly string BanchoBotName;
    public static readonly List<string> DisallowedNames;
    public static readonly List<string> OsuDirectSearchEndpoints;
    public static readonly List<(Activity Activity, string Description)> BotStatuses;
    public static readonly string OsuDirectDownloadEndpoint;
    public static readonly string WelcomeMessage;
    public static readonly string FirstLoginMessage;
    public static readonly string RestrictedMessage;
    public static readonly int BotStatusUpdateInterval;
    public static readonly int VersionFetchHoursInterval;
    public static readonly string CommandPrefix;
    public static readonly string OsuApiKey;
}