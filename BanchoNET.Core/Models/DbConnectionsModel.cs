namespace BanchoNET.Core.Models;

public class DbConnectionsModel
{
    public string MysqlHost { get; set; } = null!;
    public string MysqlPort { get; set; } = null!;
    public string MysqlUser { get; set; } = null!;
    public string MysqlPass { get; set; } = null!;
    public string MysqlDb { get; set; } = null!;
    
    public string RedisHost { get; set; } = null!;
    public string RedisPort { get; set; } = null!;
    public string RedisPass { get; set; } = null!;
    
    public string MongoHost { get; set; } = null!;
    public string MongoPort { get; set; } = null!;
    public string MongoUser { get; set; } = null!;
    public string MongoPass { get; set; } = null!;
}