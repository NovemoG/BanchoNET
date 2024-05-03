using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace BanchoNET.Models.Dtos;

[Index(nameof(SenderId))]
[Index(nameof(ReceiverId))]
[Index(nameof(Read))]
[PrimaryKey(nameof(Id))]
public class MessageDto
{
    [Key] public long Id { get; set; }
    public int SenderId { get; set; }
    public int ReceiverId { get; set; }
    public string Message { get; set; } = null!;
    public bool Read { get; set; }
    public DateTime SentAt { get; set; }
	
    [ForeignKey("SenderId")]
    public PlayerDto Sender { get; set; } = null!;
	
    [ForeignKey("ReceiverId")]
    public PlayerDto Receiver { get; set; } = null!;
}