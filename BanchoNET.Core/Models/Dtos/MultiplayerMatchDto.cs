namespace BanchoNET.Core.Models.Dtos;

public class MultiplayerMatchDto
{
	public long Id { get; set; }

	public required string Name { get; set; }

	/// <summary>
	/// Cleared rather than cascaded when the host is deleted, so the rest of the match survives.
	/// </summary>
	public int? HostId { get; set; }

	public byte Mode { get; set; }

	public DateTimeOffset StartTime { get; set; }
	public DateTimeOffset? EndTime { get; set; }

	public ICollection<MultiplayerGameDto> Games { get; set; } = [];
	public ICollection<MultiplayerEventDto> Events { get; set; } = [];
	public ICollection<MultiplayerParticipantDto> Participants { get; set; } = [];
}