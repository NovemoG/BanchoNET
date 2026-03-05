using BanchoNET.Core.Abstractions.Repositories;
using BanchoNET.Core.Abstractions.Repositories.Histories;
using BanchoNET.Core.Abstractions.Services;
using BanchoNET.Core.Attributes;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BanchoNET.Infrastructure.Controllers.Api;

[ApiController]
[Route("api/v2")]
[Authorize]
[SubdomainAuthorize("osu")]
public partial class ApiController(
    IAuthService auth,
    IPlayersRepository players,
    IHistoriesRepository histories
) : ControllerBase;