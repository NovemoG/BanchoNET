namespace BanchoNET.Core.Models.Api.Player;

public class StatisticsRulesets
{
    public Statistics Osu { get; set; } = new();
    public Statistics Taiko { get; set; } = new();
    public Statistics Fruits { get; set; } = new();
    public Statistics Mania { get; set; } = new();
}