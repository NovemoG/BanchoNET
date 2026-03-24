using System.Text.Json.Serialization;

namespace BanchoNET.Core.Models.Api.Beatmaps;

public class ScoreRequestDto
{
    public required string version_hash { get; set; }
    public required string beatmap_hash { get; set; }
    public int ruleset_id { get; set; }
}