using BanchoNET.Core.Models.Channels;
using BanchoNET.Core.Models.Multiplayer;
using BanchoNET.Core.Models.Users;

namespace BanchoNET.Core.Utils.Extensions;

public static class ModelExtensions
{
    extension(
        User? instance
    ) {
        public bool MatchesOnlineID(User? other) => instance.matchesOnlineID(other);

        private bool matchesOnlineID(
            User? other
        ) {
            if (instance is null || other is null)
                return false;

            if (instance.OnlineId < 0 || other.OnlineId < 0)
                return false;

            return instance.OnlineId.Equals(other.OnlineId);
        }
    }

    extension(
        MultiplayerMatch? instance
    ) {
        public bool MatchesOnlineId(MultiplayerMatch? other) => instance.matchesOnlineID(other);

        private bool matchesOnlineID(
            MultiplayerMatch? other
        ) {
            if (instance is null || other is null)
                return false;

            if (instance.OnlineId < 0 || other.OnlineId < 0)
                return false;

            if (instance.LobbyId < 0 || other.LobbyId < 0)
                return false;

            return instance.OnlineId.Equals(other.OnlineId) && instance.LobbyId.Equals(other.LobbyId);
        }
    }

    extension(
        Channel? instance
    ) {
        public bool MatchesOnlineID(Channel? other) => instance.matchesOnlineID(other);

        private bool matchesOnlineID(
            Channel? other
        ) {
            if (instance is null || other is null)
                return false;

            if (string.IsNullOrEmpty(instance.OnlineId) || string.IsNullOrEmpty(other.OnlineId))
                return false;

            return instance.OnlineId.Equals(other.OnlineId);
        }
    }
}