using System.Text.Json.Serialization;

namespace BanchoNET.Core.Models.Api.Player;

public class Badge
{
    public DateTimeOffset AwardedAt { get; set; }
    public required string Description { get; set; }
    [JsonPropertyName("image@2x_url")]
    public required string Image2xUrl { get; set; }
    public required string ImageUrl { get; set; }
    public required string Url { get; set; }
}