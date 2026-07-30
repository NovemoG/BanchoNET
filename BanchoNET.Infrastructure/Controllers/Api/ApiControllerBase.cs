using BanchoNET.Core.Abstractions.Repositories;
using BanchoNET.Core.Abstractions.Services;
using BanchoNET.Core.Abstractions.Services.Lazer;
using BanchoNET.Core.Attributes;
using BanchoNET.Core.Models.Auth;
using BanchoNET.Core.Utils.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BanchoNET.Infrastructure.Controllers.Api;

[ApiController]
[Authorize]
[SubdomainAuthorize("osu")]
[RequireScope(OAuthScopes.Public)]
[RequireUser]
public abstract class ApiControllerBase(
    IPlayersRepository players,
    ILazerPlayerService playerService,
    IBeatmapHandler beatmaps
) : ControllerBase
{
    protected readonly ILazerPlayerService PlayerService = playerService;
    protected readonly IPlayersRepository Players = players;
    protected readonly IBeatmapHandler Beatmaps = beatmaps;

    protected static JsonResult JsonSnake(object? value) => new(value, SnakeCaseNamingPolicy.Options);
}