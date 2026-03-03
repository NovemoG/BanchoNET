using System.Text;
using BanchoNET.Infrastructure.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

namespace BanchoNET.Infrastructure;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddOAuth(
        this IServiceCollection services
    ) {
        services.AddScoped<AuthService>();

        var key = Encoding.UTF8.GetBytes("FGc9GAtyHzeQDshWP5Ah7dega8hJACAJpQtw6OXk");
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
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = false,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = "https://osu.ppy.sh",
                ValidAudience = "5",
                IssuerSigningKey = new SymmetricSecurityKey(key)
            };
        });
        
        return services;
    }
}