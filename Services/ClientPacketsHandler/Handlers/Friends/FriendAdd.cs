using BanchoNET.Objects.Players;

namespace BanchoNET.Services.ClientPacketsHandler;

public partial class ClientPacketsHandler
{
    private async Task FriendAdd(Player player, BinaryReader br)
    {
        var friendId = br.ReadInt32();
        var target = _session.GetPlayerById(friendId);

        if (target == null)
        {
            Console.WriteLine($"[FriendAdd] {player.Username} tried to add a non-existent player ({friendId})");
            return;
        }
        
        if (target.IsBot)
            return;

        player.LastActivityTime = DateTime.Now;
        player.Blocked.Remove(target.Id);
        await players.AddFriend(player, target.Id);
    }
}