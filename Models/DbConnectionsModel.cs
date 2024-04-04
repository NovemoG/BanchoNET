namespace BanchoNET.Models;

public class DbConnectionsModel
{
    public string MysqlHost { get; set; } = null!;
    public string MysqlPort { get; set; } = null!;
    public string MysqlUser { get; set; } = null!;
    public string MysqlPass { get; set; } = null!;
    public string MysqlDb { get; set; } = null!;
    
    public string HangfireHost { get; set; } = null!;
    public string HangfirePort { get; set; } = null!;
    public string HangfireUser { get; set; } = null!;
    public string HangfirePass { get; set; } = null!;
    public string HangfireDb { get; set; } = null!;
    
    public string RedisHost { get; set; } = null!;
    public string RedisPort { get; set; } = null!;
    public string RedisPass { get; set; } = null!;
}