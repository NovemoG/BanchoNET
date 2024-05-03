using BanchoNET.Models.Mongo;
using MongoDB.Bson;
using MongoDB.Driver;

namespace BanchoNET.Services.Repositories;

public class HistoriesRepository
{
    private readonly IMongoCollection<MultiplayerMatch> _multiplayerMatches;
    private readonly IMongoCollection<RankHistory> _rankHistories;
    private readonly IMongoCollection<ReplayViewsHistory> _replayViewsHistories;
    private readonly IMongoCollection<PlayCountHistory> _playCountHistories;
    
    public HistoriesRepository(string connectionString)
    {
        var mongoClient = new MongoClient(connectionString);
        var mongoDatabase = mongoClient.GetDatabase("utopia");
        
        if (!CollectionExists(mongoDatabase, "rankHistories"))
            mongoDatabase.CreateCollection("rankHistories");
        
        if (!CollectionExists(mongoDatabase, "replayViewsHistories"))
            mongoDatabase.CreateCollection("replayViewsHistories");
        
        if (!CollectionExists(mongoDatabase, "playCountHistories"))
            mongoDatabase.CreateCollection("playCountHistories");
        
        if (!CollectionExists(mongoDatabase, "multiplayerMatches"))
            mongoDatabase.CreateCollection("multiplayerMatches");
        
        _multiplayerMatches = mongoDatabase.GetCollection<MultiplayerMatch>("multiplayerMatches");
        _rankHistories = mongoDatabase.GetCollection<RankHistory>("rankHistories");
        _replayViewsHistories = mongoDatabase.GetCollection<ReplayViewsHistory>("replayViewsHistories");
        _playCountHistories = mongoDatabase.GetCollection<PlayCountHistory>("playCountHistories");
    }

    private static bool CollectionExists(IMongoDatabase db, string name)
    {
        var filter = new BsonDocument("name", name);
        var collections = db.ListCollectionNames(new ListCollectionNamesOptions { Filter = filter });
        return collections.Any();
    }
}