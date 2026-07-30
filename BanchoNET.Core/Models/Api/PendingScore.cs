using BanchoNET.Core.Models.Api.Beatmaps;
using BanchoNET.Core.Models.Api.Scores;

namespace BanchoNET.Core.Models.Api;

public sealed class PendingScore
{
    public required ScoreResponseDto Response { get; set; }
    public ApiScore? Score { get; set; }
    
    public double ClockRate { get; set; }
    public int[] Pauses { get; set; } = [];
    
    public void CaptureInternalState()
    {
        if (Score == null) return;

        ClockRate = Score.ClockRate;
        Pauses = Score.Pauses;
    }
    
    public void RestoreInternalState()
    {
        if (Score == null) return;

        Score.ClockRate = ClockRate;
        Score.Pauses = Pauses;
    }
}