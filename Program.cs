using System.Diagnostics;
using BanchoNET.Models;
using BanchoNET.Models.Dtos;
using BanchoNET.Objects;
using BanchoNET.Objects.Players;
using BanchoNET.Objects.Privileges;
using BanchoNET.Services;
using BanchoNET.Utils;
using dotenv.net;
using Hangfire;
using Hangfire.MySql;
using Microsoft.EntityFrameworkCore;
using StackExchange.Redis;
using static System.Data.IsolationLevel;
using IsolationLevel = System.Transactions.IsolationLevel;

namespace BanchoNET;

public class Program
{
	public static void Main(string[] args)
	{
		var initStopwatch = new Stopwatch();
		initStopwatch.Start();
		
		var builder = WebApplication.CreateBuilder(args);
		var messages = builder.Configuration.GetSection("messages");
		
		DotEnv.Load(); // Load .env when non dockerized

		#region Domain Check

		var domain = AppSettings.Domain;
		if (string.IsNullOrEmpty(domain))
		{
			Console.ForegroundColor = ConsoleColor.Red;
			Console.WriteLine("[Init] Please set the DOMAIN environment variable.");
			Console.ForegroundColor = ConsoleColor.White;
			return;
		}

		#endregion

		#region Environment Variables Initialization

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
		
		var missing = false;
		foreach (var requiredEnvVar in requiredEnvVars)
		{
			if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable(requiredEnvVar))) continue;
			Console.WriteLine($"[Init] Missing environment variable: {requiredEnvVar}");
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
		
		var redisConnectionString = 
			$"{dbConnections.RedisHost}:{dbConnections.RedisPort},password={dbConnections.RedisPass},allowAdmin=true";
		
		#endregion
			
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
		builder.Services.AddSwaggerGen();
		
		builder.Services.AddSingleton<IConnectionMultiplexer>(ConnectionMultiplexer.Connect(redisConnectionString));
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
		app.UseSwagger();
		app.UseSwaggerUI();

		app.Use(async (context, next) =>
		{
			context.Response.ApplyHeaders();
			
			var stopwatch = new Stopwatch();
			stopwatch.Start();
			
			await next(context);
			
			stopwatch.Stop();
			//TODO some fancy coloring
			Console.WriteLine($"[{context.Request.Method} {context.Response.StatusCode}]\t{context.Request.Host}{context.Request.Path} | Request took: {stopwatch.Elapsed.Seconds}s {stopwatch.Elapsed.Milliseconds}ms {stopwatch.Elapsed.Microseconds}μs");
		});

		#region Initialization
		
		if (string.IsNullOrEmpty(AppSettings.OsuApiKey))
		{
			//TODO: Logging system || probably to change
			Console.ForegroundColor = ConsoleColor.Yellow;
			Console.WriteLine("[Init] OSU_API_KEY is not set. Some features will be disabled.");
			Console.ForegroundColor = ConsoleColor.White;
		}
		
		InitBanchoBot(app.Services.CreateScope());
		InitChannels(app.Services.CreateScope());
		
		// Even if redis creates snapshots of rankings it isn't
		// always 100% accurate with database so we need to update
		// redis leaderboards on startup
		InitRedis(app.Services.CreateScope(), dbConnections.RedisHost, dbConnections.RedisPort);
		
		app.Services.GetRequiredService<OsuVersionService>().FetchOsuVersion().Wait();

		#endregion
		
		initStopwatch.Stop();
		Console.WriteLine($"[Init] Initialization took: {initStopwatch.Elapsed}");
		
		app.Run();
	}
	
	private static void InitBanchoBot(IServiceScope scope)
	{
		var db = scope.ServiceProvider.GetRequiredService<BanchoDbContext>();

		var dbBanchoBot = db.Players.FirstOrDefault(p => p.Id == 1);
		if (dbBanchoBot != null)
		{
			BanchoSession.Instance.AppendBot(new Player(dbBanchoBot));
			return;
		}

		var banchoBotName = AppSettings.BanchoBotName;
		if (banchoBotName.Length > 16)
		{
			Console.WriteLine("[Init] Bancho bot name is too long, truncating to 16 characters");
			banchoBotName = banchoBotName[..16];
			Console.WriteLine("[Init] New bot name: " + banchoBotName);
		}
		
		var entry = db.Players.Add(new PlayerDto
		{
			Id = 1,
			Username = banchoBotName,
			SafeName = banchoBotName.MakeSafe(),
			LoginName = banchoBotName.MakeSafe(),
			Email = "ban@cho.bot",
			PasswordHash = "1",
			Country = "a2",
			LastActivityTime = DateTime.Now.AddYears(100),
			Privileges = (int)(Privileges.Verified | Privileges.Staff),
		});
		db.SaveChanges();
		
		BanchoSession.Instance.AppendBot(new Player(entry.Entity));
	}

	private static void InitChannels(IServiceScope scope)
	{
		var db = scope.ServiceProvider.GetRequiredService<BanchoDbContext>();
		
		//TODO get channels and add them to BanchoSession collection
	}

	private static void InitRedis(IServiceScope scope, string redisHost, string redisPort)
	{
		var db = scope.ServiceProvider.GetRequiredService<BanchoDbContext>();
		var redis = scope.ServiceProvider.GetRequiredService<IConnectionMultiplexer>();
		var redisDb = redis.GetDatabase();
		
		var stopwatch = new Stopwatch();
		stopwatch.Start();
		
		redis.GetServer($"{redisHost}:{redisPort}").FlushDatabase();

		for (byte i = 0; i <= (byte)GameMode.AutopilotStd; i++)
		{
			if (i == 7) continue;

			var mode = i;
			var playersPpModeValues = db.Stats.Include(s => s.Player)
				.Where(s => s.Mode == mode &&
				            (s.Player.Privileges & 1) == 1)
				.Select(s => new { s.PlayerId, s.Player.Country, s.PP })
				.ToArray();

			foreach (var values in playersPpModeValues)
			{
				redisDb.SortedSetAdd($"bancho:leaderboard:{mode}", values.PlayerId, values.PP);
				redisDb.SortedSetAdd($"bancho:leaderboard:{mode}:{values.Country}", values.PlayerId, values.PP);
			}
		}
		
		stopwatch.Stop();
		Console.WriteLine($"[Init] Redis leaderboards updated in {stopwatch.ElapsedMilliseconds}ms");
	}
}