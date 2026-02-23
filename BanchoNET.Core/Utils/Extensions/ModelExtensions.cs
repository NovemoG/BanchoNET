using BanchoNET.Core.Models.Users;

namespace BanchoNET.Core.Utils.Extensions;

public static class ModelExtensions
{
    public static bool MatchesOnlineID(this User? instance, User? other) => matchesOnlineID(instance, other);
    
    private static bool matchesOnlineID(this User? instance, User? other)
    {
        if (instance == null || other == null)
            return false;

        if (instance.OnlineId < 0 || other.OnlineId < 0)
            return false;

        return instance.OnlineId.Equals(other.OnlineId);
    }
}