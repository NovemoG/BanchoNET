using System.Text.Json.Serialization;

namespace BanchoNET.Core.Models.Api.Player;

public class Variant
{
    public required string Mode { get; set; }
    [JsonPropertyName("variant")]
    public required string ModeVariant { get; set; }
    public int CountryRank { get; set; }
    public int GlobalRank { get; set; }
    public float Pp { get; set; }
}