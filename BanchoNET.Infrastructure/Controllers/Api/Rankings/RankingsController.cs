using BanchoNET.Core.Abstractions.Repositories;
using BanchoNET.Core.Abstractions.Services;
using Microsoft.AspNetCore.Mvc;

namespace BanchoNET.Infrastructure.Controllers.Api.Rankings;

[Route("api/v2/rankings")]
public partial class RankingsController(
    IAuthService auth,
    IPlayersRepository players,
    IBeatmapsRepository beatmaps
) : ApiController(auth, players, beatmaps)
{
    
}