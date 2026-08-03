namespace BanchoNET.Core.Models.Auth;

public class UserSessionDto
{
    public string Id { get; set; } = null!;
    public bool Current { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime? LastUsedAt { get; set; }
    public DateTime ExpiresAt { get; set; }

    public string? Ip { get; set; }
    public string? UserAgent { get; set; }

    public SessionClientDto? Client { get; set; }
}

public class SessionClientDto
{
    public int Id { get; set; }
    public string? Name { get; set; }
}