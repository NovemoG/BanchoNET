using BanchoNET.Core.Models.Dtos;

namespace BanchoNET.Core.Abstractions.Repositories;

public interface IMessagesRepository
{
    Task<MessageDto?> GetMessage(long id);
    Task<ChannelDto> GetOrAddPmChannel(int senderId, int receiverId, bool loadNav = false);
    Task<List<ChannelDto>> GetPmChannels(int playerId);
    Task<int?> GetTargetOfPmChannel(long channelId, int senderId);
    Task<ChannelDto?> GetPmChannel(int senderId, int receiverId);
    Task<ChannelDto?> GetPmChannel(long id);

    Task<MessageDto> AddMessage(
        string message,
        int senderId,
        long channelId,
        bool isAction = false,
        bool loadNav = false
    );
    
    Task<List<MessageDto>> GetChannelMessages(long channelId);
    Task<long?> GetLastChannelMessageId(long channelId);
    Task<long?> GetLastReadMessageId(long channelId, int userId);
    Task<List<MessageDto>> GetUnreadMessages(int playerId);
    Task MarkMessagesAsRead(long channelId, int userId, long messageId);
    Task DeleteMessage(long id);
    Task DeletePlayerSentMessages(int playerId);
}