using System.Collections;
using MessagePack;

namespace BanchoNET.Core.Abstractions.HubClients.Multiplayer.Matchmaking;

[Serializable]
[MessagePackObject]
public class MatchmakingUserList : IEnumerable<MatchmakingUser>
{
    [Key(0)]
    public IDictionary<int, MatchmakingUser> UserDictionary { get; set; } = new Dictionary<int, MatchmakingUser>();
    
    [IgnoreMember]
    public int Count => UserDictionary.Count;
    
    public MatchmakingUser GetOrAdd(int userId)
    {
        if (UserDictionary.TryGetValue(userId, out var user))
            return user;

        return UserDictionary[userId] = new MatchmakingUser { UserId = userId };
    }

    public IEnumerator<MatchmakingUser> GetEnumerator() => UserDictionary.Values.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}