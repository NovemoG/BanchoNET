using BanchoNET.Core.Models.Api.Scores;
using BanchoNET.Core.Models.Beatmaps;
using BanchoNET.Core.Models.Lazer.Spectator;

namespace BanchoNET.Core.Models.Api;

public class ClientSpectatorState
{
    public SpectatorState? State { get; set; }
    public long? ScoreToken { get; set; }
    public ApiScore? Score { get; set; }
    public BeatmapStatus BeatmapStatus { get; set; }
    
    public DateTime SubmitTime { get; set; }
}