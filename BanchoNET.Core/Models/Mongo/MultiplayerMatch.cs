using MongoDB.Bson.Serialization.Attributes;

namespace BanchoNET.Core.Models.Mongo;

public class MultiplayerMatch
{
    [BsonId]
    public long MatchId { get; set; }
    
    public string Name { get; set; } = null!;
    public List<ActionEntry> Actions { get; set; } = null!;
    public List<ScoresEntry> Scores { get; set; } = null!;
}