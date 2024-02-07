using BanchoNET.Models.Dtos;
using BanchoNET.Objects.Channels;
using BanchoNET.Objects.Multiplayer;
using BanchoNET.Packets;
using BanchoNET.Utils;
using static BanchoNET.Objects.Privileges.Privileges;

namespace BanchoNET.Objects.Players;

public class Player
{
	private ServerPackets? _queue;
	public ServerPackets Queue
	{
		get
		{
			return _queue ??= new ServerPackets();
		}
	}
	
	public readonly int Id;
	public readonly Guid Token;
	
	public string Username { get; set; }
	public string SafeName { get; set; }
	public string LoginName { get; set; }
	public string PasswordHash { get; set; }
	public Privileges.Privileges Privileges { get; set; }
	public Player? Spectating { get; set; }
	public MultiplayerLobby? Lobby { get; set; }
	public Geoloc Geoloc { get; set; }
	public byte TimeZone { get; set; }
	public bool PmFriendsOnly { get; set; }
	public int RemainingSilence { get; set; }
	public int RemainingSupporter { get; set; }
	public DateTime LoginTime { get; set; }
	public DateTime LastActivityTime { get; set; }
	public string AwayMessage { get; set; }
	public bool Stealth { get; set; }
	
	public PresenceFilter PresenceFilter { get; set; }
	public PlayerStatus Status { get; set; }
	public Dictionary<GameMode, ModeStats> Stats { get; set; }
	
	public List<int> Friends { get; }
	public List<int> Blocked { get; }
	public List<Channel> Channels { get; }
	public List<Player> Spectators { get; }
	
	//public Club Club { get; set; }
	//public int ClubPrivileges { get; set; }

	public bool Online => Token != Guid.Empty;
	public bool InLobby => Lobby != null;
	public bool Silenced => RemainingSilence > 0;
	public bool Restricted => !Privileges.HasPrivilege(Unrestricted);
	public bool IsSpectating => Spectating != null;
	public bool HasSpectators => Spectators.Count > 0;
	
	public string? ApiKey { get; set; }
	
	public Player(PlayerDto playerData, string token = "", DateTime? loginTime = null, byte timeZone = 0)
	{
		Id = playerData.Id;
		
		var isValid = Guid.TryParse(token, out var tokenOut);
		Token = isValid ? tokenOut : Guid.NewGuid();
		
		LoginTime = loginTime ?? DateTime.UtcNow;
		
		Username = playerData.Username;
		SafeName = playerData.SafeName;
		LoginName = playerData.LoginName;
		PasswordHash = playerData.PasswordHash;
		Privileges = (Privileges.Privileges)playerData.Privileges;
		TimeZone = timeZone;
		RemainingSilence = playerData.RemainingSilence;
		RemainingSupporter = playerData.RemainingSupporter;
		AwayMessage = "";

		Status = new PlayerStatus
		{
			Activity = Activity.Idle,
			ActivityDescription = "",
			BeatmapMD5 = "",
			CurrentMods = Mods.None,
			Mode = GameMode.VanillaStd,
			BeatmapId = 0
		};

		Stats = [];
		Friends = [];
		Blocked = [];
		Channels = [];
		Spectators = [];
	}

	public byte[] Dequeue()
	{
		if (_queue == null) return [];

		var bytes = _queue.GetContent();
		
		_queue.Dispose();
		_queue = null;

		return bytes;
	}
}