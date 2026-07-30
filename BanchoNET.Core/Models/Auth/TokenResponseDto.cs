using System.Text.Json.Serialization;

namespace BanchoNET.Core.Models.Auth;

public class TokenResponseDto
{
    public string token_type { get; set; } = "Bearer";
    public int expires_in { get; set; }
    public string access_token { get; set; } = null!;

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? refresh_token { get; set; }
}