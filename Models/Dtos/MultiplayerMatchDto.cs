using Microsoft.EntityFrameworkCore;

namespace BanchoNET.Models.Dtos;

[PrimaryKey(nameof(Id))]
public class MultiplayerMatchDto
{
	public int Id { get; set; }
}