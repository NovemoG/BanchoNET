using System.IdentityModel.Tokens.Jwt;
using Microsoft.AspNetCore.SignalR;

namespace BanchoNET.Core.Utils.SignalR;

public class SubUserIdProvider : IUserIdProvider
{
    public string? GetUserId(
        HubConnectionContext connection
    ) {
        return connection.User?.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
    }
}