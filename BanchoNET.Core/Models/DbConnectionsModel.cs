namespace BanchoNET.Core.Models;

public class DbConnectionsModel
{
    public string PostgresHost { get; set; } = null!;
    public string PostgresPort { get; set; } = null!;
    public string PostgresUser { get; set; } = null!;
    public string PostgresPass { get; set; } = null!;
    public string PostgresDb { get; set; } = null!;
    
    public string RedisHost { get; set; } = null!;
    public string RedisPort { get; set; } = null!;
    public string RedisPass { get; set; } = null!;
}