using System.Diagnostics;
using System.Net;
using System.Net.Http.Headers;
using System.Reflection;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using BanchoNET.BeatmapServer.Services;
using BanchoNET.Bot.Utils;
using BanchoNET.Core.Abstractions;
using BanchoNET.Core.Abstractions.Bancho.Services;
using BanchoNET.Core.Abstractions.Repositories;
using BanchoNET.Core.Abstractions.Repositories.Histories;
using BanchoNET.Core.Abstractions.Services;
using BanchoNET.Core.Abstractions.Services.Lazer;
using BanchoNET.Core.Models;
using BanchoNET.Core.Models.Channels;
using BanchoNET.Core.Models.Db;
using BanchoNET.Core.Models.Dtos;
using BanchoNET.Core.Models.Players;
using BanchoNET.Core.Models.Privileges;
using BanchoNET.Core.Utils;
using BanchoNET.Core.Utils.Extensions;
using BanchoNET.Core.Utils.SignalR;
using BanchoNET.Handlers.Lazer.Hubs;
using BanchoNET.Handlers.Lazer.Services;
using BanchoNET.Handlers.Stable.Commands;
using BanchoNET.Handlers.Stable.Services;
using BanchoNET.Handlers.Stable.Services.ClientPacketsHandler;
using BanchoNET.Handlers.Stable.Services.LobbyScoresQueue;
using BanchoNET.Infrastructure.Bancho.Coordinators;
using BanchoNET.Middlewares;
using BanchoNET.Services;
using BanchoNET.Services.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using Novelog.Config;
using StackExchange.Redis;
using BanchoNET.Infrastructure;
using BanchoNET.Infrastructure.Bancho.Services;
using BanchoNET.Infrastructure.Services;
using DSharpPlus;
using Microsoft.AspNetCore.SignalR;
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
			"CLIENT_ID",
			"CLIENT_SECRET",
			"GITHUB_TOKEN"
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
			$"Host={dbConnections.MysqlHost};" +
			$"Port=5432;" +
			$"Username={dbConnections.MysqlUser};" +
			$"Password={dbConnections.MysqlPass};" +
			$"Database={dbConnections.MysqlDb};";

		if (AppSettings.Debug)
			mySqlConnectionString += "Include Error Detail=True;";

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

		builder.Services
			.AddEndpointsApiExplorer()
			.AddAuthorization()
			.AddOAuth()
			.AddControllers()
			.AddJsonOptions(o =>
			{
				o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
			});

		var mongoSettings = MongoClientSettings.FromConnectionString(mongoConnectionString);

		void ConfigureBancho(DbContextOptionsBuilder options)
		{
			options.UseNpgsql(mySqlConnectionString);
		}
		
		builder.Services
			.AddSingleton<IConnectionMultiplexer>(ConnectionMultiplexer.Connect(redisConnectionString))
			.AddSingleton(new MongoClient(mongoSettings))
			.AddDbContext<BanchoDbContext>(ConfigureBancho)
			.AddDbContextFactory<BanchoDbContext>(ConfigureBancho)
			.AddSingleton<IHistoriesRepository, HistoriesRepository>();
		
		//TODO
		builder.Services.AddScoped<IBeatmapsRepository, BeatmapsRepository>();
		builder.Services.AddScoped<IClientsRepository, ClientsRepository>();
		builder.Services.AddScoped<IMessagesRepository, MessagesRepository>();
		builder.Services.AddScoped<IPlayersRepository, PlayersRepository>();
		builder.Services.AddScoped<ILegacyScoresRepository, LegacyScoresRepository>();
		builder.Services.AddScoped<ILazerScoresRepository, LazerScoresRepository>();
		builder.Services.AddScoped<IReleasesRepository, ReleasesRepository>();
		builder.Services.AddScoped<ICommentsRepository, CommentsRepository>();
		builder.Services.AddScoped<IBeatmapHandler, BeatmapHandler>();
		builder.Services.AddScoped<ISearchProjectionSyncService, SearchProjectionSyncService>();
		builder.Services.AddScoped<IBeatmapSearchService, BeatmapSearchService>();
			
		builder.Services
			.AddSingleton<ScoreSubmissionQueue>()
			.AddSingleton<IScoreSubmissionQueue>(sp => sp.GetRequiredService<ScoreSubmissionQueue>())
			.AddHostedService(sp => sp.GetRequiredService<ScoreSubmissionQueue>())
			.AddSingleton<ILobbyScoresQueue, LobbyScoresQueue>()
			.AddHostedService<LobbyQueueHostedService>();

		builder.Services
			.AddScoped<IGeolocService, GeolocService>()
			.AddScoped<IClientPacketsHandler, ClientPacketsHandler>()
			.AddScoped<ICommandProcessor, CommandProcessor>();
		
		builder.Services
			.AddSingleton<ILazerPlayerService, LazerPlayerService>()
			.AddSingleton<INotifySocketManager, NotifySocketManager>()
			.AddSingleton<OsuVersionService>()
			.AddSingleton<IOsuVersionService>(sp => sp.GetRequiredService<OsuVersionService>())
			.AddHostedService(sp => sp.GetRequiredService<OsuVersionService>())
			.AddHostedService<BackgroundTasks>();

		builder.Services
			.AddHostedService<LazerUpdaterService>()
			.AddHttpClient(nameof(LazerUpdaterService), client =>
			{
				client.DefaultRequestHeaders.Accept.Add(
					new MediaTypeWithQualityHeaderValue("application/vnd.github+json")
				);
				client.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue(new ProductHeaderValue("request")));
        
				if (!string.IsNullOrWhiteSpace(AppSettings.GithubToken))
					client.DefaultRequestHeaders.TryAddWithoutValidation("Authorization", "token " + AppSettings.GithubToken);
			});

		builder.Services
			.AddSingleton<OsuTokenProvider>()
			.AddTransient<OsuAuthHandler>()
			.AddHttpClient(nameof(BeatmapHandler), client =>
			{
				client.BaseAddress = new Uri("https://osu.ppy.sh/api/v2");
			})
			.AddHttpMessageHandler<OsuAuthHandler>();
		
		builder.Services
			.AddSingleton<IBeatmapDownloader, BeatmapDownloader>()
			.AddHttpClient(nameof(BeatmapDownloader), client =>
			{
				client.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/131.0.0.0 Safari/537.36");
				client.DefaultRequestHeaders.TryAddWithoutValidation("Referer", "https://osu.ppy.sh/beatmapsets/");
				client.Timeout = TimeSpan.FromSeconds(5);
			})
			.ConfigurePrimaryHttpMessageHandler(() =>
			{
				var container = new CookieContainer();
				
				var cookie = new Cookie("osu_session", AppSettings.OsuCookie, "/", ".ppy.sh")
				{
					Secure = true,
					HttpOnly = true
				};
				
				container.Add(cookie);

				return new HttpClientHandler
				{
					CookieContainer = container,
					UseCookies = true,
					AllowAutoRedirect = true
				};
			});
		
		Assembly[] assemblies = [
			typeof(ICoordinator).Assembly,
			typeof(PlayerCoordinator).Assembly
		];

		builder.Services
			.AddHttpClient()
			.AddMemoryCache()
			.AddSessionServices(assemblies)
			.AddSingleton<IUserIdProvider, SubUserIdProvider>()
			.AddSignalR(options =>
			{
				if (AppSettings.Debug)
					options.EnableDetailedErrors = true;
			})
			.AddMessagePackProtocol(options => 
				options.SerializerOptions = SignalRUnionWorkaroundResolver.Options
			);

		builder.Services.AddDiscordBot(
			AppSettings.DiscordToken,
			AppSettings.DiscordDebugGuildId,
			DiscordIntents.AllUnprivileged
		);
		
		var app = builder.Build();
		
		app.UseHttpsRedirection();
		app.UseAuthentication();
		app.UseAuthorization();
		app.UseWebSockets();
		
		app.UseMiddleware<SubdomainMiddleware>();
		app.UseMiddleware<RequestTimingMiddleware>();
		
		app.MapHub<MetadataHub>("/metadata");
		app.MapHub<SpectatorHub>("/spectator");
		app.MapHub<MultiplayerHub>("/multiplayer");
		
		app.MapControllers();

		#region Initialization
		
		EnsureDatabaseExists(app.Services.CreateScope());
		
		InitBanchoBot(app.Services.CreateScope());
		InitChannels(app.Services.CreateScope());
		
		// Even if redis creates snapshots of rankings it isn't
		// always 100% accurate with database so we need to update
		// redis leaderboards on startup
		InitRedis(app.Services.CreateScope(), dbConnections.RedisHost, dbConnections.RedisPort);

		#endregion
		
		initStopwatch.Stop();
		Logger.Shared.LogInfo($"Initialization took: {initStopwatch.Elapsed}", "Init");
		
		app.Run();
	}
	
	private static void EnsureDatabaseExists(IServiceScope scope)
	{
		var db = scope.ServiceProvider.GetRequiredService<BanchoDbContext>();

		Logger.Shared.LogInfo("Applying database migrations.", "Init");
		
		db.Database.Migrate();
		
		Logger.Shared.LogInfo("Database is ready.", "Init");
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

			var banchoBot = new Player(dbBanchoBot, loginTime: DateTime.UtcNow);

			players.FetchPlayerRelationships(banchoBot);
			playerService.InsertPlayer(banchoBot, isBot: true);
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
		
		playerService.InsertPlayer(new Player(entry.Entity, loginTime: DateTime.UtcNow), isBot: true);
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
			
			foreach (var channel in db.Channels.Where(c => c.Type != ChannelType.PM).ToList())
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