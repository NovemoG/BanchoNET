using System.Collections.Concurrent;
using BanchoNET.Models.Dtos;
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

	public Channel(ChannelDto channel)
	{
		IdName = Name = channel.Name;

		if (Name.StartsWith("#multi_") || Name.StartsWith("#s_"))
		{
			Logger.Shared.LogWarning("Channel name cannot start with '#multi_' or '#s_', skipping.", caller: "ChannelInit");
			return;
		}
		
		Description = channel.Description;
		AutoJoin = channel.AutoJoin;
		Hidden = channel.Hidden;
		ReadOnly = channel.ReadOnly;
		
		if (Enum.TryParse<ClientPrivileges>(channel.ReadPrivileges.ToString(), out var readPrivileges))
			ReadPrivileges = readPrivileges;
		else Logger.Shared.LogWarning($"Wrong read privileges for channel {Name}, setting default one (Player).", caller: "ChannelInit");
		
		if (Enum.TryParse<ClientPrivileges>(channel.WritePrivileges.ToString(), out var writePrivileges))
			WritePrivileges = writePrivileges;
		else Logger.Shared.LogWarning($"Wrong write privileges for channel {Name}, setting default one (Player).", caller: "ChannelInit"); 
	}
	
	public bool AutoJoin { get; set; }
	public bool Hidden { get; set; }
	public bool ReadOnly { get; set; }
	public bool Instance { get; set; }
	public ClientPrivileges ReadPrivileges { get; set; } = ClientPrivileges.Player;
	public ClientPrivileges WritePrivileges { get; set; } = ClientPrivileges.Player;

	public string IdName { get; }
	public string Name { get; }
	public string Description { get; set; } = null!;
	
	private readonly ConcurrentDictionary<Player, bool> _players = [];
	public IEnumerable<Player> Players => _players.Keys;
	public int PlayersCount => _players.Count;
	public void AddPlayer(Player player) => _players.TryAdd(player, false);
	public void RemovePlayer(Player player) => _players.TryRemove(player, out _);
}