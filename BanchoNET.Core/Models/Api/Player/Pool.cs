namespace BanchoNET.Core.Models.Api.Player;

public class Pool //TODO
{
    public bool Active { get; set; }
    public int Id { get; set; } = 1;
    public string Name { get; set; } = "osu!";
    public int RulesetId { get; set; }
    public int VariantId { get; set; }
}