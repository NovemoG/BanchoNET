using BanchoNET.Core.Models.Multiplayer;
using BanchoNET.Core.Models.Players;
using BanchoNET.Core.Models.Privileges;
using BanchoNET.Core.Models.Scores;
using BanchoNET.Core.Packets;
using BanchoNET.Core.Utils.Extensions;

namespace BanchoNET.Core.Models.Users;

public sealed class User : IUser, IDisposable,
    IEquatable<User>
{
    public User(
        Guid? id = null,
        DateTime? loginTime = null
    ) {
        SessionId = id ?? Guid.Empty;
        LoginTime = loginTime ?? DateTime.UtcNow;
    }
    
    public bool IsBot { get; set; }
    
    public int Id { get; init; } = 1;
    public int OnlineId => Id;
    public Guid SessionId { get; set; }
    
    public string Username { get; set; } = string.Empty;
    public string SafeName => Username.MakeSafe();
    public string[] PreviousUsernames = [];
    
    private string _countryCodeString = null!;
    public CountryCode CountryCode
    {
        get => Enum.TryParse(_countryCodeString, out CountryCode result) ? result : CountryCode.Unknown;
        set => _countryCodeString = value.ToString();
    }
    
    public Geoloc Geoloc { get; set; }
    public ClientDetails? ClientDetails { get; set; }
    public Dictionary<GameMode, ModeStats> Stats { get; } = new();
    public List<int> Friends { get; } = [];
    public List<int> Blocked { get; } = [];
    
    public PlayerPrivileges Privileges { get; set; }
    public bool IsRestricted => !Privileges.HasPrivilege(PlayerPrivileges.Unrestricted);
    
    public int LastValidBeatmapId { get; set; }
    public LastNp? LastNp { get; set; }
    public Score? RecentScore { get; set; }
    public PlayerStatus Status { get; } = new();
    
    public bool AppearOffline { get; set; } //TODO
    public string AwayMessage { get; set; } = string.Empty;
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
    public List<User> Spectators { get; } = [];
    public bool HasSpectators => Spectators.Count > 0;
    
    public DateTime LoginTime { get; }
    public DateTime LastActivityTime { get; set; }
    
    public bool IsOnlineOnStable => SessionId != Guid.Empty;
    public bool IsOnlineOnLazer { get; set; } //TODO
    
    public override string ToString() => Username;
    
    public bool Equals(User? other) => this.MatchesOnlineID(other);
    public override int GetHashCode() => Id.GetHashCode();

    #region StablePacketQueue

    private readonly ServerPackets _queue = new();
    private readonly Lock _queueLock = new();
    
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
            return bytes;
        }
    }

    #endregion

    public void Dispose() {
        _queue.Dispose();
    }
}