namespace BanchoNET.Models;

public class ServerConfig
{
	public string Domain { get; set; } = null!;
	public string BanchoNETVersion { get; set; } = null!;
	public string WelcomeMessage { get; set; } = null!;
	public string FirstLoginMessage { get; set; } = null!;
	public string RestrictedMessage { get; set; } = null!;
	public bool DisallowOldClients { get; set; }
	public string MenuIconUrl { get; set; } = null!;
	public string MenuOnclickUrl { get; set; } = null!;
	public string CommandPrefix { get; set; } = null!;
}