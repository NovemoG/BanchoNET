namespace BanchoNET.Models;

public class ServerConfig
{
	public string Domain { get; set; } = null!;
	public string BanchoNETVersion { get; set; } = null!;
	public string WelcomeMessage { get; set; } = null!;
	public string FirstLoginMessage { get; set; } = null!;
	public string RestrictedMessage { get; set; } = null!;
	public int VersionFetchHoursDelay { get; set; }
	public bool DisallowOldClients { get; set; }
	public bool SortLeaderboardByPP { get; set; }
	public bool DisplayPPOnLeaderboard { get; set; }
	public bool DisplayScoreOnLeaderboard { get; set; }
	public bool DisplayMissesOnLeaderboard { get; set; }
	public bool SubmitByPP { get; set; }
	public string MenuIconUrl { get; set; } = null!;
	public string MenuOnclickUrl { get; set; } = null!;
	public string CommandPrefix { get; set; } = null!;
	public string OsuApiKey { get; set; } = null!;
}