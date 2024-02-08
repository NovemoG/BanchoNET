using System.Text.RegularExpressions;
using BanchoNET.Models;
using BanchoNET.Services;
using BanchoNET.Utils;
using Hangfire;
using Hangfire.MySql;
using Microsoft.EntityFrameworkCore;
using IsolationLevel = System.Transactions.IsolationLevel;

namespace BanchoNET;

public class Program
{
	public static void Main(string[] args)
	{
		var builder = WebApplication.CreateBuilder(args);
		var serverConfigSection = builder.Configuration.GetSection("ServerConfig");

		var domain = serverConfigSection["Domain"];
		var mySqlConnectionString = builder.Configuration.GetConnectionString("MySql")!;
		var hangfireConnectionString = builder.Configuration.GetConnectionString("Hangfire")!;

		Regexes.NowPlaying = new Lazy<Regex>(() => new Regex(@$"^\x01ACTION is (?:playing|editing|watching|listening to) \[https://osu\.(?:{domain}|ppy\.sh)/beatmapsets/(?<sid>\d{{1,10}})#/?(?:osu|taiko|fruits|mania)?/(?<bid>\d{{1,10}})/? .+\](?<mods>(?: (?:-|\+|~|\|)\w+(?:~|\|)?)+)?\x01$", RegexOptions.Compiled));
		
		builder.Services.AddHangfire(config =>
		{
			config.UseStorage(new MySqlStorage(hangfireConnectionString, new MySqlStorageOptions
			{
				TransactionIsolationLevel = (IsolationLevel?)System.Data.IsolationLevel.ReadCommitted,
				QueuePollInterval = TimeSpan.FromSeconds(15),
				JobExpirationCheckInterval = TimeSpan.FromHours(1),
				CountersAggregateInterval = TimeSpan.FromMinutes(5),
				PrepareSchemaIfNecessary = true,
			}));
		});
		builder.Services.AddHangfireServer();
		
		builder.Services.AddEndpointsApiExplorer();
		builder.Services.AddAuthorization();
		builder.Services.AddControllers();

		builder.Services.Configure<ServerConfig>(serverConfigSection);
		builder.Services.AddDbContext<BanchoDbContext>(options =>
		{
			options.UseMySQL(mySqlConnectionString);
		});
		builder.Services.AddScoped<BanchoHandler>();
		builder.Services.AddHttpClient();

		var app = builder.Build();

		app.UseHangfireDashboard();
		app.UseHttpsRedirection();
		app.UseAuthorization();
		app.MapControllers();

		app.Use(async (context, next) =>
		{
			context.Response.ApplyHeaders();
			
			await next(context);
		});
		
		app.Run();
	}
}