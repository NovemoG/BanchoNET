namespace BanchoNET.Core.Models.Dtos;

public class CountryRankingDto
{
	public string Code { get; set; } = null!;
	public int ActiveUsers { get; set; }
	public long PlayCount { get; set; }
	public long RankedScore { get; set; }
	public long Performance { get; set; }
}