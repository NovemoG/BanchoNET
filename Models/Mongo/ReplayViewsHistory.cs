using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace BanchoNET.Models.Mongo;

public class ReplayViewsHistory
{
    [BsonId]
    [BsonRepresentation(BsonType.Int32)]
    public int PlayerId { get; set; }
    
    public List<ValueEntry> Entries { get; set; } = null!;
}