namespace BanchoNET.Core.Models.Api.Player;

public class DailyChallengeUserStats
{
    public int DailyStreakBest { get; set; }
    public int DailyStreakCurrent { get; set; }
    public DateTimeOffset LastUpdate { get; set; }
    public DateTimeOffset LastWeeklyStreak { get; set; }
    public int Playcount { get; set; }
    public int Top10PPlacements { get; set; }
    public int Top50PPlacements { get; set; }
    public int UserId { get; set; }
    public int WeeklyStreakBest { get; set; }
    public int WeeklyStreakCurrent { get; set; }
}