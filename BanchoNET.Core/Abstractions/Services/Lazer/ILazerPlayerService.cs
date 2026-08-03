using BanchoNET.Core.Models.Api.Player;
using BanchoNET.Core.Models.Players;

namespace BanchoNET.Core.Abstractions.Services.Lazer;

public interface ILazerPlayerService
{
    Task<bool> AddPlayer(
        ApiPlayer player
    );

    Task AssignFriends(
        int userId,
        int[] friends
    );

    Task<bool> RemovePlayer(
        int userId
    );
    
    Task RefreshPlayer(
        int userId,
        ApiPlayer fresh
    );

    Task<LazerPlayer?> GetPlayer(
        int userId
    );

    Task<bool> IsOnline(
        int userId
    );

    /// <summary>
    /// Batched presence lookup. Anything iterating a friends list or a lookup result must
    /// use this rather than calling <see cref="IsOnline"/> per id.
    /// </summary>
    Task<HashSet<int>> FilterOnline(
        IReadOnlyCollection<int> userIds
    );

    /// <summary>
    /// Explicit mutator because the player is no longer a shared in process object:
    /// assigning to whatever <see cref="GetPlayer"/> returns would only change a local copy.
    /// </summary>
    Task SetLastPlayed(
        int userId,
        int beatmapId,
        int exitIndex
    );

    Task<HashSet<int>> FilterHidden(
        IReadOnlyCollection<int> userIds
    );

    Task SetPresenceHidden(
        int userId,
        bool hidden
    );

    Task<bool> WriteActivity(
        int userId,
        TimeSpan interval
    );
}