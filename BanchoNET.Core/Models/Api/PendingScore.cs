using BanchoNET.Core.Models.Api.Beatmaps;
using BanchoNET.Core.Models.Api.Scores;
using BanchoNET.Core.Models.Scores;

namespace BanchoNET.Core.Models.Api;

public sealed class PendingScore
{
    public required ScoreResponseDto Response { get; set; }
    public ApiScore? Score { get; set; }
    
    public SubmissionStatus Status { get; set; }
    public SubmissionStatus TotalScoreStatus { get; set; }
    public double ClockRate { get; set; }
    public int[] Pauses { get; set; } = [];
    
    public void CaptureInternalState()
    {
        if (Score == null) return;

        Status = Score.Status;
        TotalScoreStatus = Score.TotalScoreStatus;
        ClockRate = Score.ClockRate;
        Pauses = Score.Pauses;
    }
    
    public void RestoreInternalState()
    {
        if (Score == null) return;

        Score.Status = Status;
        Score.TotalScoreStatus = TotalScoreStatus;
        Score.ClockRate = ClockRate;
        Score.Pauses = Pauses;
    }
}