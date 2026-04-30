using BanchoNET.Core.Models.Api.Scores;
using BanchoNET.Core.Models.Beatmaps;
using BanchoNET.Core.Models.Lazer.Spectator;
using BanchoNET.Core.Models.Lazer.Spectator.Frames;

namespace BanchoNET.Core.Models.Api;

public class ClientSpectatorState
{
    public SpectatorState? State { get; set; }
    public long? ScoreToken { get; set; }
    public ApiScore? Score { get; set; }
    public BeatmapStatus BeatmapStatus { get; set; }
    public List<LegacyReplayFrame> Frames { get; } = [];
    
    public DateTime SubmitTime { get; set; }
}