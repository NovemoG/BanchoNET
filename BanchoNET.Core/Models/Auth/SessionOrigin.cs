namespace BanchoNET.Core.Models.Auth;

public readonly record struct SessionOrigin(
    string? Ip,
    string? UserAgent
) {
    public static readonly SessionOrigin Unknown = new(null, null);
}