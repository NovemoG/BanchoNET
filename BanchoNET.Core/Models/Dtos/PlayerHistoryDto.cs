using BanchoNET.Core.Models.History;

namespace BanchoNET.Core.Models.Dtos;

public class PlayerHistoryDto
{
	public int PlayerId { get; set; }
	public byte Mode { get; set; }
	public HistoryMetric Metric { get; set; }
	public HistoryGranularity Granularity { get; set; }
	
	public DateOnly Date { get; set; }
	
	public double Value { get; set; }
}