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
    
    public async Task InsertMatchHistory(MultiplayerMatch history)
    {
        await _multiplayerMatches.InsertOneAsync(history);
    }
    
    public async Task<int> GetMatchId()
    {
        return (int)await _multiplayerMatches.CountDocumentsAsync(new BsonDocument()) + 1;
    }

    public async Task<MultiplayerMatch> GetMultiplayerMatch(int matchId)
    {
        var filter = Builders<MultiplayerMatch>.Filter.Eq("MatchId", matchId);
        return await _multiplayerMatches.Find(filter).SingleAsync();
    }

    public async Task AddMatchAction(int matchId, ActionEntry entry, bool creation = false)
    {
        var fieldName = creation ? "CreationActions" : "";
        
        var filter = Builders<MultiplayerMatch>.Filter.Eq("MatchId", matchId);
        var builder = creation
            ? Builders<MultiplayerMatch>.Update.Push("CreationActions", entry)
            : Builders<MultiplayerMatch>.Update.Push<ActionEntry>(p => p.Scores.Last().Actions, entry);
        
        var result = await _multiplayerMatches.UpdateOneAsync(filter, builder);
        
        if (result.MatchedCount == 0)
            Console.WriteLine("[Histories] Match not found in multiplayer history");
    }
    
    public async Task AddMatchScores(int matchId, ScoresEntry entry)
    {
        var filter = Builders<MultiplayerMatch>.Filter.Eq("MatchId", matchId);
        var update = Builders<MultiplayerMatch>.Update.Push("Scores", entry);
        
        var result = await _multiplayerMatches.UpdateOneAsync(filter, update);
        
        if (result.MatchedCount == 0)
            Console.WriteLine("[Histories] Match not found in multiplayer history");
    }
    
    public async Task InsertRankHistory(RankHistory history)
    {
        await _rankHistories.InsertOneAsync(history);
    }
    
    public async Task UpdateRankHistory(int playerId, ValueEntry entry)
    {
        var filter = Builders<RankHistory>.Filter.Eq("PlayerId", playerId);
        var update = Builders<RankHistory>.Update.Push("Entries", entry);

        var result = await _rankHistories.UpdateOneAsync(filter, update);
        
        if (result.MatchedCount == 0)
            Console.WriteLine("[Histories] Player not found in rank history");
    }
    
    public async Task UpdatePeakRank(int playerId, ValueEntry peakRank)
    {
        var filter = Builders<RankHistory>.Filter.Eq("PlayerId", playerId);
        var update = Builders<RankHistory>.Update.Set("PeakRank", peakRank);
        
        var result = await _rankHistories.UpdateOneAsync(filter, update);
        
        if (result.MatchedCount == 0)
            Console.WriteLine("[Histories] Player not found in rank history");
    }
    
    public async Task InsertReplayHistory(ReplayViewsHistory history)
    {
        await _replayViewsHistories.InsertOneAsync(history);
    }
    
    public async Task UpdateReplayHistory(int replayId, ValueEntry entry)
    {
        var filter = Builders<ReplayViewsHistory>.Filter.Eq("PlayerId", replayId);
        var update = Builders<ReplayViewsHistory>.Update.Push("Entries", entry);
        
        var result = await _replayViewsHistories.UpdateOneAsync(filter, update);
        
        if (result.MatchedCount == 0)
            Console.WriteLine("[Histories] Player not found in views history");
    }
    
    public async Task InsertPlayCountHistory(PlayCountHistory history)
    {
        await _playCountHistories.InsertOneAsync(history);
    }
    
    public async Task UpdatePlayCountHistory(int beatmapId, ValueEntry entry)
    {
        var filter = Builders<PlayCountHistory>.Filter.Eq("PlayerId", beatmapId);
        var update = Builders<PlayCountHistory>.Update.Push("Entries", entry);
        
        var result = await _playCountHistories.UpdateOneAsync(filter, update);
        
        if (result.MatchedCount == 0)
            Console.WriteLine("[Histories] Player not found in play count history");
    }

    private static bool CollectionExists(IMongoDatabase db, string name)
    {
        var filter = new BsonDocument("name", name);
        var collections = db.ListCollectionNames(new ListCollectionNamesOptions { Filter = filter });
        return collections.Any();
    }
}