using System.Diagnostics;
using System.Reflection;
using System.Text.RegularExpressions;
using BanchoNET.Commands;
using BanchoNET.Core.Abstractions;
using BanchoNET.Core.Abstractions.Bancho.Coordinators;
using BanchoNET.Core.Abstractions.Bancho.Services;
using BanchoNET.Core.Abstractions.Repositories;
using BanchoNET.Core.Abstractions.Repositories.Histories;
using BanchoNET.Core.Abstractions.Services;
using BanchoNET.Core.Models;
using BanchoNET.Core.Models.Channels;
using BanchoNET.Core.Models.Dtos;
using BanchoNET.Core.Models.Players;
using BanchoNET.Core.Models.Privileges;
using BanchoNET.Core.Utils;
using BanchoNET.Core.Utils.Extensions;
using BanchoNET.Infrastructure.Bancho.Coordinators;
using BanchoNET.Infrastructure.Bancho.Services;
using BanchoNET.Middlewares;
using BanchoNET.Services;
using BanchoNET.Services.ClientPacketsHandler;
using BanchoNET.Services.LobbyScoresQueue;
using BanchoNET.Services.Repositories;
using Hangfire;
using Hangfire.AspNetCore;
using Hangfire.MySql;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using MySql.Data.MySqlClient;
using Novelog.Config;
using StackExchange.Redis;
using static System.Data.IsolationLevel;
using IsolationLevel = System.Transactions.IsolationLevel;
using LogLevel = Novelog.Types.LogLevel;
// ReSharper disable ExplicitCallerInfoArgument

namespace BanchoNET;

public class Program
{
	public static void Main(string[] args)
	{
		var initStopwatch = new Stopwatch();
		initStopwatch.Start();
		
		var builder = WebApplication.CreateBuilder(args);
		
		#region Logging

		builder.Logging.ClearProviders();

		var microsoftLogger = new LoggerConfigBuilder(false)
			.AttachConsole()
			.ModifyDefaultFormatter("", "[{0}] [{1} | {3}] {4}");
		
		builder.Logging.AddProvider(new NovelogLoggerProvider(microsoftLogger.Build()));
		builder.Services.AddNovelog(options => options
			.AttachConsole()
			.AttachRollingFile(new RollingFileConfig
			{
				FilePath = Storage.GetLogFilePath("log.txt")
			})
			.AttachRollingFile(new RollingFileConfig
			{
				FilePath = Storage.GetLogFilePath("debug.txt"),
				MinLogLevel = LogLevel.DEBUG
			})
			.ForType<RequestTimingMiddleware>(rtmOptions => rtmOptions
				.AttachConsole()
				.AttachRollingFile(new RollingFileConfig
				{
					FilePath = Storage.GetLogFilePath("requests.txt"),
					MinLogLevel = LogLevel.DEBUG
				})
				.ModifyDefaultFormatter("", "[{0}] [{1} | {2}] {4}"))
		);

		#endregion

		#region Domain Check

		var domain = AppSettings.Domain;
		if (string.IsNullOrEmpty(domain))
		{
			Logger.Shared.LogCritical("Please set the DOMAIN environment variable.", null, "Init");
			return;
		}

		#endregion

		#region Environment Variables Initialization

		var requiredEnvVars = new[]
		{
			"MYSQL_HOST",
			"MYSQL_USER",
			"MYSQL_DB",
			"REDIS_HOST",
			"REDIS_PORT",
			"MONGO_HOST",
			"MONGO_PORT",
		};
		
		var missing = false;
		foreach (var requiredEnvVar in requiredEnvVars)
		{
			if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable(requiredEnvVar))) continue;
			Logger.Shared.LogCritical($"Missing environment variable: {requiredEnvVar}", null, "Init");
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
			RedisHost = Environment.GetEnvironmentVariable("REDIS_HOST")!,
			RedisPort = Environment.GetEnvironmentVariable("REDIS_PORT")!,
			RedisPass = Environment.GetEnvironmentVariable("REDIS_PASS")!,
			MongoHost = Environment.GetEnvironmentVariable("MONGO_HOST")!,
			MongoPort = Environment.GetEnvironmentVariable("MONGO_PORT")!,
			MongoUser = Environment.GetEnvironmentVariable("MONGO_USER")!,
			MongoPass = Environment.GetEnvironmentVariable("MONGO_PASS")!,
		};
		
		var mySqlConnectionString = 
			$"server={dbConnections.MysqlHost};" +
			$"port=3306;" +
			$"user={dbConnections.MysqlUser};" +
			$"password={dbConnections.MysqlPass};";
		
		EnsureHangfireDatabaseExists(mySqlConnectionString);

		mySqlConnectionString += $"database={dbConnections.MysqlDb};";

		var hangfirePass = dbConnections.MysqlPass;
		var hangfireConnectionString =
			$"server={dbConnections.MysqlHost};" +
			$"port=3306;" +
			$"user={dbConnections.MysqlUser};" +
			$"{(string.IsNullOrEmpty(hangfirePass) ? "" : $"password={hangfirePass};")}" +
			$"database=hangfire;" +
			$"Allow User Variables=True";
		
		var redisConnectionString = 
			$"{dbConnections.RedisHost}:{dbConnections.RedisPort}," +
			$"password={dbConnections.RedisPass}," +
			$"allowAdmin=true";

		var credentials = true;
		var mongoUser = dbConnections.MongoUser;
		var mongoPass = dbConnections.MongoPass;
		if (string.IsNullOrEmpty(mongoUser)
		    && !string.IsNullOrEmpty(mongoPass))
		{
			Logger.Shared.LogWarning("You specified password but left username empty for MongoDB. Ignoring password.", caller: "Init");
			credentials = false;
		}
		else if (string.IsNullOrEmpty(mongoUser)
		         && string.IsNullOrEmpty(mongoPass))
		{
			Logger.Shared.LogWarning("No credentials specified for MongoDB.", caller: "Init");
			credentials = false;
		}
		else
		{
			mongoUser = EscapeMongoCharacters(mongoUser);
			mongoPass = string.IsNullOrEmpty(mongoPass)
				? ""
				: EscapeMongoCharacters(mongoPass);
		}
		
		var mongoConnectionString =
			$"mongodb://" +
			$"{(credentials ? $"{mongoUser}:{mongoPass}@" : "")}" +
			$"{dbConnections.MongoHost}:{dbConnections.MongoPort}";
		
		#endregion
		
		builder.Services.AddHangfire((sp, config) => {
			config.UseStorage(new MySqlStorage(hangfireConnectionString, new MySqlStorageOptions {
				TransactionIsolationLevel = (IsolationLevel?)ReadCommitted,
				QueuePollInterval = TimeSpan.FromSeconds(15),
				JobExpirationCheckInterval = TimeSpan.FromHours(1),
				CountersAggregateInterval = TimeSpan.FromMinutes(5),
				PrepareSchemaIfNecessary = true,
			})).UseActivator(new AspNetCoreJobActivator(sp.GetRequiredService<IServiceScopeFactory>()));
		}).AddHangfireServer();

		builder.Services.AddEndpointsApiExplorer()
			.AddAuthorization()
			.AddControllers();

		var mongoSettings = MongoClientSettings.FromConnectionString(mongoConnectionString);

		builder.Services.AddSingleton<IConnectionMultiplexer>(ConnectionMultiplexer.Connect(redisConnectionString))
			.AddSingleton(new MongoClient(mongoSettings))
			.AddDbContext<BanchoDbContext>(options => {
				options.UseMySQL(mySqlConnectionString);
			})
			.AddSingleton<IHistoriesRepository, HistoriesRepository>();
		
		//TODO
		builder.Services.AddScoped<IBeatmapsRepository, BeatmapsRepository>();
		builder.Services.AddScoped<IClientsRepository, ClientsRepository>();
		builder.Services.AddScoped<IMessagesRepository, MessagesRepository>();
		builder.Services.AddScoped<IPlayersRepository, PlayersRepository>();
		builder.Services.AddScoped<IScoresRepository, ScoresRepository>();
		builder.Services.AddScoped<IBeatmapHandler, BeatmapHandler>();
			
		builder.Services.AddSingleton<ILobbyScoresQueue, LobbyScoresQueue>()
			.AddHostedService<LobbyQueueHostedService>();

		builder.Services.AddScoped<IGeolocService, GeolocService>()
			.AddScoped<IClientPacketsHandler, ClientPacketsHandler>()
			.AddScoped<ICommandProcessor, CommandProcessor>();
		
		builder.Services.AddTransient<IOsuVersionService, OsuVersionService>()
			.AddTransient<IBackgroundTasks, BackgroundTasks>();
		
		Assembly[] assemblies = [
			typeof(ICoordinator).Assembly,
			typeof(PlayerCoordinator).Assembly
		];

		builder.Services.AddHttpClient()
			.AddSessionServices(assemblies)
			.AddMediatR(config => config.RegisterServicesFromAssemblies(assemblies));
		
		var app = builder.Build();

		app.UseHangfireDashboard();
		app.UseHttpsRedirection();
		app.UseAuthorization();

		app.UseMiddleware<SubdomainMiddleware>();
		app.UseMiddleware<RequestTimingMiddleware>();
		
		app.MapControllers();

		#region Initialization
		
		if (string.IsNullOrEmpty(AppSettings.OsuApiKey))
		{
			Logger.Shared.LogWarning("OSU_API_KEY is not set. Some features will be disabled.", caller: "Init");
		}
		
		EnsureDatabaseExists(app.Services.CreateScope());
		
		InitBanchoBot(app.Services.CreateScope());
		InitChannels(app.Services.CreateScope());
		
		// Even if redis creates snapshots of rankings it isn't
		// always 100% accurate with database so we need to update
		// redis leaderboards on startup
		InitRedis(app.Services.CreateScope(), dbConnections.RedisHost, dbConnections.RedisPort);
		
		app.Services.GetRequiredService<IOsuVersionService>().Init().Wait();
		app.Services.GetRequiredService<IBackgroundTasks>().Init().Wait();

		#endregion
		
		initStopwatch.Stop();
		Logger.Shared.LogInfo($"Initialization took: {initStopwatch.Elapsed}", "Init");
		
		app.Run();
	}

	private static void EnsureHangfireDatabaseExists(string connectionString)
	{
		Logger.Shared.LogInfo("Checking if Hangfire database exists.", "Init");
		
		using var connection = new MySqlConnection(connectionString);
		using var command = connection.CreateCommand();
		
		connection.Open();
		command.CommandText = "CREATE DATABASE IF NOT EXISTS `hangfire`";
		command.ExecuteNonQuery();
	}
	
	private static void EnsureDatabaseExists(IServiceScope scope)
	{
		var db = scope.ServiceProvider.GetRequiredService<BanchoDbContext>();

		Logger.Shared.LogInfo("Checking if database exists.", "Init");
		
		var created = db.Database.EnsureCreated();
		
		Logger.Shared.LogInfo(created
			? "Database couldn't be found and was created."
			: "Database already exists, skipping creation.",
			"Init");
	}
	
	private static void InitBanchoBot(IServiceScope scope)
	{
		var db = scope.ServiceProvider.GetRequiredService<BanchoDbContext>();
		var players = scope.ServiceProvider.GetRequiredService<IPlayersRepository>();
		var playerService = scope.ServiceProvider.GetRequiredService<IPlayerService>();

		var dbBanchoBot = db.Players.FirstOrDefault(p => p.Id == 1);
		if (dbBanchoBot != null)
		{
			dbBanchoBot.RemainingSupporter = DateTime.UtcNow.AddYears(100);
			dbBanchoBot.LastActivityTime = DateTime.UtcNow.AddYears(100);
			db.SaveChanges();

			var banchoBot = new User(dbBanchoBot);

			players.GetPlayerRelationships(banchoBot);
			playerService.InsertPlayer(banchoBot, true);
			return;
		}

		var banchoBotName = AppSettings.BanchoBotName;
		if (banchoBotName.Length > 16)
		{
			Logger.Shared.LogWarning("Bancho bot name is too long, truncating to 16 characters", caller: "Init");
			banchoBotName = banchoBotName[..16];
			Logger.Shared.LogInfo("New bot name: " + banchoBotName, "Init");
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
			RemainingSupporter = DateTime.UtcNow.AddYears(100),
			CreationTime = DateTime.UtcNow,
			LastActivityTime = DateTime.UtcNow.AddYears(100),
			Privileges = (int)(PlayerPrivileges.Verified | PlayerPrivileges.Staff | PlayerPrivileges.Unrestricted),
		});
		db.SaveChanges();
		
		playerService.InsertPlayer(new User(entry.Entity), true);
	}

	private static void InitChannels(IServiceScope scope)
	{
		var db = scope.ServiceProvider.GetRequiredService<BanchoDbContext>();
		var channels = scope.ServiceProvider.GetRequiredService<IChannelService>();

		if (!db.Channels.Any())
		{
			Logger.Shared.LogInfo("No channels found in database, creating default ones.", "Init");
			
			foreach (var channel in ChannelExtensions.DefaultChannels)
			{
				db.Channels.Add(new ChannelDto
				{
					Name = channel.Name,
					Description = channel.Description,
					Hidden = channel.Hidden,
					AutoJoin = channel.AutoJoin,
					ReadOnly = channel.ReadOnly,
					ReadPrivileges = (int)channel.ReadPrivileges,
					WritePrivileges = (int)channel.WritePrivileges
				});
				
				channels.InsertChannel(channel);
			}

			db.SaveChanges();
		}
		else
		{
			Logger.Shared.LogInfo("Loading channels from database.", "Init");
			
			foreach (var channel in db.Channels.ToList())
				channels.InsertChannel(new Channel(channel));
		}
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
		Logger.Shared.LogInfo($"Redis leaderboards updated in {stopwatch.ElapsedMilliseconds}ms", "Init");
	}
	
	private static string EscapeMongoCharacters(string input)
	{
		return Regex.Replace(
			Uri.EscapeDataString(input),
			@"[$:/?$[\]@]",
			m => Uri.HexEscape(Convert.ToChar(m.Value[0].ToString())));
	}
}