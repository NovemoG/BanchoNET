using BanchoNET.Core.Abstractions.Repositories;
using BanchoNET.Core.Abstractions.Services;
using BanchoNET.Core.Abstractions.Services.Lazer;
using BanchoNET.Core.Attributes;
using BanchoNET.Core.Models.Api.Account;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace BanchoNET.Infrastructure.Controllers.Api.Account;

[Route("api/v2/me")]
[FormError]
public partial class AccountController(
    IPlayersRepository players,
    ILazerPlayerService playerService,
    IBeatmapHandler beatmaps,
    IAccountSettingsService settings,
    IProfileImageService images,
    IAuthService auth,
    IPasswordService passwords
) : ApiControllerBase(players, playerService, beatmaps)
{
    private async Task<IActionResult> SettingsResult(
        int playerId
    ) {
        var response = await settings.GetSettings(playerId);

        return response == null ? NotFound() : JsonSnake(response);
    }
    
    private static ObjectResult FormErrorResult(
        string group,
        string field,
        string message
    ) => new(FormError.Single(group, field, message))
    {
        StatusCode = StatusCodes.Status422UnprocessableEntity
    };
}