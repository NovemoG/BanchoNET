using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using BanchoNET.Core.Abstractions.Repositories;
using BanchoNET.Core.Abstractions.Services;
using BanchoNET.Core.Models.Auth;
using BanchoNET.Core.Models.Db;
using BanchoNET.Core.Models.Players;
using BanchoNET.Core.Utils;
using BanchoNET.Core.Utils.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace BanchoNET.Infrastructure.Services;

public class AuthService(
    BanchoDbContext db,
    IPlayersRepository players,
    IPasswordService passwords,
    IAccessTokenDenylist denylist
) : IAuthService
{
    private static readonly string Issuer = $"https://osu.{AppSettings.Domain}";
    private static readonly TimeSpan AccessTokenLifetime = TimeSpan.FromDays(1);
    private static readonly TimeSpan RefreshTokenLifetime = TimeSpan.FromDays(30);

    public async Task<Player?> ValidateUserCredentials(
        string username,
        string password
    ) {
        var md5 = password.CreateMD5();
        var userInfo = await players.GetPlayerInfoFromLogin(username);
        if (userInfo == null) return null;

        return passwords.Verify(md5, userInfo.PasswordHash)
            ? new Player(userInfo)
            : null;
    }

    public async Task<OAuthClient?> ValidateClient(
        string? clientId,
        string? clientSecret
    ) {
        if (!int.TryParse(clientId, out var id)) return null;
        if (string.IsNullOrEmpty(clientSecret)) return null;

        var client = await db.OAuthClients.FirstOrDefaultAsync(c => c.Id == id);
        if (client == null || client.Revoked) return null;

        var presented = Encoding.UTF8.GetBytes(clientSecret.HashStringSHA256());
        var stored = Encoding.UTF8.GetBytes(client.SecretHash);

        return CryptographicOperations.FixedTimeEquals(presented, stored) ? client : null;
    }

    public async Task<TokenResponseDto> CreateTokensForUser(
        Player player,
        OAuthClient client,
        string scope
    ) {
        return await IssueTokens(player.Id, client.Id, scope, Guid.NewGuid());
    }

    public TokenResponseDto CreateTokensForClient(
        OAuthClient client,
        string scope
    ) {
        var now = DateTime.UtcNow;
        var expires = now.Add(AccessTokenLifetime);

        return new TokenResponseDto
        {
            access_token = CreateAccessToken(0, client.Id, scope, Guid.NewGuid().ToString(), now, expires),
            expires_in = (int)(expires - now).TotalSeconds,
            refresh_token = null
        };
    }

    public async Task<TokenResponseDto?> Refresh(
        string refreshTokenPlain,
        OAuthClient client
    ) {
        var hash = refreshTokenPlain.HashStringSHA256();
        var dbToken = await db.RefreshTokens.FirstOrDefaultAsync(t => t.TokenHash == hash);
        if (dbToken == null) return null;
        
        if (dbToken.Revoked)
        {
            await db.RefreshTokens
                .Where(t => t.FamilyId == dbToken.FamilyId && !t.Revoked)
                .ExecuteUpdateAsync(s => s.SetProperty(t => t.Revoked, true));

            return null;
        }

        if (dbToken.ExpiresAt < DateTime.UtcNow) return null;
        if (dbToken.ClientId != client.Id) return null;

        var exists = await players.PlayerExists(dbToken.UserId);
        if (!exists) return null;

        return await IssueTokens(
            dbToken.UserId,
            dbToken.ClientId,
            dbToken.Scope,
            dbToken.FamilyId,
            replaced: dbToken
        );
    }

    public async Task RevokeToken(
        string jti,
        DateTimeOffset accessTokenExpiresAt
    ) {
        var dbToken = await db.RefreshTokens.FirstOrDefaultAsync(t => t.Jti == jti);

        // Client credentials tokens have no refresh chain, only the denylist applies
        if (dbToken != null)
        {
            await db.RefreshTokens
                .Where(t => t.FamilyId == dbToken.FamilyId && !t.Revoked)
                .ExecuteUpdateAsync(s => s.SetProperty(t => t.Revoked, true));
        }

        await denylist.Revoke(jti, accessTokenExpiresAt);
    }

    private async Task<TokenResponseDto> IssueTokens(
        int userId,
        int clientId,
        string scope,
        Guid familyId,
        RefreshToken? replaced = null
    ) {
        var jti = Guid.NewGuid().ToString();
        var now = DateTime.UtcNow;
        var expires = now.Add(AccessTokenLifetime);

        var refreshPlain = StringExtensions.RandomBase64Url(64);
        var refreshHash = refreshPlain.HashStringSHA256();

        if (replaced != null)
        {
            replaced.Revoked = true;
            replaced.ReplacedByToken = refreshHash;
        }

        db.RefreshTokens.Add(new RefreshToken
        {
            TokenHash = refreshHash,
            UserId = userId,
            ClientId = clientId,
            FamilyId = familyId,
            Scope = scope,
            ExpiresAt = now.Add(RefreshTokenLifetime),
            Revoked = false,
            Jti = jti
        });
        await db.SaveChangesAsync();

        return new TokenResponseDto
        {
            access_token = CreateAccessToken(userId, clientId, scope, jti, now, expires),
            expires_in = (int)(expires - now).TotalSeconds,
            refresh_token = refreshPlain
        };
    }

    private static string CreateAccessToken(
        int userId,
        int clientId,
        string scope,
        string jti,
        DateTime notBefore,
        DateTime expires
    ) {
        var key = new SymmetricSecurityKey(AppSettings.JwtSecret);
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        List<Claim> claims = [
            new(JwtRegisteredClaimNames.Jti, jti),
            new("scopes", scope),
        ];
        
        if (userId > 0)
            claims.Add(new Claim(JwtRegisteredClaimNames.Sub, userId.ToString()));

        var token = new JwtSecurityToken(
            issuer: Issuer,
            audience: clientId.ToString(),
            claims: claims,
            notBefore: notBefore,
            expires: expires,
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public async Task<SessionVerification> CreateSessionVerificationForUser(
        int userId,
        int expireSeconds = 300
    ) {
        var codePlain = StringExtensions.Gen6DigitCode();
        var codeHash = codePlain.HashStringSHA256();
        var ver = new SessionVerification
        {
            UserId = userId,
            CodeHash = codeHash,
            ExpiresAt = DateTime.UtcNow.AddSeconds(expireSeconds),
            Used = false
        };
        db.SessionVerifications.Add(ver);
        await db.SaveChangesAsync();

        ver.Notes = $"demo_code:{codePlain}";
        await db.SaveChangesAsync();
        return ver;
    }

    public async Task<bool> VerifySessionCode(
        int userId,
        string codePlain
    ) {
        var hash = codePlain.HashStringSHA256();
        var session = await db.SessionVerifications
            .Where(s => s.UserId == userId && !s.Used && s.ExpiresAt >= DateTime.UtcNow)
            .OrderByDescending(s => s.Id)
            .FirstOrDefaultAsync();

        if (session == null) return false;
        if (session.CodeHash != hash) return false;

        session.Used = true;
        session.VerifiedAt = DateTime.UtcNow;

        await db.SaveChangesAsync();
        return true;
    }
}