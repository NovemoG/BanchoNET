using BanchoNET.Core.Models.Dtos;

namespace BanchoNET.Core.Abstractions.Repositories;

public interface IMessagesRepository
{
    Task<MessageDto?> GetMessage(long id);
    Task<ChannelDto> GetOrAddPmChannel(int senderId, int receiverId);
    Task<List<ChannelDto>> GetPmChannels(int playerId);
    Task<int?> GetTargetOfPmChannel(long channelId, int senderId);
    Task<ChannelDto?> GetPmChannel(int senderId, int receiverId);
    Task<ChannelDto?> GetPmChannel(long id);

    Task<MessageDto> AddMessage(
        string message,
        int senderId,
        long channelId,
        int? receiverId = null,
        bool read = false,
        bool isAction = false,
        bool loadNav = false
    );
    
    Task<List<MessageDto>> GetChannelMessages(long channelId);
    Task<List<MessageDto>> GetUnreadMessages(int playerId);
    Task MarkMessageAsRead(long id);
    Task DeleteMessage(long id);
    Task DeletePlayerReceivedMessages(int playerId);
    Task DeletePlayerSentMessages(int playerId);
}