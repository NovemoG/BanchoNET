namespace BanchoNET.Core.Models.Dtos;

public class ThreadFollows
{
    public int PlayerId { get; set; }
    public PlayerDto Player { get; set; } = null!;
    
    public int ThreadId { get; set; }
    public ThreadDto Thread { get; set; } = null!;
}