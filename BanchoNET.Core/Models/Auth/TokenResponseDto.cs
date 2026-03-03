namespace BanchoNET.Core.Models.Auth;

public class TokenResponseDto
{
    public string token_type { get; set; } = "Bearer";
    public int expires_in { get; set; }
    public string access_token { get; set; }
    public string refresh_token { get; set; }
}