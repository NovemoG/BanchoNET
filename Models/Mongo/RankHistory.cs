using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace BanchoNET.Models.Mongo;

public class RankHistory
{
    [BsonId]
    [BsonRepresentation(BsonType.Int32)]
    public int PlayerId { get; set; }

    public ValueEntry PeakRank { get; set; } = null!;
    public List<ValueEntry> Entries { get; set; } = null!;
}