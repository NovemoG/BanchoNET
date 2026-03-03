using BanchoNET.Core.Models.Auth;
using BanchoNET.Core.Models.Players;

namespace BanchoNET.Core.Abstractions.Services;

public interface IAuthService
{
    Task<Player?> ValidateUserCredentials(
        string username,
        string password
    );

    Task<TokenResponseDto> CreateTokensForUser(
        Player player,
        string scope = "*"
    );

    Task<TokenResponseDto?> Refresh(
        string refreshTokenPlain
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