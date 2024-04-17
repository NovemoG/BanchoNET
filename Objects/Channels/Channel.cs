using BanchoNET.Objects.Players;
using BanchoNET.Objects.Privileges;

namespace BanchoNET.Objects.Channels;

public class Channel
{
	public Channel(string name)
	{
		IdName = name;
		
		if (name.StartsWith("#multi_"))
			Name = "#multiplayer";
		else if (name.StartsWith("#s_"))
			Name = "#spectator";
		else Name = name;
	}
	
	public bool AutoJoin { get; set; }
	public bool Hidden { get; set; }
	public bool ReadOnly { get; set; }
	public bool Instance { get; set; }
	public ClientPrivileges ReadPrivileges { get; set; } = ClientPrivileges.Player;
	public ClientPrivileges WritePrivileges { get; set; } = ClientPrivileges.Player;

	public string IdName { get; }
	public string Name { get; }
	public required string Description { get; set; }
	public List<Player> Players { get; set; } = [];
}