using BanchoNET.Core.Abstractions.HubClients.Multiplayer;
using BanchoNET.Core.Abstractions.HubClients.Multiplayer.MultiplayerRooms;
using BanchoNET.Core.Models.Api.Scores;
using BanchoNET.Core.Models.Beatmaps;
using BanchoNET.Core.Utils.Extensions;
using MessagePack;
using MatchType = BanchoNET.Core.Abstractions.HubClients.Multiplayer.Match.MatchType;

namespace BanchoNET.Core.Abstractions.HubClients.Metadata;

[Serializable]
[MessagePackObject]
[Union(11, typeof(ChoosingBeatmap))]
[Union(12, typeof(InSoloGame))]
[Union(13, typeof(WatchingReplay))]
[Union(14, typeof(SpectatingUser))]
[Union(21, typeof(SearchingForLobby))]
[Union(22, typeof(InLobby))]
[Union(23, typeof(InMultiplayerGame))]
[Union(24, typeof(SpectatingMultiplayerGame))]
[Union(31, typeof(InPlaylistGame))]
[Union(41, typeof(EditingBeatmap))]
[Union(42, typeof(ModdingBeatmap))]
[Union(43, typeof(TestingBeatmap))]
[Union(51, typeof(InDailyChallengeLobby))]
[Union(52, typeof(PlayingDailyChallenge))]
public abstract class UserActivity
{
    public abstract string GetStatus(bool hideIdentifiableInformation = false);
    public virtual string? GetDetails(bool hideIdentifiableInformation = false) => null;
    
    public virtual int? GetBeatmapID(bool hideIdentifiableInformation = false) => null;

    [MessagePackObject]
    public class ChoosingBeatmap : UserActivity
    {
        public override string GetStatus(bool hideIdentifiableInformation = false) => "Choosing a beatmap";
    }

    [MessagePackObject]
    [Union(12, typeof(InSoloGame))]
    [Union(23, typeof(InMultiplayerGame))]
    [Union(24, typeof(SpectatingMultiplayerGame))]
    [Union(31, typeof(InPlaylistGame))]
    [Union(52, typeof(PlayingDailyChallenge))]
    public abstract class InGame : UserActivity
    {
        [Key(0)]
        public int BeatmapID { get; set; }
        
        [Key(1)]
        public string BeatmapDisplayTitle { get; set; } = string.Empty;
        
        [Key(2)]
        public int RulesetID { get; set; }
        
        [Key(3)]
        public string RulesetPlayingVerb { get; set; } = string.Empty;

        protected InGame(
            Beatmap beatmap,
            int rulesetId
        ) {
            BeatmapID = beatmap.OnlineId;
            BeatmapDisplayTitle = beatmap.DisplayTitle();
            
            RulesetID = rulesetId;
            RulesetPlayingVerb = rulesetId.PlayingVerb();
        }
        
        [SerializationConstructor]
        protected InGame() { }

        public override string GetStatus(bool hideIdentifiableInformation = false) => RulesetPlayingVerb;
        public override string GetDetails(bool hideIdentifiableInformation = false) => BeatmapDisplayTitle;
        public override int? GetBeatmapID(bool hideIdentifiableInformation = false) => BeatmapID;
    }

    [MessagePackObject]
    public class InSoloGame : InGame
    {
        public InSoloGame(Beatmap beatmap, int rulesetId) : base(beatmap, rulesetId) { }
        
        [SerializationConstructor]
        public InSoloGame() { }
    }

    [MessagePackObject]
    public class InMultiplayerGame : InGame
    {
        public InMultiplayerGame(Beatmap beatmap, int rulesetId) : base(beatmap, rulesetId) { }
        
        [SerializationConstructor]
        public InMultiplayerGame() { }
        
        public override string GetStatus(bool hideIdentifiableInformation = false) => $"{base.GetStatus(hideIdentifiableInformation)} with others";
    }

    [MessagePackObject]
    public class InPlaylistGame : InGame
    {
        public InPlaylistGame(Beatmap beatmap, int rulesetId) : base(beatmap, rulesetId) { }
        
        [SerializationConstructor]
        public InPlaylistGame() { }
    }

    [MessagePackObject]
    public class EditingBeatmap : UserActivity
    {
        [Key(0)]
        public int BeatmapID { get; set; }
        
        [Key(1)]
        public string BeatmapDisplayTitle { get; set; } = string.Empty;

        public EditingBeatmap(
            Beatmap beatmap
        ) {
            BeatmapID = beatmap.OnlineId;
            BeatmapDisplayTitle = beatmap.DisplayTitle();
        }
        
        [SerializationConstructor]
        public EditingBeatmap() { }
        
        public override string GetStatus(bool hideIdentifiableInformation = false) => "Editing a beatmap";
        
        public override string GetDetails(bool hideIdentifiableInformation = false) => hideIdentifiableInformation
            ? string.Empty
            : BeatmapDisplayTitle;
        
        public override int? GetBeatmapID(bool hideIdentifiableInformation = false) => hideIdentifiableInformation
            ? null
            : BeatmapID;
    }

    [MessagePackObject]
    public class TestingBeatmap : EditingBeatmap
    {
        public TestingBeatmap(Beatmap beatmap) : base(beatmap) { }
        
        [SerializationConstructor]
        public TestingBeatmap() { }
        
        public override string GetStatus(bool hideIdentifiableInformation = false) => "Testing a beatmap";
    }

    [MessagePackObject]
    public class ModdingBeatmap : EditingBeatmap
    {
        public ModdingBeatmap(Beatmap beatmap) : base(beatmap) { }
        
        [SerializationConstructor]
        public ModdingBeatmap() { }
        
        public override string GetStatus(bool hideIdentifiableInformation = false) => "Modding a beatmap";
    }

    [MessagePackObject]
    public class WatchingReplay : UserActivity
    {
        [Key(0)]
        public long ScoreID { get; set; }
        
        [Key(1)]
        public string PlayerName { get; set; } = string.Empty;
        
        [Key(2)]
        public int BeatmapID { get; set; }
        
        [Key(3)]
        public string? BeatmapDisplayTitle { get; set; }

        public WatchingReplay(
            ApiScore score,
            Beatmap beatmap
        ) {
            ScoreID = score.Id;
            PlayerName = score.User?.Username ?? "Unknown";
            BeatmapID = beatmap.OnlineId;
            BeatmapDisplayTitle = beatmap.DisplayTitle();
        }
        
        [SerializationConstructor]
        public WatchingReplay() { }
        
        public override string GetStatus(bool hideIdentifiableInformation = false) => hideIdentifiableInformation
            ? "Watching a replay"
            : $"Watching {PlayerName}'s replay";
        
        public override string? GetDetails(bool hideIdentifiableInformation = false) => BeatmapDisplayTitle;
    }

    [MessagePackObject]
    public class SpectatingUser : WatchingReplay
    {
        public SpectatingUser(ApiScore score, Beatmap beatmap) : base(score, beatmap) { }
        
        [SerializationConstructor]
        public SpectatingUser() { }
        
        public override string GetStatus(bool hideIdentifiableInformation = false) => hideIdentifiableInformation
            ? "Spectating a user"
            : $"Spectating {PlayerName}";
    }

    [MessagePackObject]
    public class SpectatingMultiplayerGame : InGame
    {
        public SpectatingMultiplayerGame(Beatmap beatmap, int rulesetId) : base(beatmap, rulesetId) { }
        
        [SerializationConstructor]
        public SpectatingMultiplayerGame() { }
        
        public override string GetStatus(bool hideIdentifiableInformation = false) => "Spectating a multiplayer game";
    }

    [MessagePackObject]
    public class SearchingForLobby : UserActivity
    {
        public override string GetStatus(bool hideIdentifiableInformation = false) => "Looking for a lobby";
    }

    [MessagePackObject]
    public class InLobby : UserActivity
    {
        [Key(0)]
        public long RoomID { get; set; }
        
        [Key(1)]
        public string RoomName { get; set; } = string.Empty;

        public InLobby(
            Room room
        ) {
            RoomID = room.RoomId;
            RoomName = room.Name;
        }

        public InLobby(
            MultiplayerRoom room
        ) {
            switch (room.Settings.MatchType)
            {
                case MatchType.Matchmaking:
                    RoomID = -1;
                    RoomName = "Quick Play";
                    break;
                
                case MatchType.RankedPlay:
                    RoomID = -1;
                    RoomName = "Ranked Play";
                    break;
                
                default:
                    RoomID = room.RoomId;
                    RoomName = room.Name;
                    break;
            }
        }
        
        [SerializationConstructor]
        public InLobby() { }
        
        public override string GetStatus(bool hideIdentifiableInformation = false) => "In a lobby";
        
        public override string? GetDetails(bool hideIdentifiableInformation = false) => hideIdentifiableInformation
            ? null
            : RoomName;
    }

    [MessagePackObject]
    public class InDailyChallengeLobby : UserActivity
    {
        [SerializationConstructor]
        public InDailyChallengeLobby() { }
        
        public override string GetStatus(bool hideIdentifiableInformation = false) => "In daily challenge lobby";
    }

    [MessagePackObject]
    public class PlayingDailyChallenge : InGame
    {
        public PlayingDailyChallenge(Beatmap beatmap, int rulesetId) : base(beatmap, rulesetId) { }
        
        [SerializationConstructor]
        public PlayingDailyChallenge() { }
        
        public override string GetStatus(bool hideIdentifiableInformation = false) => $"{RulesetPlayingVerb} in daily challenge";
    }
}