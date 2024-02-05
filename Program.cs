using BanchoNET.Models;
using BanchoNET.Services;
using BanchoNET.Utils;
using Microsoft.EntityFrameworkCore;

namespace BanchoNET;

public class Program
{
	public static void Main(string[] args)
	{
		var builder = WebApplication.CreateBuilder(args);
		var mySqlConnectionString = builder.Configuration.GetConnectionString("MySql")!;
		
		/*builder.Services.AddHangfire(config =>
		{
			config.UseSqlServerStorage(() => new MySqlConnection(mySqlConnectionString));
		});
		builder.Services.AddHangfireServer();*/
		
		builder.Services.AddEndpointsApiExplorer();
		builder.Services.AddAuthorization();
		builder.Services.AddControllers();

		builder.Services.Configure<ServerConfig>(builder.Configuration.GetSection("ServerConfig"));
		builder.Services.AddDbContext<BanchoDbContext>(options =>
		{
			options.UseMySQL(mySqlConnectionString);
		});
		builder.Services.AddSingleton<BanchoSession>();
		builder.Services.AddScoped<BanchoHandler>();
		builder.Services.AddHttpClient();

		var app = builder.Build();

		//app.UseHangfireDashboard();
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