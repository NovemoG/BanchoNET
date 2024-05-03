using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace BanchoNET.Models.Mongo;

public class RankHistory
{
    [BsonId]
    [BsonIgnoreIfDefault]
    [BsonRepresentation(BsonType.ObjectId)]
    public ObjectId Id { get; set; }
    
    public int PlayerId { get; set; }
    public byte Mode { get; set; }
    public ValueEntry PeakRank { get; set; } = null!;
    public List<ValueEntry> Entries { get; set; } = null!;
}