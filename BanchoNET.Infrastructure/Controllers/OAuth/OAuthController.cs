using System.Text.Json;
using BanchoNET.Core.Abstractions.Services;
using BanchoNET.Core.Attributes;
using BanchoNET.Core.Models.Auth;
using BanchoNET.Core.Utils.Extensions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace BanchoNET.Infrastructure.Controllers.OAuth;

[ApiController]
[SubdomainAuthorize("osu")]
public class OAuthController(
    IAuthService auth
) : ControllerBase
{
    [HttpPost("/oauth/token")]
    public async Task<IActionResult> Token()
    {
        var req = await ReadRequest();

        if (req.grant_type is not ("password" or "refresh_token" or "client_credentials"))
            return OAuthError(
                StatusCodes.Status400BadRequest,
                "unsupported_grant_type",
                "The authorization grant type is not supported by the authorization server.",
                "Check that all required parameters have been provided"
            );

        var client = await auth.ValidateClient(req.client_id, req.client_secret);
        if (client == null)
            return OAuthError(
                StatusCodes.Status401Unauthorized,
                "invalid_client",
                "Client authentication failed.",
                "Check the `client_id` and `client_secret` parameters"
            );

        switch (req.grant_type)
        {
            case "password":
            {
                if (!client.Trusted)
                    return OAuthError(
                        StatusCodes.Status400BadRequest,
                        "invalid_grant",
                        "The provided authorization grant is invalid.",
                        "This client may not use the password grant"
                    );

                if (string.IsNullOrEmpty(req.username) || string.IsNullOrEmpty(req.password))
                    return OAuthError(
                        StatusCodes.Status400BadRequest,
                        "invalid_request",
                        "The request is missing a required parameter, includes an invalid parameter value, includes a parameter more than once, or is otherwise malformed.",
                        "Check the `username` and `password` parameters"
                    );

                var scope = ResolveScope(req.scope, client);
                if (scope == null) return InvalidScope();

                var user = await auth.ValidateUserCredentials(req.username, req.password);
                if (user == null)
                    return OAuthError(
                        StatusCodes.Status400BadRequest,
                        "invalid_grant",
                        "The user credentials were incorrect."
                    );

                var tokens = await auth.CreateTokensForUser(user, client, scope, Request.GetSessionOrigin());
                var session = await auth.CreateSessionVerificationForUser(user.Id);

                return Ok(new
                {
                    token_type = tokens.token_type,
                    expires_in = tokens.expires_in,
                    access_token = tokens.access_token,
                    refresh_token = tokens.refresh_token,
                    demo_session_code = session.Notes //TODO
                });
            }

            case "refresh_token":
            {
                if (string.IsNullOrEmpty(req.refresh_token))
                    return OAuthError(
                        StatusCodes.Status400BadRequest,
                        "invalid_request",
                        "The request is missing a required parameter, includes an invalid parameter value, includes a parameter more than once, or is otherwise malformed.",
                        "Check the `refresh_token` parameter"
                    );

                var tokens = await auth.Refresh(req.refresh_token, client, Request.GetSessionOrigin());
                if (tokens == null)
                    return OAuthError(
                        StatusCodes.Status401Unauthorized,
                        "invalid_request",
                        "The refresh token is invalid.",
                        "Token has expired"
                    );

                return Ok(tokens);
            }

            default:
            {
                if (!OAuthScopes.Grants(client.AllowedScopes, OAuthScopes.Public))
                    return InvalidScope();

                var requested = OAuthScopes.Split(req.scope);
                if (requested.Length > 0 && requested.Any(s => s != OAuthScopes.Public))
                    return InvalidScope();

                return Ok(auth.CreateTokensForClient(client, OAuthScopes.Public));
            }
        }
    }
    
    private static string? ResolveScope(
        string? requested,
        OAuthClient client
    ) {
        if (string.IsNullOrWhiteSpace(requested))
            return client.AllowedScopes;

        return !OAuthScopes.AreKnown(OAuthScopes.Split(requested))
            ? null
            : OAuthScopes.Restrict(requested, client.AllowedScopes);
    }

    private ObjectResult InvalidScope() => OAuthError(
        StatusCodes.Status400BadRequest,
        "invalid_scope",
        "The requested scope is invalid, unknown, or malformed.",
        "Check the `scope` parameter"
    );
    
    private async Task<TokenRequestDto> ReadRequest()
    {
        if (Request.HasFormContentType)
        {
            var form = await Request.ReadFormAsync();

            return new TokenRequestDto
            {
                grant_type = form["grant_type"],
                username = form["username"],
                password = form["password"],
                client_id = form["client_id"],
                client_secret = form["client_secret"],
                scope = form["scope"],
                refresh_token = form["refresh_token"]
            };
        }

        Dictionary<string, JsonElement>? body;
        try
        {
            body = await JsonSerializer.DeserializeAsync<Dictionary<string, JsonElement>>(
                Request.Body,
                cancellationToken: HttpContext.RequestAborted);
        }
        catch (JsonException)
        {
            return new TokenRequestDto();
        }

        return new TokenRequestDto
        {
            grant_type = ReadValue(body, "grant_type"),
            username = ReadValue(body, "username"),
            password = ReadValue(body, "password"),
            client_id = ReadValue(body, "client_id"),
            client_secret = ReadValue(body, "client_secret"),
            scope = ReadValue(body, "scope"),
            refresh_token = ReadValue(body, "refresh_token")
        };
    }
    
    private static string? ReadValue(
        Dictionary<string, JsonElement>? body,
        string key
    ) {
        if (body == null || !body.TryGetValue(key, out var value))
            return null;

        return value.ValueKind switch
        {
            JsonValueKind.String => value.GetString(),
            JsonValueKind.Null or JsonValueKind.Undefined => null,
            JsonValueKind.Array => string.Join(' ', value.EnumerateArray().Select(e => e.ToString())),
            _ => value.ToString()
        };
    }

    private ObjectResult OAuthError(
        int status,
        string error,
        string description,
        string? hint = null
    ) {
        return StatusCode(status, new
        {
            error,
            error_description = description,
            hint,
            message = description
        });
    }
}