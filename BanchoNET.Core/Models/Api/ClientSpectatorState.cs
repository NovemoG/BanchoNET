using BanchoNET.Core.Abstractions.HubClients.Spectator;
using BanchoNET.Core.Abstractions.HubClients.Spectator.Frames;
using BanchoNET.Core.Models.Api.Scores;
using BanchoNET.Core.Models.Beatmaps;

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