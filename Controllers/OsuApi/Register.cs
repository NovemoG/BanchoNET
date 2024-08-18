using BanchoNET.Utils;
using BanchoNET.Utils.Extensions;
using Microsoft.AspNetCore.Mvc;

namespace BanchoNET.Controllers.OsuApi;

public partial class OsuController
{
	[HttpPost("/users")]
	public async Task<IActionResult> RegisterAccount(
		[FromForm(Name = "user[username]")] string username, 
		[FromForm(Name = "user[user_email]")] string email, 
		[FromForm(Name = "user[password]")] string password, 
		[FromForm(Name = "Check")] int check)
	{
		if (string.IsNullOrEmpty(username) || 
		    string.IsNullOrEmpty(email) ||
		    string.IsNullOrEmpty(password))
		{
			return BadRequest("Missing required params");
		}

		var errors = new List<ErrorDetails>();

		if (!Regexes.Username.Match(username).Success) errors.Add(new ErrorDetails
		{
			Field = "username",
			Messages = ["Must be 2-15 characters long."]
		});
		if (username.Contains(' ') && username.Contains('_')) errors.Add(new ErrorDetails
		{
			Field = "username",
			Messages = ["Must not contain both spaces and underscores."]
		});
		if (await players.UsernameTaken(username)) errors.Add(new ErrorDetails
		{
			Field = "username",
			Messages = ["Username already taken by other player."]
		});
		if (AppSettings.DisallowedNames.Contains(username)) errors.Add(new ErrorDetails
		{
			Field = "username",
			Messages = ["Username is disallowed."]
		});
		
		if (!Regexes.Email.Match(email).Success) errors.Add(new ErrorDetails
		{
			Field = "user_email",
			Messages = ["Invalid email syntax."]
		});
		if (await players.EmailTaken(email)) errors.Add(new ErrorDetails
		{
			Field = "user_email",
			Messages = ["Email already taken by someone else."]
		});

		if (password.Length is <= 8 or > 32) errors.Add(new ErrorDetails
		{
			Field = "password",
			Messages = ["Password must be 8-32 characters long."]
		});
		
		if (errors.Count != 0)
		{
			return BadRequest(new
			{
				form_error = new
				{
					user = errors.GroupBy(e => e.Field)
					             .ToDictionary(group => group.Key, group => group
						         .Select(e => string.Join('\n', e.Messages)))
				}
			});
		}
		
		if (check != 0) return Ok("ok"); //if there are no errors but it is a check request
		
		var passwordMD5 = password.CreateMD5();
		var passwordBcrypt = BCrypt.Net.BCrypt.HashPassword(passwordMD5);
		
		_session.InsertPasswordHash(passwordMD5, passwordBcrypt);
		
		var playerGeoloc = await geoloc.GetGeoloc(Request.Headers);
		await players.CreatePlayer(
			username,
			email,
			passwordBcrypt,
			playerGeoloc == null
				? "xx"
				: playerGeoloc.Value.Country.Acronym);

		return Ok("ok");
	}

	private readonly record struct ErrorDetails(string Field, List<string> Messages);
}