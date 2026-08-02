namespace BanchoNET.Core.Abstractions.Services;

public interface IAccessTokenDenylist
{
    Task Revoke(
        string jti,
        DateTimeOffset expiresAt
    );

    Task<bool> IsRevoked(
        string jti
    );
}