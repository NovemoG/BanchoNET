using BanchoNET.Objects.Players;
using BanchoNET.Objects.Privileges;

namespace BanchoNET.Objects.Channels;

public class Channel
{
	public int Id { get; set; }
	public bool AutoJoin { get; set; }
	public bool Hidden { get; set; }
	public bool ReadOnly { get; set; }
	public bool Instance { get; set; }
	public ClientPrivileges ReadPrivileges { get; set; }
	public ClientPrivileges WritePrivileges { get; set; }

	public string Name { get; set; }
	public string Description { get; set; }
	public List<Player> Players { get; set; } = [];
}