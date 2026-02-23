using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace BanchoNET.Core.Models.Mongo;

public class RankHistory
{
    [BsonId]
    [BsonIgnoreIfDefault]
    [BsonRepresentation(BsonType.ObjectId)]
    public ObjectId Id { get; set; }
    
    public int PlayerId { get; set; }
    public byte Mode { get; set; }
    public PeakRank PeakRank { get; set; } = null!;
    public List<int> Entries { get; set; } = null!;
}