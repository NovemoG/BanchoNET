using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace BanchoNET.Models.Mongo;

public class PlayCountHistory
{
    [BsonId]
    [BsonIgnoreIfDefault]
    [BsonRepresentation(BsonType.ObjectId)]
    public ObjectId Id { get; set; }
    
    public int PlayerId { get; set; }
    public byte Mode { get; set; }
    public List<int> Entries { get; set; } = null!;
}