using BanchoNET.Objects.Players;

namespace BanchoNET.Services.ClientPacketsHandler;

public partial class ClientPacketsHandler
{
    private async Task FriendRemove(Player player, BinaryReader br)
    {
        var friendId = br.ReadInt32();
        var target = _session.GetPlayerById(friendId);

        if (target == null)
        {
            Console.WriteLine($"[FriendRemove] {player.Username} tried to remove an offline player ({friendId})");
            return;
        }
        
        if (target.IsBot)
            return;
        
        player.LastActivityTime = DateTime.Now;
        await players.RemoveFriend(player, target.Id);
    }
}