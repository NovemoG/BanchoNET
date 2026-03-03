using BanchoNET.Core.Abstractions.Repositories;
using BanchoNET.Core.Abstractions.Services;
using BanchoNET.Core.Attributes;
using BanchoNET.Core.Models.Auth;
using BanchoNET.Core.Utils.Extensions;
using Microsoft.AspNetCore.Mvc;

namespace BanchoNET.Infrastructure.Controllers.OAuth;

[ApiController]
[SubdomainAuthorize("osu")]
public class OAuthController(
    IPlayersRepository players,
    IAuthService auth
) : ControllerBase
{
    [HttpGet("test")]
    public async Task Test() {
        await players.CreatePlayer("Cossin", "nig@ger.com", BCrypt.Net.BCrypt.HashPassword("nigger123".CreateMD5()), "pl");
    }
    
    [HttpPost("/oauth/token")]
    public async Task<IActionResult> Token(
        [FromForm] TokenRequestDto req
    ) {
        if (req.grant_type == "password")
        {
            if (string.IsNullOrEmpty(req.username) || string.IsNullOrEmpty(req.password))
                return BadRequest(new { error = "invalid_request", hint = "Username or password missing" });
            //TODO return proper model
            
            var user = await auth.ValidateUserCredentials(req.username, req.password);
            if (user == null) return Unauthorized(new { error = "invalid_grant", hint = "Incorrect sign in" });

            var tokens = await auth.CreateTokensForUser(user, req.scope);
            var session = await auth.CreateSessionVerificationForUser(user.Id);
            
            return Ok(new
            {
                token_type = tokens.token_type,
                expires_in = tokens.expires_in,
                access_token = tokens.access_token,
                refresh_token = tokens.refresh_token,
                demo_session_code = session.Notes
            });
        }

        if (req.grant_type == "refresh_token")
        {
            if (string.IsNullOrEmpty(req.refresh_token))
                return BadRequest(new { error = "invalid_request" });
            
            var tokens = await auth.Refresh(req.refresh_token);
            if (tokens == null) return Unauthorized(new { error = "invalid_grant" });
            
            return Ok(tokens);
        }

        return BadRequest(new { error = "unsupported_grant_type" });
    }
}