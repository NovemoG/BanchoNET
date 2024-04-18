using System.Diagnostics;
using BanchoNET.Objects;
using BanchoNET.Objects.Players;
using BanchoNET.Services;
using Microsoft.AspNetCore.Mvc;

namespace BanchoNET.Controllers.OsuApi;

[ApiController]
public partial class OsuController : ControllerBase
{
    private readonly BanchoHandler _bancho;
    private readonly GeolocService _geoloc;
    private readonly BanchoSession _session;
    private readonly HttpClient _httpClient;
    // private readonly ILogger<OsuController> _logger;

    public OsuController(BanchoHandler bancho, GeolocService geoloc, HttpClient httpClient/*, ILogger<OsuController>? logger*/)
    {
        _bancho = bancho;
        _geoloc = geoloc;
        _session = BanchoSession.Instance;
        _httpClient = httpClient;
        // _logger = logger;
    }
	
    [HttpPost("/web/osu-error.php")]
    public async Task<IActionResult> OsuError()
    {
        Console.WriteLine("OsuError");
        return Ok("");
    }

    [HttpGet("/web/bancho_connect.php")]
    public async Task<IActionResult> BanchoConnect()
    {
        Console.WriteLine("BanchoConnect");
        return Ok();
    }

    [HttpGet("/web/check-updates.php")]
    public async Task<IActionResult> CheckUpdates()
    {
        Console.WriteLine("CheckUpdates");
        return Ok();
    }
}