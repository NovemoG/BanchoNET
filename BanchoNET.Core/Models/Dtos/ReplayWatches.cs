namespace BanchoNET.Core.Models.Dtos;

public class ReplayWatches
{
    public int PlayerId { get; set; }
    public PlayerDto Player { get; set; } = null!;
    
    public long ScoreId { get; set; }
    public ScoreDto Score { get; set; } = null!;
    
    public int Count { get; set; }
}