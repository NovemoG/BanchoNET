using System.Text;
using BanchoNET.Objects;
using BanchoNET.Services;
using BanchoNET.Utils;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace BanchoNET.Controllers.OsuApi;

[ApiController]
public partial class OsuController(BanchoHandler bancho) : ControllerBase
{
	[HttpPost("/web/osu-error.php")]
	public async Task OsuError()
	{
		Console.WriteLine("OsuError");
	}

	[HttpGet("/web/bancho_connect.php")]
	public async Task<IActionResult> BanchoConnect()
	{
		Console.WriteLine("BanchoConnect");
		return Ok();
	}

	[HttpGet("/web/check-updates.php")]
	public async Task CheckUpdates()
	{
		
	}

	[HttpGet("/web/lastfm.php")]
	public async Task<IActionResult> LastFM(
		[FromQuery(Name = "b")]string beatmapId, 
		[FromQuery(Name = "c")]string beatmapNameBase64,
		[FromQuery(Name = "action")]string action,
		[FromQuery(Name = "us")]string username,
		[FromQuery(Name = "ha")]string passwordHash)
	{
		if (beatmapId[0] != 'a') return Ok("-3");

		var flags = (LastFMFLags)int.Parse(beatmapId[1..]);

		if ((flags & (LastFMFLags.HQ_ASSEMBLY | LastFMFLags.HQ_FILE)) != 0)
		{
			//TODO restrict player
			
			//TODO logout player

			return Ok("-3");
		}

		if ((flags & LastFMFLags.REGISTRY_EDITS) != 0)
		{
			//TODO
		}
		
		return Ok();
	}

	[HttpPost("/users")]
	public async Task<IActionResult> RegisterAccount([FromForm(Name = "user[username]")] string username, [FromForm(Name = "user[user_email]")] string email, [FromForm(Name = "user[password]")] string password, [FromForm(Name = "Check")] int check)
	{
		if (string.IsNullOrEmpty(username) || 
		    string.IsNullOrEmpty(email) ||
		    string.IsNullOrEmpty(password))
		{
			return BadRequest("Missing required params");
		}

		var errors = new Dictionary<string, List<string>>
		{
			{ "username", [] },
			{ "password", [] },
			{ "user_email", [] }
		};
		
		if (!Regexes.Username().Match(username).Success) errors["username"].Add("Must be 2-15 characters long");
		if (Regexes.SingleCharacterType().Match(username).Success) errors["username"].Add("Cannot contain spaces and underscores");
		//TODO check for disallowed names
		if (await bancho.UsernameTaken(username)) errors["username"].Add("Username already taken by other player");
		
		if (!Regexes.Email().Match(email).Success) errors["user_email"].Add("Invalid email syntax");
		if (await bancho.EmailTaken(email)) errors["user_email"].Add("Email already taken by other player");
		
		if (password.Length is <= 8 or > 32) errors["password"].Add("Password must be 8-32 characters long");
		
		if (errors.Any(e => e.Value.Count > 0))
		{
			//TODO refactor this shit???
			var jsonSb = new StringBuilder("{\"form_error\":{\"user\":{");

			foreach (var error in errors.Where(error => error.Value.Count != 0))
			{
				jsonSb.Append($"\"{error.Key}\":[\"{string.Join('\n', error.Value)}\"],");
			}

			jsonSb.Length--;
			jsonSb.Append("}}}");
			
			return new ContentResult
			{
				Content = jsonSb.ToString(),
				ContentType = "application/json",
				StatusCode = 400
			};
		}

		if (check == 0)
		{
			var pwdMD5 = password.CreateMD5();
			var pwdBcrypt = BCrypt.Net.BCrypt.HashPassword(pwdMD5);
			
			//TODO cache password hashes
			
			//TODO geoloc from ip header

			await bancho.CreatePlayer(username, email, pwdBcrypt, "pl");
		}

		return Ok("ok");
	}
}