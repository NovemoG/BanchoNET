using BanchoNET.Abstractions.Repositories;
using BanchoNET.Models;
using BanchoNET.Models.Dtos;
using Microsoft.EntityFrameworkCore;

namespace BanchoNET.Services.Repositories;

public class MessagesRepository(BanchoDbContext dbContext) : IMessagesRepository
{
    public async Task<MessageDto?> GetMessage(long id)
    {
        return await dbContext.Messages.FirstOrDefaultAsync(m => m.Id == id);
    }
    
    public async Task AddMessage(string message, int senderId, int receiverId, bool read = false)
    {
        await dbContext.Messages.AddAsync(new MessageDto
        {
            SenderId = senderId,
            ReceiverId = receiverId,
            Message = message,
            Read = read,
            SentAt = DateTime.UtcNow
        });
        await dbContext.SaveChangesAsync();
    }
    
    public async Task<List<MessageDto>> GetUnreadMessages(int playerId)
    {
        return await dbContext.Messages
            .Include(m => m.Sender)
            .Include(m => m.Receiver)
            .Where(m => m.ReceiverId == playerId && !m.Read)
            .ToListAsync();
    }
    
    public async Task MarkMessageAsRead(long id)
    {
        await dbContext.Messages.Where(m => m.Id == id)
            .ExecuteUpdateAsync(p => p.SetProperty(m => m.Read, true));
    }
    
    public async Task DeleteMessage(long id)
    {
        await dbContext.Messages.Where(m => m.Id == id).ExecuteDeleteAsync();
    }
    
    public async Task DeletePlayerReceivedMessages(int playerId)
    {
        await dbContext.Messages.Where(m => m.ReceiverId == playerId).ExecuteDeleteAsync();
    }
    
    public async Task DeletePlayerSentMessages(int playerId)
    {
        await dbContext.Messages.Where(m => m.SenderId == playerId).ExecuteDeleteAsync();
    }
}