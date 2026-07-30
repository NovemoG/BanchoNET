using System.IdentityModel.Tokens.Jwt;
using BanchoNET.Core.Abstractions.Services;
using BanchoNET.Core.Utils;
using BanchoNET.Infrastructure.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

namespace BanchoNET.Infrastructure;

public static class ServiceCollectionExtensions
{
    private const string NotifyPath = "/notify";

    public static IServiceCollection AddOAuth(
        this IServiceCollection services
    ) {
        services.AddScoped<IAuthService, AuthService>();

        services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        })
        .AddJwtBearer(options =>
        {
            options.MapInboundClaims = false;
            options.RequireHttpsMetadata = true;
            options.SaveToken = true;
            options.Events = new JwtBearerEvents
            {
                OnMessageReceived = context =>
                {
                    if (!context.Request.Path.StartsWithSegments(NotifyPath)
                        || !string.IsNullOrEmpty(context.Request.Headers.Authorization))
                        return Task.CompletedTask;

                    var token = context.Request.Query["access_token"].ToString();
                    if (!string.IsNullOrEmpty(token))
                        context.Token = token;

                    return Task.CompletedTask;
                },
                
                OnTokenValidated = async context =>
                {
                    var jti = context.Principal?.FindFirst(JwtRegisteredClaimNames.Jti)?.Value;
                    if (string.IsNullOrEmpty(jti)) return;

                    var denylist = context.HttpContext.RequestServices.GetRequiredService<IAccessTokenDenylist>();
                    if (await denylist.IsRevoked(jti))
                        context.Fail("This token has been revoked.");
                }
            };
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = false,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = $"https://osu.{AppSettings.Domain}",
                IssuerSigningKey = new SymmetricSecurityKey(AppSettings.JwtSecret)
            };
        });
        
        return services;
    }
}