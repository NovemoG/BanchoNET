using BanchoNET.Core.Models.Beatmaps;
using BanchoNET.Core.Models.Channels;
using BanchoNET.Core.Models.Multiplayer;
using BanchoNET.Core.Models.Players;

namespace BanchoNET.Core.Abstractions.Services;

public interface IBanchoSession
{
    Player BanchoBot { get; }
    IEnumerable<Player> Players { get; }
    IEnumerable<Player> PlayersInLobby { get; }
    IEnumerable<Player> Restricted { get; }
    IEnumerable<Player> Bots { get; }
    
    IEnumerable<Channel> Channels { get; }
    Channel LobbyChannel { get; }
    
    IEnumerable<MultiplayerLobby> Lobbies { get; }

    void ClearPasswordsCache();
    void InsertPasswordHash(string passwordMD5, string passwordHash);
    bool CheckHashes(string passwordMD5, string passwordHash);
    
    void AppendBot(Player bot);
    void AppendPlayer(Player player);
    bool LogoutPlayer(Player player);

    void JoinLobby(Player player);
    void LeaveLobby(Player player);
    
    Player? GetPlayerById(int id);
    Player? GetPlayerByName(string? username);
    Player? GetPlayerByToken(Guid token);
    
    Channel? GetChannel(string name, bool spectator = false);
    void InsertChannel(Channel channel, bool spectator = false);
    
    Beatmap? GetBeatmapByMD5(string beatmapMD5);
    Beatmap? GetBeatmapById(int id);
    BeatmapSet? GetBeatmapSet(int setId);
    void CacheBeatmapSet(BeatmapSet set);
    bool IsBeatmapNotSubmitted(string beatmapMD5);
    void CacheNotSubmittedBeatmap(string beatmapMD5);
    bool BeatmapNeedsUpdate(string beatmapMD5);
    void CacheNeedsUpdateBeatmap(string beatmapMD5);
    
    ushort GetFreeMatchId();
    MultiplayerLobby? GetLobby(ushort id);
    void InsertLobby(MultiplayerLobby lobby);
    void RemoveLobby(MultiplayerLobby lobby);

    void EnqueueToPlayers(byte[] data);
}