using System.ComponentModel.DataAnnotations;

namespace BanchoNET.Core.Models.Auth;

public class OAuthClient
{
    /// <summary>
    /// Doubles as the oauth client_id, so it is assigned rather than generated.
    /// </summary>
    [Key] public int Id { get; set; }
    
    [Required]
    public string Name { get; set; } = null!;
    
    [Required]
    public string SecretHash { get; set; } = null!;
    public string? RedirectUri { get; set; }
    public string AllowedScopes { get; set; } = OAuthScopes.Public;
    
    public bool Trusted { get; set; }
    public bool Revoked { get; set; }
    
    public DateTime CreatedAt { get; set; }
}