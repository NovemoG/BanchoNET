using BanchoNET.Models.Dtos;

namespace BanchoNET.Abstractions.Repositories;

public interface IMessagesRepository
{
    Task<MessageDto?> GetMessage(long id);
    Task AddMessage(string message, int senderId, int receiverId, bool read = false);
    Task<List<MessageDto>> GetUnreadMessages(int playerId);
    Task MarkMessageAsRead(long id);
    Task DeleteMessage(long id);
    Task DeletePlayerReceivedMessages(int playerId);
    Task DeletePlayerSentMessages(int playerId);
}