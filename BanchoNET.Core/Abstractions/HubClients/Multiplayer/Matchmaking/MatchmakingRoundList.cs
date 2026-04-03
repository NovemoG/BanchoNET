using System.Collections;
using MessagePack;

namespace BanchoNET.Core.Abstractions.HubClients.Multiplayer.Matchmaking;

[Serializable]
[MessagePackObject]
public class MatchmakingRoundList : IEnumerable<MatchmakingRound>
{
    [Key(0)]
    public IDictionary<int, MatchmakingRound> RoundsDictionary { get; set; } = new Dictionary<int, MatchmakingRound>();
    
    [IgnoreMember]
    public int Count => RoundsDictionary.Count;
    
    public MatchmakingRound GetOrAdd(int round)
    {
        if (RoundsDictionary.TryGetValue(round, out var score))
            return score;

        return RoundsDictionary[round] = new MatchmakingRound { Round = round };
    }

    public IEnumerator<MatchmakingRound> GetEnumerator() => RoundsDictionary.Values.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}