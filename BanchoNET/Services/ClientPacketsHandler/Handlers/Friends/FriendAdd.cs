using BanchoNET.Core.Models.Users;

namespace BanchoNET.Services.ClientPacketsHandler;

public partial class ClientPacketsHandler
{
    private async Task FriendAdd(User player, BinaryReader br)
    {
        var friendId = br.ReadInt32();
        var target = playerService.GetPlayer(friendId);

        if (target == null)
        {
            Console.WriteLine($"[FriendAdd] {player.Username} tried to add a non-existent player ({friendId})");
            return;
        }
        
        if (target.IsBot)
            return;

        player.LastActivityTime = DateTime.UtcNow;
        player.Blocked.Remove(target.Id);
        await players.AddFriend(player, target.Id);
    }
}