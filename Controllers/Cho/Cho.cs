using BanchoNET.Models;
using BanchoNET.Services;
using Microsoft.AspNetCore.Mvc;

namespace BanchoNET.Controllers.Cho;

[ApiController]
public partial class Cho(ServicesProvider services) : ControllerBase
{
	private readonly ServicesProvider _services = services;

	[HttpPost]
	public async Task<HttpResponse> BanchoHandler()
	{
		
	}

	[HttpGet]
	public async Task<HttpResponse> BanchoHttpHandler()
	{
		
	}

	[HttpGet("matches")]
	public async Task<HttpResponse> ViewOnlineUsers()
	{
		
	}

	[HttpGet("online")]
	public async Task<HttpResponse> ViewMatches()
	{
		
	}
}