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

    public async Task<bool> IsClientTrusted(
        string? clientId
    ) {
        if (!int.TryParse(clientId, out var id)) return false;

        var client = await db.OAuthClients.FirstOrDefaultAsync(c => c.Id == id);

        return client is { Revoked: false, Trusted: true };
    }

    public async Task<TokenResponseDto> CreateTokensForUser(
        Player player,
        OAuthClient client,
        string scope,
        SessionOrigin origin
    ) {
        var existing = await FindReusableFamily(player.Id, client.Id, origin);

        return await IssueTokens(player.Id, client.Id, scope, existing ?? Guid.NewGuid(), origin);
    }

    /// <summary>
    /// The newest live family for this user on the same client and origin, if any.
    /// </summary>
    private async Task<Guid?> FindReusableFamily(
        int userId,
        int clientId,
        SessionOrigin origin
    ) {
        // An unknown origin cannot be matched to anything, so it always starts a new session
        if (origin.Ip == null || origin.UserAgent == null) return null;

        var now = DateTime.UtcNow;
        var candidate = await db.RefreshTokens
            .Where(t => t.UserId == userId
                        && t.ClientId == clientId
                        && !t.Revoked
                        && t.ExpiresAt > now
                        && t.Ip == origin.Ip
                        && t.UserAgent == origin.UserAgent)
            .OrderByDescending(t => t.CreatedAt)
            .FirstOrDefaultAsync();

        if (candidate == null) return null;
        
        candidate.Revoked = true;
        await db.SaveChangesAsync();

        return candidate.FamilyId;
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
        OAuthClient client,
        SessionOrigin origin
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

        dbToken.LastUsedAt = DateTime.UtcNow;

        return await IssueTokens(
            dbToken.UserId,
            dbToken.ClientId,
            dbToken.Scope,
            dbToken.FamilyId,
            origin,
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

    public async Task<Guid?> GetFamilyForJti(
        string? jti
    ) {
        if (string.IsNullOrEmpty(jti)) return null;

        var token = await db.RefreshTokens.FirstOrDefaultAsync(t => t.Jti == jti);

        return token?.FamilyId;
    }

    public async Task<List<UserSessionDto>> GetSessions(
        int userId,
        string? currentJti
    ) {
        var currentFamily = await GetFamilyForJti(currentJti);

        var rows = await db.RefreshTokens
            .Where(t => t.UserId == userId && !t.Revoked && t.ExpiresAt > DateTime.UtcNow)
            .ToListAsync();

        var clientIds = rows.Select(t => t.ClientId).Distinct().ToArray();
        var clients = await db.OAuthClients
            .Where(c => clientIds.Contains(c.Id))
            .ToDictionaryAsync(c => c.Id, c => c.Name);
        
        return rows
            .GroupBy(t => t.FamilyId)
            .Select(family => family.OrderByDescending(t => t.CreatedAt).First())
            .Select(token => new UserSessionDto
            {
                Id = token.FamilyId.ToString(),
                Current = currentFamily == token.FamilyId,
                CreatedAt = token.CreatedAt,
                LastUsedAt = token.LastUsedAt,
                ExpiresAt = token.ExpiresAt,
                Ip = token.Ip,
                UserAgent = token.UserAgent,
                Client = new SessionClientDto
                {
                    Id = token.ClientId,
                    Name = clients.GetValueOrDefault(token.ClientId)
                }
            })
            .OrderByDescending(s => s.LastUsedAt ?? s.CreatedAt)
            .ToList();
    }

    public async Task<bool> RevokeSession(
        int userId,
        Guid familyId
    ) {
        var rows = await db.RefreshTokens
            .Where(t => t.UserId == userId && t.FamilyId == familyId && !t.Revoked)
            .ToListAsync();

        if (rows.Count == 0) return false;

        await RevokeRows(rows);
        return true;
    }

    public async Task RevokeAllSessions(
        int userId,
        Guid? exceptFamily
    ) {
        var rows = await db.RefreshTokens
            .Where(t => t.UserId == userId && !t.Revoked && t.FamilyId != exceptFamily)
            .ToListAsync();

        if (rows.Count == 0) return;

        await RevokeRows(rows);
    }
    
    private async Task RevokeRows(
        List<RefreshToken> rows
    ) {
        foreach (var row in rows)
        {
            row.Revoked = true;

            if (row.AccessTokenExpiresAt > DateTime.UtcNow)
                await denylist.Revoke(row.Jti, row.AccessTokenExpiresAt);
        }

        await db.SaveChangesAsync();
    }

    private async Task<TokenResponseDto> IssueTokens(
        int userId,
        int clientId,
        string scope,
        Guid familyId,
        SessionOrigin origin,
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
            Jti = jti,
            CreatedAt = now,
            AccessTokenExpiresAt = expires,
            Ip = origin.Ip,
            UserAgent = origin.UserAgent
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