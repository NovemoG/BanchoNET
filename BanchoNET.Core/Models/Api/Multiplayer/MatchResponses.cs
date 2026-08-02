using System.Text.Json.Serialization;
using BanchoNET.Core.Models.Api.Player;
using BanchoNET.Core.Models.Multiplayer;
using BanchoNET.Core.Models.Scores;
using BanchoNET.Core.Models.Stable.Multiplayer;

namespace BanchoNET.Core.Models.Api.Multiplayer;

/// <summary>
/// A match as it appears in a list, without its event stream.
/// </summary>
public sealed class MatchSummaryResponse
{
    public long Id { get; set; }
    public string Name { get; set; } = null!;
    public int? HostId { get; set; }
    public string Mode { get; set; } = "osu";

    public DateTimeOffset StartTime { get; set; }
    public DateTimeOffset? EndTime { get; set; }
    
    public bool InProgress => EndTime == null;

    public int ParticipantCount { get; set; }
}

public sealed class MatchListResponse
{
    public List<MatchSummaryResponse> Matches { get; set; } = [];

    /// <summary>
    /// Id to pass back as <c>before</c> for the next page, or null at the end of the list.
    /// </summary>
    public long? NextCursor { get; set; }
}

public sealed class PlayerMatchListResponse
{
    public List<MatchSummaryResponse> Matches { get; set; } = [];
    public int Total { get; set; }
}

/// <summary>
/// A whole match: its participants and every event in the order it happened, with games appearing
/// in the stream where they were played.
/// </summary>
public sealed class MatchResponse
{
    public MatchSummaryResponse Match { get; set; } = null!;
    public List<BasicApiPlayer> Users { get; set; } = [];
    public List<MatchEventResponse> Events { get; set; } = [];
}

public sealed class MatchEventResponse
{
    public long Id { get; set; }
    public MultiplayerEventType Type { get; set; }
    public int? UserId { get; set; }
    public DateTimeOffset CreatedAt { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public MatchGameResponse? Game { get; set; }
}

public sealed class MatchGameResponse
{
    public long Id { get; set; }

    public int? BeatmapId { get; set; }
    public string BeatmapMD5 { get; set; } = null!;
    public string BeatmapName { get; set; } = null!;

    public string Mode { get; set; } = "osu";
    public WinCondition WinCondition { get; set; }
    public LobbyType LobbyType { get; set; }
    public int Mods { get; set; }

    public DateTimeOffset StartTime { get; set; }
    public DateTimeOffset? EndTime { get; set; }

    public bool Aborted { get; set; }

    /// <summary>
    /// The lobby moved on before every client reported in. Scores submitted late are still here.
    /// </summary>
    public bool ForceCompleted { get; set; }

    public List<MatchScoreResponse> Scores { get; set; } = [];
}

public sealed class MatchScoreResponse
{
    public int UserId { get; set; }

    /// <summary>
    /// Null once retention has removed the underlying score; the result below still stands.
    /// </summary>
    public long? ScoreId { get; set; }

    public LobbyTeams Team { get; set; }

    public long TotalScore { get; set; }
    public int MaxCombo { get; set; }
    public float Accuracy { get; set; }
    public Grade Grade { get; set; }
    public int Mods { get; set; }

    public int Count300 { get; set; }
    public int Count100 { get; set; }
    public int Count50 { get; set; }
    public int CountGeki { get; set; }
    public int CountKatu { get; set; }
    public int CountMiss { get; set; }

    public bool Failed { get; set; }
}