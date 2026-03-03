using BanchoNET.Core.Attributes;
using BanchoNET.Core.Models;
using BanchoNET.Infrastructure.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BanchoNET.Infrastructure.Controllers.Api;

[ApiController]
[Route("api/v2")]
[Authorize]
[SubdomainAuthorize("osu")]
public partial class ApiController(BanchoDbContext db, AuthService auth) : ControllerBase
{
    
}