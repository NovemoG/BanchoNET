using System.Net;

namespace BanchoNET.Core.Models.Players;

public class ClientDetails
{
	public OsuVersion OsuVersion { get; set; }
	public string OsuPathMD5 { get; set; }
	public string AdaptersMD5 { get; set; }
	public string UninstallMD5 { get; set; }
	public string DiskSignatureMD5 { get; set; }
	public List<string> Adapters { get; set; } = [];
	public IPAddress IpAddress { get; set; }

	public string ClientHash => $"{OsuPathMD5}:{string.Join('.', Adapters)}.:{AdaptersMD5}:{UninstallMD5}:{DiskSignatureMD5}:";
}