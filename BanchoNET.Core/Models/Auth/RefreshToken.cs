using System.ComponentModel.DataAnnotations;

namespace BanchoNET.Core.Models.Auth;

public class RefreshToken
{
    [Key] public int Id { get; set; }
    
    [Required]
    public string TokenHash { get; set; } = null!;
    public int UserId { get; set; }
    public int ClientId { get; set; }
    public Guid FamilyId { get; set; }
    public string Scope { get; set; } = OAuthScopes.All;
    public DateTime ExpiresAt { get; set; }
    
    public bool Revoked { get; set; }
    public string? ReplacedByToken { get; set; }
    public string Jti { get; set; } = null!;
}