using System.Text.RegularExpressions;
using BanchoNET.Models;
using BanchoNET.Services;
using BanchoNET.Utils;
using dotenv.net;
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

		var messages = builder.Configuration.GetSection("messages");

		var requiredEnvVars = new[]
		{
			"MYSQL_HOST",
			"MYSQL_PORT",
			"MYSQL_USER",
			"MYSQL_DB",
			"HANGFIRE_HOST",
			"HANGFIRE_PORT",
			"HANGFIRE_USER",
			"HANGFIRE_DB",
			"REDIS_HOST",
			"REDIS_PORT",
		};
		DotEnv.Load(); // Load .env when non dockerized
		var missing = false;
		foreach (var requiredEnvVar in requiredEnvVars)
		{
			if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable(requiredEnvVar))) continue;
			Console.WriteLine($"Missing environment variable: {requiredEnvVar}");
			missing = true;
		}
		if (missing) return;

		var dbConnections = new DbConnectionsModel
		{
			MysqlHost = Environment.GetEnvironmentVariable("MYSQL_HOST")!,
			MysqlPort = Environment.GetEnvironmentVariable("MYSQL_PORT")!,
			MysqlUser = Environment.GetEnvironmentVariable("MYSQL_USER")!,
			MysqlPass = Environment.GetEnvironmentVariable("MYSQL_PASS")!,
			MysqlDb = Environment.GetEnvironmentVariable("MYSQL_DB")!,
			HangfireHost = Environment.GetEnvironmentVariable("HANGFIRE_HOST")!,
			HangfirePort = Environment.GetEnvironmentVariable("HANGFIRE_PORT")!,
			HangfireUser = Environment.GetEnvironmentVariable("HANGFIRE_USER")!,
			HangfirePass = Environment.GetEnvironmentVariable("HANGFIRE_PASS")!,
			HangfireDb = Environment.GetEnvironmentVariable("HANGFIRE_DB")!,
			RedisHost = Environment.GetEnvironmentVariable("REDIS_HOST")!,
			RedisPort = Environment.GetEnvironmentVariable("REDIS_PORT")!,
			RedisPass = Environment.GetEnvironmentVariable("REDIS_PASS")!
		};
		
		var mySqlConnectionString = 
			$"server={dbConnections.MysqlHost};port={dbConnections.MysqlPort};user={dbConnections.MysqlUser};password={dbConnections.MysqlPass};database={dbConnections.MysqlDb};";
		//TODO: String builder for password in Hangfire connection string
		var hangfireConnectionString = 
			$"server={dbConnections.HangfireHost};port={dbConnections.HangfirePort};user={dbConnections.HangfireUser};database={dbConnections.HangfireDb};Allow User Variables=True";
		
		// Didnt know is this correct
		var redisConnectionString = 
			$"{dbConnections.RedisHost}:{dbConnections.RedisPort},password={dbConnections.RedisPass}";
			
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

		builder.Services.Configure<Messages>(messages);
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

		#region Initialization

		var domain = Environment.GetEnvironmentVariable("DOMAIN");
		if (string.IsNullOrEmpty(domain))
		{
			Console.WriteLine("Please set the DOMAIN environment variable.");
			return;
		}
		if (string.IsNullOrEmpty(AppSettings.OsuApiKey))
		{
			//TODO: Logging system || probably to change
			Console.ForegroundColor = ConsoleColor.Yellow;
			Console.WriteLine("OSU_API_KEY is not set. Some features will be disabled.");
			Console.ForegroundColor = ConsoleColor.White;
		}
		
		
		Regexes.InitNowPlayingRegex(domain);
		BeatmapExtensions.InitBaseUrlValue(domain);
		
		app.Services.GetRequiredService<OsuVersionService>().FetchOsuVersion().Wait();

		#endregion
		
		
		
		app.Run();
	}
}