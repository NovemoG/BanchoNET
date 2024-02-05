namespace BanchoNET.Models;

public class ServerConfig
{
	public string WelcomeMessage { get; set; } = null!;
	public string RestrictedMessage { get; set; } = null!;
	public bool DisallowOldClients { get; set; }
	public string MenuIconUrl { get; set; } = null!;
	public string MenuOnclickUrl { get; set; } = null!;
}