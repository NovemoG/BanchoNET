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
    
    Task<bool> IsClientTrusted(
        string? clientId
    );

    Task<TokenResponseDto> CreateTokensForUser(
        Player player,
        OAuthClient client,
        string scope,
        SessionOrigin origin
    );

    TokenResponseDto CreateTokensForClient(
        OAuthClient client,
        string scope
    );

    Task<TokenResponseDto?> Refresh(
        string refreshTokenPlain,
        OAuthClient client,
        SessionOrigin origin
    );
    
    Task RevokeToken(
        string jti,
        DateTimeOffset accessTokenExpiresAt
    );

    /// <summary>
    /// One entry per refresh token family, which is the stable identity of a browser or client
    /// install across token rotation.
    /// </summary>
    Task<List<UserSessionDto>> GetSessions(
        int userId,
        string? currentJti
    );

    /// <summary>
    /// Revokes a whole family and denylists every access token it issued, so the kill is
    /// immediate.
    /// <returns>
    /// False when the family does not belong to this user, which the caller should
    /// surface as a 404.
    /// </returns>
    /// </summary>
    Task<bool> RevokeSession(
        int userId,
        Guid familyId
    );

    Task RevokeAllSessions(
        int userId,
        Guid? exceptFamily
    );

    /// <summary>
    /// Resolves the family a given access token belongs to, used to spare the caller's own session
    /// when revoking everything else.
    /// </summary>
    Task<Guid?> GetFamilyForJti(
        string? jti
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