﻿using BanchoNET.Models.Dtos;
using BanchoNET.Objects.Channels;
using BanchoNET.Objects.Multiplayer;
using BanchoNET.Objects.Scores;
using BanchoNET.Packets;
using BanchoNET.Utils;
using static BanchoNET.Objects.Privileges.Privileges;

namespace BanchoNET.Objects.Players;

public class Player
{
	private ServerPackets? _queue;
	
	public readonly int Id;
	public readonly Guid Token;
	
	public string Username { get; set; }
	public string SafeName { get; set; }
	public string LoginName { get; set; }
	public string PasswordHash { get; set; }
	public Privileges.Privileges Privileges { get; set; }
	public Player? Spectating { get; set; }
	public MultiplayerLobby? Lobby { get; set; }
	public bool InLobby { get; set; }
	public Geoloc Geoloc { get; set; }
	public sbyte TimeZone { get; set; }
	public bool PmFriendsOnly { get; set; }
	public DateTime RemainingSilence { get; set; }
	public DateTime RemainingSupporter { get; set; }
	public DateTime LoginTime { get; set; }
	public DateTime LastActivityTime { get; set; }
	public string? AwayMessage { get; set; }
	public bool IsBot { get; set; }
	public bool Stealth { get; set; } //TODO
	
	public ClientDetails ClientDetails { get; set; }
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
	public bool Silenced => RemainingSilence > DateTime.Now;
	public bool Supporter => RemainingSupporter > DateTime.Now;
	public bool Restricted => !Privileges.HasPrivilege(Unrestricted);
	public bool IsSpectating => Spectating != null;
	public bool HasSpectators => Spectators.Count > 0;
	
	public string? ApiKey { get; set; }
	
	public Player(PlayerDto playerData, Guid token = new(), DateTime? loginTime = null, sbyte timeZone = 0)
	{
		Id = playerData.Id;
		
		Token = token != Guid.Empty ? token : Guid.Empty;
		
		LoginTime = loginTime ?? DateTime.Now;
		
		Username = playerData.Username;
		SafeName = playerData.SafeName;
		LoginName = playerData.LoginName;
		PasswordHash = playerData.PasswordHash;
		Privileges = (Privileges.Privileges)playerData.Privileges;
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
		_queue ??= new ServerPackets();
		_queue.WriteBytes(dataBytes);
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