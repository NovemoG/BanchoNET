using System.Collections.Immutable;
using System.Reflection;
using BanchoNET.Objects.Players;

namespace BanchoNET.Utils;

public static class AppSettings
{
    static AppSettings()
    {
        Domain = Environment.GetEnvironmentVariable("DOMAIN")!;
        
        var dataPath = Environment.GetEnvironmentVariable("DATA_PATH");
        DataPath = string.IsNullOrEmpty(dataPath)
            ? "/home/user/BanchoNET/.data"
            : dataPath;
        
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!)
            .AddJsonFile("appsettings.json")
            .AddJsonFile("appsettings.Development.json", true)
            .Build();
        
        var banchoNetVersion = configuration["Config:BanchoNETVersion"];
        BanchoNETVersion = string.IsNullOrEmpty(banchoNetVersion)
            ? "0.11.0"
            : banchoNetVersion;
        
        var debug = configuration["Config:Debug"];
        Debug = string.IsNullOrEmpty(debug)
                             || bool.Parse(debug);
        
        var disallowOldClients = configuration["Config:DisallowOldClients"];
        DisallowOldClients = string.IsNullOrEmpty(disallowOldClients)
                             || bool.Parse(disallowOldClients);
        
        var banchoBotName = configuration["Config:BanchoBotName"];
        BanchoBotName = string.IsNullOrEmpty(banchoBotName)
            ? "BanchoBot"
            : banchoBotName;
        
        var disallowedNames = configuration["Config:DisallowedNames"];
        DisallowedNames = ImmutableList.Create(string.IsNullOrEmpty(disallowedNames)
            ? [""]
            : disallowedNames.ToLower().Split(","));
        
        //TODO make it more customizable (use string.Format or something to allow usage of some variables)
        var welcomeMessage = configuration["Config:WelcomeMessage"];
        WelcomeMessage = string.IsNullOrEmpty(welcomeMessage)
            ? $"Welcome to {Domain}!\nRunning BanchoNET v{BanchoNETVersion}"
            : welcomeMessage;
        
        var firstLoginMessage = configuration["Config:FirstLoginMessage"];
        FirstLoginMessage = string.IsNullOrEmpty(firstLoginMessage)
            ? $"Welcome to {Domain}!"
            : firstLoginMessage;
        
        var restrictedMessage = configuration["Config:RestrictedMessage"];
        RestrictedMessage = string.IsNullOrEmpty(restrictedMessage)
            ? "You've been restricted. You can't access any online features on this server until restrictions are lifted.\nPlease contact support for more information"
            : restrictedMessage;
        
        var sortLeaderboardByPP = configuration["Config:SortLeaderboardByPP"];
        SortLeaderboardByPP = string.IsNullOrEmpty(sortLeaderboardByPP)
                              || bool.Parse(sortLeaderboardByPP);
        
        var scoresOnLeaderboard = configuration["Config:ScoresOnLeaderboard"];
        ScoresOnLeaderboard = string.IsNullOrEmpty(scoresOnLeaderboard)
            ? 50
            : int.Parse(scoresOnLeaderboard);
        
        var submitByPP = configuration["Config:SubmitByPP"];
        SubmitByPP = string.IsNullOrEmpty(submitByPP)
                     || bool.Parse(submitByPP);

        var ppInNotification = configuration["Config:DisplayPPInNotification"];
        DisplayPPInNotification = string.IsNullOrEmpty(ppInNotification)
                                  || bool.Parse(ppInNotification);
        
        var scoreInNotification = configuration["Config:DisplayScoreInNotification"];
        DisplayScoreInNotification = string.IsNullOrEmpty(scoreInNotification)
                                     || bool.Parse(scoreInNotification);
        
        var menuIconUrl = configuration["Config:DisplayScoreInNotification"];
        MenuIconUrl = string.IsNullOrEmpty(menuIconUrl)
            ? ""
            : menuIconUrl;
        
        var menuOnclickUrl = configuration["Config:DisplayScoreInNotification"];
        MenuOnclickUrl = string.IsNullOrEmpty(menuOnclickUrl)
            ? ""
            : menuOnclickUrl;

        var searchEndpoints = configuration["Config:OsuDirectSearchEndpoints"];
        OsuDirectSearchEndpoints = ImmutableList.Create(string.IsNullOrEmpty(searchEndpoints)
            ? ["https://osu.direct/api/search"]
            : searchEndpoints.Split(","));

        var downloadEndpoint = configuration["Config:OsuDirectDownloadEndpoint"];
        OsuDirectDownloadEndpoint = string.IsNullOrEmpty(downloadEndpoint)
            ? "https://osu.direct/d"
            : downloadEndpoint;

        #region BotStatuses

        var botStatuses = configuration["Config:BotStatuses"];
        BotStatuses = ImmutableList.Create(string.IsNullOrEmpty(botStatuses)
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
            : botStatuses.Split(",").Select(status => {
                var statusParts = status.Split(":");
                return (Enum.Parse<Activity>(statusParts[0]), statusParts[1]);
            }).ToArray());
        
        #endregion

        //TODO this cant contain spaces
        var commandPrefix = configuration["Config:CommandPrefix"];
        CommandPrefix = string.IsNullOrEmpty(commandPrefix)
            ? "!"
            : commandPrefix;
        
        var statusInterval = configuration["Config:BotStatusUpdateInterval"];
        BotStatusUpdateInterval = string.IsNullOrEmpty(statusInterval)
            ? 5
            : int.Parse(statusInterval);
        
        var versionFetchHours = configuration["Config:VersionFetchHoursInterval"];
        VersionFetchHoursInterval = string.IsNullOrEmpty(versionFetchHours)
            ? 1
            : int.Parse(versionFetchHours);

        var osuApiKey = configuration["Config:OsuApiKey"];
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
    public static readonly ImmutableList<string> DisallowedNames;
    public static readonly ImmutableList<string> OsuDirectSearchEndpoints;
    public static readonly ImmutableList<(Activity Activity, string Description)> BotStatuses;
    public static readonly string OsuDirectDownloadEndpoint;
    public static readonly string WelcomeMessage;
    public static readonly string FirstLoginMessage;
    public static readonly string RestrictedMessage;
    public static readonly int BotStatusUpdateInterval;
    public static readonly int VersionFetchHoursInterval;
    public static readonly string CommandPrefix;
    public static readonly string OsuApiKey;
}