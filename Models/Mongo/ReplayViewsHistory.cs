using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace BanchoNET.Models.Mongo;

public class ReplayViewsHistory
{
    [BsonId]
    [BsonIgnoreIfDefault]
    [BsonRepresentation(BsonType.ObjectId)]
    public ObjectId Id { get; set; }
    
    public int PlayerId { get; set; }
    public byte Mode { get; set; }
    public List<ValueEntry> Entries { get; set; } = null!;
}