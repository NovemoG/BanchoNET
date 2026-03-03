using System.ComponentModel.DataAnnotations;

namespace BanchoNET.Core.Models.Auth;

public class SessionVerification
{
    [Key]
    public int Id { get; set; }
    public int UserId { get; set; }
    public string CodeHash { get; set; }
    public DateTime ExpiresAt { get; set; }
    public DateTime? VerifiedAt { get; set; }
    public bool Used { get; set; }
    public string? Notes { get; set; }
}