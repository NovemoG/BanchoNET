using BanchoNET.Core.Models.Auth;
using BanchoNET.Core.Models.Players;

namespace BanchoNET.Core.Abstractions.Services;

public interface IAuthService
{
    Task<Player?> ValidateUserCredentials(
        string username,
        string password
    );

    Task<OAuthClient?> ValidateClient(
        string? clientId,
        string? clientSecret
    );

    Task<TokenResponseDto> CreateTokensForUser(
        Player player,
        OAuthClient client,
        string scope
    );
    
    TokenResponseDto CreateTokensForClient(
        OAuthClient client,
        string scope
    );

    Task<TokenResponseDto?> Refresh(
        string refreshTokenPlain,
        OAuthClient client
    );
    
    Task RevokeToken(
        string jti,
        DateTimeOffset accessTokenExpiresAt
    );

    Task<SessionVerification> CreateSessionVerificationForUser(
        int userId,
        int expireSeconds = 300
    );

    Task<bool> VerifySessionCode(
        int userId,
        string codePlain
    );
}