namespace BanchoNET.Core.Models.Dtos;

public class PlayerRankingDto
{
    public required StatsDto Stats { get; set; }
    public required PlayerDto Player { get; set; }
    public int RankChangeSince30Days { get; set; }
}