using BanchoNET.Core.Models.Channels;
using BanchoNET.Core.Models.Dtos;
using BanchoNET.Core.Models.Multiplayer;
using BanchoNET.Core.Models.Privileges;
using BanchoNET.Core.Models.Scores;
using BanchoNET.Core.Packets;
using BanchoNET.Core.Utils.Extensions;
using static BanchoNET.Core.Models.Privileges.PlayerPrivileges;

namespace BanchoNET.Core.Models.Players;

public sealed class Player
{
	private readonly ServerPackets _queue = new();
	private readonly Lock _queueLock = new();
	
	private bool _logout;
	
	public readonly int Id;
	public readonly Guid Token;
	
	public string Username { get; set; }
	public string SafeName { get; set; }
	public string LoginName { get; set; }
	public string PasswordHash { get; set; }
	public PlayerPrivileges Privileges { get; set; }
	public Player? Spectating { get; set; }
	public MultiplayerMatch? Lobby { get; set; }
	public bool InLobby { get; set; }
	public Geoloc Geoloc { get; init; }
	public sbyte TimeZone { get; set; }
	public bool PmFriendsOnly { get; set; }
	public DateTime RemainingSilence { get; set; }
	public DateTime RemainingSupporter { get; set; }
	public DateTime LoginTime { get; set; }
	public DateTime LastActivityTime { get; set; }
	public string? AwayMessage { get; set; }
	public bool IsBot { get; set; }
	public bool Stealth { get; set; } //TODO

	public ClientDetails ClientDetails { get; set; } = null!;
	public PresenceFilter PresenceFilter { get; set; }
	public PlayerStatus Status { get; }
	public Dictionary<GameMode, ModeStats> Stats { get; }
	
	public List<int> Friends { get; }
	public List<int> Blocked { get; }
	public List<Channel> Channels { get; }
	public List<Player> Spectators { get; }
	
	public int LastValidBeatmapId { get; set; }
	public LastNp? LastNp { get; set; }
	public Score? RecentScore { get; set; }
	
	//public Club Club { get; set; }
	//public int ClubPrivileges { get; set; }

	public bool Online => Token != Guid.Empty;
	public bool InMatch => Lobby != null;
	public bool Silenced => RemainingSilence > DateTime.UtcNow;
	public bool Supporter => RemainingSupporter > DateTime.UtcNow;
	public bool Restricted => !Privileges.HasPrivilege(Unrestricted);
	public bool IsSpectating => Spectating != null;
	public bool HasSpectators => Spectators.Count > 0;
	
	public string? ApiKey { get; set; }
	
	public Player(
		PlayerDto playerData,
		Guid token = new(),
		DateTime? loginTime = null,
		sbyte timeZone = 0)
	{
		Id = playerData.Id;
		
		Token = token != Guid.Empty ? token : Guid.Empty;
		
		LoginTime = loginTime ?? DateTime.UtcNow;
		
		Username = playerData.Username;
		SafeName = playerData.SafeName;
		LoginName = playerData.LoginName;
		PasswordHash = playerData.PasswordHash;
		Privileges = (PlayerPrivileges)playerData.Privileges;
		TimeZone = timeZone;
		RemainingSilence = playerData.RemainingSilence;
		RemainingSupporter = playerData.RemainingSupporter;
		AwayMessage = playerData.AwayMessage;
		ApiKey = playerData.ApiKey;

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
	
	public void Enqueue(byte[] dataBytes)
	{
		lock (_queueLock)
			_queue.WriteBytes(dataBytes);
	}

	public byte[] Dequeue()
	{
		lock (_queueLock)
		{
			var bytes = _queue.GetContent();
			
			_queue.Clear();
			
			if (_logout)
				_queue.Dispose();

			return bytes;
		}
	}

	public void Logout() => _logout = true;
}