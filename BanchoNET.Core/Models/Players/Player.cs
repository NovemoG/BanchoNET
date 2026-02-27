using System.Collections.Concurrent;
using BanchoNET.Core.Models.Dtos;
using BanchoNET.Core.Models.Multiplayer;
using BanchoNET.Core.Models.Privileges;
using BanchoNET.Core.Models.Scores;
using BanchoNET.Core.Packets;
using BanchoNET.Core.Utils.Extensions;

namespace BanchoNET.Core.Models.Players;

public sealed class User : IUser, IDisposable,
    IEquatable<User>
{
    public bool IsBot { get; set; }
    
    public int Id { get; init; }
    public int OnlineId => Id;
    public Guid SessionId { get; set; }
    public string PasswordHash { get; init; }
    public string? ApiKey { get; set; }
    
    public string Username { get; set; }
    public string SafeName => Username.MakeSafe();
    public string[] PreviousUsernames = [];
    
    private string _countryCodeString = null!;
    public CountryCode CountryCode
    {
        get => Enum.TryParse(_countryCodeString, out CountryCode result) ? result : CountryCode.Unknown;
        set => _countryCodeString = value.ToString();
    }
    
    public Geoloc Geoloc { get; set; }
    public sbyte TimeZone { get; set; }
    
    public ClientDetails? ClientDetails { get; set; }
    public Dictionary<GameMode, ModeStats> Stats { get; } = new();
    public List<int> Friends { get; } = [];
    public List<int> Blocked { get; } = [];
    public List<string> Channels { get; } = [];
    
    public PlayerPrivileges Privileges { get; set; }
    public bool IsRestricted => !Privileges.HasPrivilege(PlayerPrivileges.Unrestricted);
    
    public int LastValidBeatmapId { get; set; }
    public LastNp? LastNp { get; set; }
    public Score? RecentScore { get; set; }
    public PlayerStatus Status { get; } = new();
    
    public bool AppearOffline { get; set; } //TODO
    public string? AwayMessage { get; set; }
    public bool PmFriendsOnly { get; set; }
    public PresenceFilter Presence { get; set; }
    
    public int SupportLevel;
    public DateTime RemainingSupporter { get; set; }
    public bool IsSupporter => RemainingSupporter > DateTime.UtcNow;
    
    public DateTime RemainingSilence { get; set; }
    public bool IsSilenced => RemainingSilence > DateTime.UtcNow;
    
    public bool InLobby { get; set; }
    public MultiplayerMatch? Match { get; set; }
    public bool InMatch => Match != null;
    
    public User? Spectating { get; set; }
    public bool IsSpectating => Spectating != null;
    
    public bool HasSpectators => !_spectators.IsEmpty;
    public IEnumerable<User> Spectators => _spectators.Keys;
    public int SpectatorsCount => _spectators.Count;
    private readonly ConcurrentDictionary<User, bool> _spectators = new();
    public bool AddSpectator(User player) => _spectators.TryAdd(player, true);
    public bool RemoveSpectator(User player) => _spectators.TryAdd(player, true);
    
    public DateTime LoginTime { get; }
    public DateTime LastActivityTime { get; set; }

    public bool IsOnline => IsOnlineOnStable || IsOnlineOnLazer;
    public bool IsOnlineOnStable => SessionId != Guid.Empty;
    public bool IsOnlineOnLazer { get; set; } //TODO

    public User(
        PlayerDto userInfo, 
        Guid? id = null,
        DateTime? loginTime = null,
        sbyte timeZone = 0
    ) {
        Id = userInfo.Id;
        Username = userInfo.Username;
        SessionId = id ?? Guid.Empty;
        PasswordHash = userInfo.PasswordHash;
        LoginTime = loginTime ?? DateTime.UtcNow;
        TimeZone = timeZone;
        Privileges = (PlayerPrivileges)userInfo.Privileges;
        RemainingSilence = userInfo.RemainingSilence;
        RemainingSupporter = userInfo.RemainingSupporter;
        AwayMessage = userInfo.AwayMessage;
        ApiKey = userInfo.ApiKey;
    }
    
    public override string ToString() => Username;
    
    #region IEquatable
    
    public bool Equals(User? other) => this.MatchesOnlineID(other);
    
    public override bool Equals(
        object? obj
    ) {
        return ReferenceEquals(this, obj) || obj is User other && Equals(other);
    }
    
    public override int GetHashCode() => Id.GetHashCode();


    #endregion
    
    #region StablePacketQueue

    private readonly ServerPackets _queue = new();
    private readonly Lock _queueLock = new();
    private bool _logout;
    
    public void Logout() => _logout = true;
    
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
            if (_logout) _queue.Dispose();
            
            return bytes;
        }
    }

    #endregion

    public void Dispose() {
        _queue.Dispose();
    }
}