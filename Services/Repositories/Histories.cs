using BanchoNET.Models.Dtos;
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

    public HistoriesRepository(MongoClient client)
    {
        var mongoDatabase = client.GetDatabase("utopia");
        
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

    private static FilterDefinition<MultiplayerMatch>? MatchFilter(int matchId) =>
        Builders<MultiplayerMatch>.Filter.Eq("MatchId", matchId);

    private static FilterDefinition<RankHistory> RankFilter(int playerId, byte mode) =>
        Builders<RankHistory>.Filter.Eq("PlayerId", playerId)
        & Builders<RankHistory>.Filter.Eq("Mode", mode);
    
    private static FilterDefinition<ReplayViewsHistory> ReplayFilter(int playerId, byte mode) =>
        Builders<ReplayViewsHistory>.Filter.Eq("PlayerId", playerId)
        & Builders<ReplayViewsHistory>.Filter.Eq("Mode", mode);
    
    private static FilterDefinition<PlayCountHistory> PlayCountFilter(int playerId, byte mode) =>
        Builders<PlayCountHistory>.Filter.Eq("PlayerId", playerId)
        & Builders<PlayCountHistory>.Filter.Eq("Mode", mode);

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
        return await _multiplayerMatches.Find(MatchFilter(matchId)).SingleAsync();
    }

    public async Task AddMatchAction(int matchId, ActionEntry entry)
    {
        var builder = Builders<MultiplayerMatch>.Update.Push("Actions", entry);
        
        var result = await _multiplayerMatches.UpdateOneAsync(MatchFilter(matchId), builder);
        
        if (result.MatchedCount == 0)
            Console.WriteLine("[Histories] Match not found in multiplayer history");
    }

    public async Task MapStarted(int matchId, ScoresEntry entry)
    {
        var update = Builders<MultiplayerMatch>.Update.Push("Scores", entry);
        
        var result = await _multiplayerMatches.UpdateOneAsync(MatchFilter(matchId), update);
        
        if (result.MatchedCount == 0)
            Console.WriteLine("[Histories] Match not found in multiplayer history");
    }

    public async Task MapCompleted(int matchId, List<ScoreDto> scores)
    {
        var entries = scores.Select(score => new ScoreEntry
            {
                Accuracy = score.Acc,
                Gekis = score.Gekis,
                Count300 = score.Count300,
                Katus = score.Katus,
                Count100 = score.Count100,
                Count50 = score.Count50,
                Misses = score.Misses,
                MaxCombo = score.MaxCombo,
                Mods = score.Mods,
                PlayerId = score.PlayerId,
                TotalScore = score.Score,
                Failed = score.Status == 0
            })
            .ToList();
        
        //TODO insert into last scores element
        var updateScores = Builders<MultiplayerMatch>.Update.Set("Scores.Values.-1", entries);
        
        var result = await _multiplayerMatches.UpdateOneAsync(MatchFilter(matchId), updateScores);
        
        if (result.MatchedCount == 0)
            Console.WriteLine("[Histories] Match not found in multiplayer history");
    }
    
    public async Task InsertRankHistory(RankHistory history)
    {
        await _rankHistories.InsertOneAsync(history);
    }

    public async Task<List<ValueEntry>> GetRankHistory(int playerId, byte mode)
    {
        var result = await _rankHistories.Find(RankFilter(playerId, mode)).SingleAsync();
        
        List<ValueEntry> entries =
        [
            result.PeakRank
        ];
        entries.AddRange(result.Entries);
        
        return entries;
    }
    
    public async Task AddRankHistory(int playerId, byte mode, ValueEntry entry)
    {
        var filter = RankFilter(playerId, mode);
        var update = Builders<RankHistory>.Update.Push("Entries", entry);
        
        var history = await _rankHistories.Find(filter).SingleAsync();
        if (history.Entries.Count == 90)
        {
            var pop = Builders<RankHistory>.Update.PopFirst("Entries");
            
            await _rankHistories.UpdateOneAsync(filter, pop);
        }
        
        var result = await _rankHistories.UpdateOneAsync(filter, update);
        
        if (result.MatchedCount == 0)
            Console.WriteLine("[Histories] Player not found in rank history");
    }
    
    public async Task UpdatePeakRank(int playerId, byte mode, ValueEntry peakRank)
    {
        var update = Builders<RankHistory>.Update.Set("PeakRank", peakRank);
        
        var result = await _rankHistories.UpdateOneAsync(RankFilter(playerId, mode), update);
        
        if (result.MatchedCount == 0)
            Console.WriteLine("[Histories] Player not found in rank history");
    }
    
    public async Task InsertReplaysHistory(ReplayViewsHistory history)
    {
        await _replayViewsHistories.InsertOneAsync(history);
    }
    
    public async Task<List<ValueEntry>> GetReplaysHistory(int playerId, byte mode)
    {
        var result = await _replayViewsHistories.Find(ReplayFilter(playerId, mode)).SingleAsync();
        
        return result.Entries;
    }
    
    public async Task AddReplaysHistory(int playerId, byte mode, ValueEntry entry)
    {
        var update = Builders<ReplayViewsHistory>.Update.Push("Entries", entry);
        
        var result = await _replayViewsHistories.UpdateOneAsync(ReplayFilter(playerId, mode), update);
        
        if (result.MatchedCount == 0)
            Console.WriteLine("[Histories] Player not found in views history");
    }
    
    public async Task InsertPlayCountHistory(PlayCountHistory history)
    {
        await _playCountHistories.InsertOneAsync(history);
    }
    
    public async Task<List<ValueEntry>> GetPlayCountHistory(int playerId, byte mode)
    {
        var result = await _playCountHistories.Find(PlayCountFilter(playerId, mode)).SingleAsync();
        
        return result.Entries;
    }
    
    public async Task AddPlayCountHistory(int playerId, byte mode, ValueEntry entry)
    {
        var update = Builders<PlayCountHistory>.Update.Push("Entries", entry);
        
        var result = await _playCountHistories.UpdateOneAsync(PlayCountFilter(playerId, mode), update);
        
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