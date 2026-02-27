using BanchoNET.Core.Models.Channels;
using BanchoNET.Core.Models.Users;

namespace BanchoNET.Core.Abstractions.Bancho.Services;

public interface IChannelService
{
    Channel LobbyChannel { get; }
    IEnumerable<Channel> Channels { get; }

    bool InsertChannel(Channel channel);
    bool RemoveChannel(Channel channel);
    bool RemoveChannel(string id);
    Channel? GetChannel(string name);

    bool JoinPlayer(
        Channel channel,
        User player
    );
    bool LeavePlayer(
        Channel channel,
        User player,
        bool kick = true
    );
    bool LeavePlayer(
        string id,
        User player,
        bool kick = true
    );

    public void SendMessageTo(
        Channel channel,
        Message message,
        bool toSelf = false
    );
    public void SendBotMessageTo(
        Channel channel,
        string message,
        User from
    );
}