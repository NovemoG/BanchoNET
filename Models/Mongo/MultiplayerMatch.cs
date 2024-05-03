using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace BanchoNET.Models.Mongo;

public class MultiplayerMatch
{
    [BsonId]
    [BsonRepresentation(BsonType.Int32)]
    public int Id { get; set; }
    
    public string Name { get; set; } = null!;
    public List<ActionEntry> CreationActions { get; set; } = null!;
    public List<ScoresEntry> Scores { get; set; } = null!;
}