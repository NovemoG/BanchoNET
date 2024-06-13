using BanchoNET.Models.Mongo;
using MongoDB.Bson;
using MongoDB.Driver;

namespace BanchoNET.Services.Repositories;

public class HistoriesRepository
{
    #region Constructor

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

    #endregion

    #region Filters

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

    #endregion

    private static readonly ScoreEntry DummyScore = new() {
        Accuracy = 0.0f,
        Grade = 0,
        Gekis = 0,
        Count300 = 0,
        Katus = 0,
        Count100 = 0,
        Count50 = 0,
        Misses = 0,
        MaxCombo = 0,
        Mods = 0,
        PlayerId = 0,
        TotalScore = 0,
        Team = 0,
        Failed = false
    };

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
        
        if (result.ModifiedCount == 0)
            Console.WriteLine("[Histories] Couldn't insert action, match not found in multiplayer history");
    }

    public async Task AddMatchActions(int matchId, List<ActionEntry> entries)
    {
        var builder = Builders<MultiplayerMatch>.Update.PushEach("Actions", entries);
        
        var result = await _multiplayerMatches.UpdateOneAsync(MatchFilter(matchId), builder);
        
        if (result.ModifiedCount == 0)
            Console.WriteLine("[Histories] Couldn't insert action, match not found in multiplayer history");
    }

    public async Task MapStarted(int matchId, ScoresEntry entry)
    {
        var update = Builders<MultiplayerMatch>.Update.Push("Scores", entry);
        
        var result = await _multiplayerMatches.UpdateOneAsync(MatchFilter(matchId), update);
        
        if (result.ModifiedCount == 0)
            Console.WriteLine("[Histories] Couldn't insert map, match not found in multiplayer history");
    }

    public async Task MapAborted(int matchId)
    {
        var filter = MatchFilter(matchId)
                     & Builders<MultiplayerMatch>.Filter.ElemMatch(e => e.Scores,
                         score => score.Values.Count == 0);
        
        // subject to change (maybe insert a dummy score instead of deleting entry?)
        var result = await _multiplayerMatches.DeleteOneAsync(filter);
        
        if (result.DeletedCount == 0)
            Console.WriteLine("[Histories] Couldn't delete, match not found in multiplayer history");
    }
    
    public async Task MapCompleted(int matchId, List<ScoreEntry> scores)
    {
        // idk if there is a chance that a match is completed without any scores, but this is a safety system
        // to prevent any possible errors
        if (scores.Count == 0)
            scores = [DummyScore];

        var filter = MatchFilter(matchId) &
                     Builders<MultiplayerMatch>.Filter.ElemMatch(e => e.Scores, score => score.Values.Count == 0);
        
        var update = Builders<MultiplayerMatch>.Update.Set("Scores.$.Values", scores);
        
        var result = await _multiplayerMatches.UpdateOneAsync(filter, update);
        
        Console.WriteLine(result.ModifiedCount);
        
        if (result.ModifiedCount == 0)
            Console.WriteLine("[Histories] Couldn't update scores, match not found in multiplayer history");
    }
    
    public async Task InsertRankHistory(RankHistory history)
    {
        await _rankHistories.InsertOneAsync(history);
    }

    public async Task<List<int>> GetRankHistory(int playerId, byte mode)
    {
        var result = await _rankHistories.Find(RankFilter(playerId, mode)).SingleAsync();
        
        List<int> entries =
        [
            result.PeakRank.Value
        ];
        entries.AddRange(result.Entries);
        
        return entries;
    }
    
    public async Task AddRankHistory(int playerId, byte mode, int entry)
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
        
        if (result.ModifiedCount == 0)
            Console.WriteLine("[Histories] Couldn't insert rank, player not found in rank history");
    }
    
    public async Task UpdatePeakRank(int playerId, byte mode, PeakRank peakRank)
    {
        var update = Builders<RankHistory>.Update.Set("PeakRank", peakRank);
        
        var result = await _rankHistories.UpdateOneAsync(RankFilter(playerId, mode), update);
        
        if (result.ModifiedCount == 0)
            Console.WriteLine("[Histories] Couldn't update peak rank, player not found in rank history");
    }
    
    public async Task InsertReplaysHistory(ReplayViewsHistory history)
    {
        await _replayViewsHistories.InsertOneAsync(history);
    }
    
    public async Task<List<int>> GetReplaysHistory(int playerId, byte mode)
    {
        var result = await _replayViewsHistories.Find(ReplayFilter(playerId, mode)).SingleAsync();
        
        return result.Entries;
    }
    
    public async Task AddReplaysHistory(int playerId, byte mode, int entry)
    {
        var update = Builders<ReplayViewsHistory>.Update.Push("Entries", entry);
        
        var result = await _replayViewsHistories.UpdateOneAsync(ReplayFilter(playerId, mode), update);
        
        if (result.ModifiedCount == 0)
            Console.WriteLine("[Histories] Couldn't insert replays history, player not found in views history");
    }
    
    public async Task InsertPlayCountHistory(PlayCountHistory history)
    {
        await _playCountHistories.InsertOneAsync(history);
    }
    
    public async Task<List<int>> GetPlayCountHistory(int playerId, byte mode)
    {
        var result = await _playCountHistories.Find(PlayCountFilter(playerId, mode)).SingleAsync();
        
        return result.Entries;
    }
    
    public async Task AddPlayCountHistory(int playerId, byte mode, int entry)
    {
        var update = Builders<PlayCountHistory>.Update.Push("Entries", entry);
        
        var result = await _playCountHistories.UpdateOneAsync(PlayCountFilter(playerId, mode), update);
        
        if (result.ModifiedCount == 0)
            Console.WriteLine("[Histories] Couldn't insert play count history, player not found in play count history");
    }

    public async Task DeletePlayerData(int playerId)
    {
        var rankFilter = Builders<RankHistory>.Filter.Eq("PlayerId", playerId);
        var replayFilter = Builders<ReplayViewsHistory>.Filter.Eq("PlayerId", playerId);
        var playCountFilter = Builders<PlayCountHistory>.Filter.Eq("PlayerId", playerId);
        
        var rankResult = await _rankHistories.DeleteOneAsync(rankFilter);
        
        if (rankResult.DeletedCount == 0)
        {
            Console.WriteLine("[Histories] Couldn't delete player's histories, player not found in db");
            return;
        }
        
        await _replayViewsHistories.DeleteOneAsync(replayFilter);
        await _playCountHistories.DeleteOneAsync(playCountFilter);
    }

    private static bool CollectionExists(IMongoDatabase db, string name)
    {
        var filter = new BsonDocument("name", name);
        var collections = db.ListCollectionNames(new ListCollectionNamesOptions { Filter = filter });
        return collections.Any();
    }
}