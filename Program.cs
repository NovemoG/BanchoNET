using BanchoNET.Models;
using BanchoNET.Services;
using BanchoNET.Utils;
using Hangfire;
using Hangfire.MySql;
using Microsoft.EntityFrameworkCore;
using static System.Data.IsolationLevel;
using IsolationLevel = System.Transactions.IsolationLevel;

namespace BanchoNET;

public class Program
{
	public static void Main(string[] args)
	{
		var builder = WebApplication.CreateBuilder(args);

		var configSection = builder.Configuration.GetSection("ServerConfig");
		var mySqlConnectionString = builder.Configuration.GetConnectionString("MySql")!;
		var hangfireConnectionString = builder.Configuration.GetConnectionString("Hangfire")!;
		
		var domain = configSection["Domain"]!;
		Regexes.InitNowPlayingRegex(domain);
		BeatmapExtensions.InitBaseUrlValue(domain);
		
		builder.Services.AddHangfire(config =>
		{
			config.UseStorage(new MySqlStorage(hangfireConnectionString, new MySqlStorageOptions
			{
				TransactionIsolationLevel = (IsolationLevel?)ReadCommitted,
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

		builder.Services.Configure<ServerConfig>(configSection);
		builder.Services.AddSingleton<OsuVersionService>();
		builder.Services.AddDbContext<BanchoDbContext>(options =>
		{
			options.UseMySQL(mySqlConnectionString);
		});
		builder.Services.AddScoped<GeolocService>();
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
		
		//Initialization
		app.Services.GetRequiredService<OsuVersionService>().FetchOsuVersion().Wait();
		
		app.Run();
	}
}