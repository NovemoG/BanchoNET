using BanchoNET.Models;
using BanchoNET.Models.Dtos;
using Microsoft.EntityFrameworkCore;

namespace BanchoNET.Services.Repositories;

public class MessagesRepository(BanchoDbContext dbContext)
{
    public async Task<MessageDto?> GetMessageById(int id)
    {
        return await dbContext.Messages.FirstOrDefaultAsync(m => m.Id == id);
    }
    public async Task AddMessage(int senderId, int receiverId, string message, bool read = false)
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
}