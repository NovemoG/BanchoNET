using BanchoNET.Core.Models.Players;

namespace BanchoNET.Services.ClientPacketsHandler;

public partial class ClientPacketsHandler
{
    private async Task FriendRemove(User player, BinaryReader br)
    {
        var friendId = br.ReadInt32();
        var target = playerService.GetPlayer(friendId);

        if (target == null)
        {
            Console.WriteLine($"[FriendRemove] {player.Username} tried to remove an offline player ({friendId})");
            return;
        }
        
        if (target.IsBot)
            return;
        
        player.LastActivityTime = DateTime.UtcNow;
        await players.RemoveFriend(player, target.Id);
    }
}