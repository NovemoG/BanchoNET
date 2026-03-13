namespace BanchoNET.Core.Models.Api.Beatmaps;

public class SetTag
{
    public int Id { get; set; }
    public string Name { get; set; }
    public int RulesetId { get; set; }
    public string Description { get; set; }
    public DateTimeOffset? CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
}