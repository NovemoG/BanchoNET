using System.Collections.Concurrent;
using BanchoNET.Core.Abstractions.Services.Lazer;
using BanchoNET.Core.Models.Api.Player;
using BanchoNET.Core.Models.Players;

namespace BanchoNET.Handlers.Lazer.Services;

public sealed class LazerPlayerService : ILazerPlayerService
{
    private static readonly ConcurrentDictionary<int, LazerPlayer> Players = new();

    public bool AddPlayer(
        ApiPlayer player
    ) {
        if (Players.TryAdd(player.Id, new LazerPlayer { Player = player }))
        {
            player.IsOnline = true;
            return true;
        }

        return false;
    }

    public void AssignFriends(
        int userId,
        int[] friends
    ) {
        if (Players.TryGetValue(userId, out var player))
            player.Friends = friends;
    }

    public bool RemovePlayer(
        int userId
    ) {
        return Players.TryRemove(userId, out _);
    }

    public LazerPlayer? GetPlayer(
        int userId
    ) {
        return Players.TryGetValue(userId, out var player) ? player : null;
    }

    public bool IsOnline(
        int userId
    ) {
        return Players.ContainsKey(userId);
    }
}