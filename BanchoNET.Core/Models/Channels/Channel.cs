using System.Collections.Concurrent;
using BanchoNET.Core.Models.Dtos;
using BanchoNET.Core.Models.Players;
using BanchoNET.Core.Models.Privileges;
using BanchoNET.Core.Utils.Extensions;

namespace BanchoNET.Core.Models.Channels;

public class Channel : IChannel,
	IEquatable<Channel>
{
	public Channel(
		string name,
		long id,
		ChannelType type = ChannelType.Public
	) {
		Id = id;
		IdName = name;
		
		if (name.StartsWith("#multi_"))
		{
			Name = "#multiplayer";
			Type = type;
		}
		else if (name.StartsWith("#s_"))
		{
			Name = "#spectator";
			Spectator = true;
			Type = type;
		}
		else Name = name;
	}

	public Channel(ChannelDto channel)
	{
		Id = channel.Id;
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
		Type = channel.Type; //TODO
		
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
	public bool Spectator { get; set; }
	public ChannelType Type { get; set; } = ChannelType.Public;
	public ClientPrivileges ReadPrivileges { get; set; } = ClientPrivileges.Player;
	public ClientPrivileges WritePrivileges { get; set; } = ClientPrivileges.Player;

	public readonly long Id;
	public string OnlineId => IdName;
	public string IdName { get; }
	public string Name { get; }
	public string Description { get; set; } = string.Empty;
	public long? LastMessageId { get; set; }
	
	private readonly ConcurrentDictionary<Player, bool> _players = [];
	public IEnumerable<Player> Players => _players.Keys;
	public int PlayersCount => _players.Count;
	public void AddPlayer(Player player) => _players.TryAdd(player, false);
	public void RemovePlayer(Player player) => _players.TryRemove(player, out _);

	#region IEquatable

	public bool Equals(Channel? other) => this.MatchesOnlineID(other);

	public override bool Equals(
		object? obj
	) {
		return ReferenceEquals(this, obj) || obj is Channel other && Equals(other);
	}

	public override int GetHashCode() => IdName.GetHashCode();

	#endregion
}