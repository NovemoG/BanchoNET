namespace BanchoNET.Core.Models.Api.Player;

public class MatchmakingStats //TODO
{
    public int FirstPlacements { get; set; }
    public bool IsRatingProvisional { get; set; } = true;
    public int Plays { get; set; }
    public int PoolId { get; set; } = 1;
    public int Rank { get; set; }
    public int Rating { get; set; }
    public int TotalPoints { get; set; }
    public int UserId { get; set; }
    public Pool Pool { get; set; } = new();
}