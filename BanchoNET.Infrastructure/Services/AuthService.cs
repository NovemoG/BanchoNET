using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using BanchoNET.Core.Abstractions.Repositories;
using BanchoNET.Core.Abstractions.Services;
using BanchoNET.Core.Models;
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
    IPlayersRepository players
) : IAuthService
{
    private static readonly string Issuer = $"https://osu.{AppSettings.Domain}";
    
    public async Task<Player?> ValidateUserCredentials(
        string username,
        string password
    ) {
        var md5 = password.CreateMD5();
        var userInfo = await players.GetPlayerInfoFromLogin(username);
        if (userInfo == null) return null;

        return md5.VerifyPassword(userInfo.PasswordHash)
            ? new Player(userInfo)
            : null;
    }

    public async Task<TokenResponseDto> CreateTokensForUser(
        Player player,
        string scope = "*"
    ) {
        var key = "FGc9GAtyHzeQDshWP5Ah7dega8hJACAJpQtw6OXk"u8.ToArray();
        var creds = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256);
        var jti = Guid.NewGuid().ToString();
        var now = DateTime.UtcNow;
        var expires = now.AddDays(1);
        
        var claims = new[] {
            new Claim(JwtRegisteredClaimNames.Sub, player.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Aud, "5"),
            new Claim(JwtRegisteredClaimNames.Jti, jti),
            new Claim("scopes", scope),
        };

        var token = new JwtSecurityToken(
            issuer: Issuer,
            audience: "5",
            claims: claims,
            notBefore: now,
            expires: expires,
            signingCredentials: creds
        );
        var accessToken = new JwtSecurityTokenHandler().WriteToken(token);

        var refreshPlain = StringExtensions.RandomBase64Url(64);
        var refreshHash = refreshPlain.HashStringSHA256();
        var refreshDb = new RefreshToken
        {
            TokenHash = refreshHash,
            UserId = player.Id,
            ExpiresAt = DateTime.UtcNow.AddDays(30),
            Revoked = false,
            Jti = jti
        };
        db.RefreshTokens.Add(refreshDb);
        await db.SaveChangesAsync();

        return new TokenResponseDto
        {
            access_token = accessToken,
            expires_in = (int)(expires - now).TotalSeconds,
            refresh_token = refreshPlain
        };
    }

    public async Task<TokenResponseDto?> Refresh(
        string refreshTokenPlain
    ) {
        var hash = refreshTokenPlain.HashStringSHA256();
        var dbToken = db.RefreshTokens.SingleOrDefault(t => t.TokenHash == hash);
        if (dbToken == null || dbToken.Revoked || dbToken.ExpiresAt < DateTime.UtcNow)
            return null;
        
        var exists = await players.PlayerExists(dbToken.UserId);
        if (!exists) return null;

        dbToken.Revoked = true;
        var newPlain = StringExtensions.RandomBase64Url(64);
        var newHash = newPlain.HashStringSHA256();
        var newDb = new RefreshToken
        {
            TokenHash = newHash,
            UserId = dbToken.UserId,
            ExpiresAt = DateTime.UtcNow.AddDays(30),
            Revoked = false,
            Jti = Guid.NewGuid().ToString()
        };
        dbToken.ReplacedByToken = newPlain;
        db.RefreshTokens.Add(newDb);
        await db.SaveChangesAsync();
        
        var key = "FGc9GAtyHzeQDshWP5Ah7dega8hJACAJpQtw6OXk"u8.ToArray();
        var creds = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256);
        var jti = Guid.NewGuid().ToString();
        var now = DateTime.UtcNow;
        var expires = now.AddDays(1);
        
        var claims = new[] {
            new Claim(JwtRegisteredClaimNames.Sub, dbToken.UserId.ToString()),
            new Claim(JwtRegisteredClaimNames.Aud, "5"),
            new Claim(JwtRegisteredClaimNames.Jti, jti),
            new Claim("scopes", "*"),
        };

        var token = new JwtSecurityToken(
            issuer: Issuer,
            audience: "5",
            claims: claims,
            notBefore: now,
            expires: expires,
            signingCredentials: creds
        );
        var accessToken = new JwtSecurityTokenHandler().WriteToken(token);

        return new TokenResponseDto
        {
            access_token = accessToken,
            expires_in = (int)(expires - now).TotalSeconds,
            refresh_token = newPlain
        };
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