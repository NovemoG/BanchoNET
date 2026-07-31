using BanchoNET.Core.Abstractions.Repositories.Histories;
using BanchoNET.Core.Models.Mongo;
using MongoDB.Bson;
using MongoDB.Driver;

namespace BanchoNET.Services.Repositories;

public class HistoriesRepository : IHistoriesRepository
{
    #region Constructor

    private readonly IMongoCollection<MultiplayerMatch> _multiplayerMatches;
    private readonly IMongoCollection<RankHistoryEntry> _rankHistories;
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
        _rankHistories = mongoDatabase.GetCollection<RankHistoryEntry>("rankHistories");
        _replayViewsHistories = mongoDatabase.GetCollection<ReplayViewsHistory>("replayViewsHistories");
        _playCountHistories = mongoDatabase.GetCollection<PlayCountHistory>("playCountHistories");
    }

    #endregion

    #region Filters

    private static FilterDefinition<MultiplayerMatch>? MatchFilter(long matchId) =>
        Builders<MultiplayerMatch>.Filter.Eq("MatchId", matchId);

    private static FilterDefinition<RankHistoryEntry> RankFilter(int playerId, byte mode) =>
        Builders<RankHistoryEntry>.Filter.Eq("PlayerId", playerId)
        & Builders<RankHistoryEntry>.Filter.Eq("Mode", mode);
    
    private static FilterDefinition<ReplayViewsHistory> ReplayFilter(int playerId, byte mode) =>
        Builders<ReplayViewsHistory>.Filter.Eq("PlayerId", playerId)
        & Builders<ReplayViewsHistory>.Filter.Eq("Mode", mode);
    
    private static FilterDefinition<PlayCountHistory> PlayCountFilter(int playerId, byte mode) =>
        Builders<PlayCountHistory>.Filter.Eq("PlayerId", playerId)
        & Builders<PlayCountHistory>.Filter.Eq("Mode", mode);

    #endregion

    private readonly object _idLock = new();

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

    public async Task<long> InsertMatchHistory(MultiplayerMatch history)
    {
        await _multiplayerMatches.InsertOneAsync(history);
        return history.MatchId;
    }

    public async Task<MultiplayerMatch> GetMultiplayerMatch(long matchId)
    {
        return await _multiplayerMatches.Find(MatchFilter(matchId)).SingleAsync();
    }

    public async Task AddMatchAction(long matchId, ActionEntry entry)
    {
        var builder = Builders<MultiplayerMatch>.Update.Push("Actions", entry);
        
        var result = await _multiplayerMatches.UpdateOneAsync(MatchFilter(matchId), builder);
        
        if (result.ModifiedCount == 0)
            Console.WriteLine("[Histories] Couldn't insert action, match not found in multiplayer history");
    }

    public async Task AddMatchActions(long matchId, IEnumerable<ActionEntry> entries)
    {
        var builder = Builders<MultiplayerMatch>.Update.PushEach("Actions", entries);
        
        var result = await _multiplayerMatches.UpdateOneAsync(MatchFilter(matchId), builder);
        
        if (result.ModifiedCount == 0)
            Console.WriteLine("[Histories] Couldn't insert action, match not found in multiplayer history");
    }

    public async Task MapStarted(long matchId, ScoresEntry entry)
    {
        var update = Builders<MultiplayerMatch>.Update.Push("Scores", entry);
        
        var result = await _multiplayerMatches.UpdateOneAsync(MatchFilter(matchId), update);
        
        if (result.ModifiedCount == 0)
            Console.WriteLine("[Histories] Couldn't insert map, match not found in multiplayer history");
    }

    public async Task MapAborted(long matchId)
    {
        var filter = MatchFilter(matchId)
                     & Builders<MultiplayerMatch>.Filter.ElemMatch(e => e.Scores,
                         score => score.Values.Count == 0);
        
        // subject to change (maybe insert a dummy score instead of deleting entry?)
        var result = await _multiplayerMatches.DeleteOneAsync(filter);
        
        if (result.DeletedCount == 0)
            Console.WriteLine("[Histories] Couldn't delete, match not found in multiplayer history");
    }
    
    public async Task MapCompleted(long matchId, List<ScoreEntry> scores)
    {
        // idk if there is a chance that a match is completed without any scores, but this is a safety system
        // to prevent any possible errors
        if (scores.Count == 0)
            scores = [DummyScore];

        var filter = MatchFilter(matchId) &
                     Builders<MultiplayerMatch>.Filter.ElemMatch(e => e.Scores, score => score.Values.Count == 0);
        
        var update = Builders<MultiplayerMatch>.Update.Set("Scores.$.Values", scores);
        
        var result = await _multiplayerMatches.UpdateOneAsync(filter, update);
        
        if (result.ModifiedCount == 0)
            Console.WriteLine("[Histories] Couldn't update scores, match not found in multiplayer history");
    }
    
    public async Task<List<RankHistoryEntry>> GetRankHistories(byte mode)
    {
        var filter = Builders<RankHistoryEntry>.Filter.Eq("Mode", mode);

        return await _rankHistories.Find(filter).ToListAsync();
    }
    
    public async Task<List<ReplayViewsHistory>> GetReplaysHistories(byte mode)
    {
        var filter = Builders<ReplayViewsHistory>.Filter.Eq("Mode", mode);

        return await _replayViewsHistories.Find(filter).ToListAsync();
    }
    
    public async Task<List<PlayCountHistory>> GetPlayCountHistories(byte mode)
    {
        var filter = Builders<PlayCountHistory>.Filter.Eq("Mode", mode);

        return await _playCountHistories.Find(filter).ToListAsync();
    }
    
    public async Task DeletePlayerData(int playerId)
    {
        var rankFilter = Builders<RankHistoryEntry>.Filter.Eq("PlayerId", playerId);
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