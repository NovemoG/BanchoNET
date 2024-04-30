using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BanchoNET.Models.Dtos;

public class MessagesDto
{
    [Key]
    public int Id { get; set; }
    public int SenderId { get; set; }
    public int ReceiverId { get; set; }
    public string Message { get; set; } = null!;
	
    [ForeignKey("SenderId")]
    public PlayerDto Sender { get; set; } = null!;
	
    [ForeignKey("ReceiverId")]
    public PlayerDto Receiver { get; set; } = null!;
}