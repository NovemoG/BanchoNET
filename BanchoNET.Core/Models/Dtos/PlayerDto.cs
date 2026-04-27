using System.ComponentModel.DataAnnotations.Schema;

namespace BanchoNET.Core.Models.Dtos;

public class PlayerDto
{
	public int Id { get; set; }
	
	public string Username { get; set; } = null!;
	public string SafeName { get; set; } = null!;
	public string LoginName { get; set; } = null!;
	public string Email { get; set; } = null!;
	public string PasswordHash { get; set; } = null!;
	
	public string Country { get; set; } = null!;
	public int Privileges { get; set; }
	public bool PmFriendsOnly { get; set; }
	public bool HideOnlineActivity { get; set; }
	
	public bool Inactive { get; set; }
	public bool Deleted { get; set; } //TODO
	
	public DateTime RemainingSilence { get; set; }
	public DateTime RemainingSupporter { get; set; }
	public bool HasSupported { get; set; } //TODO
	public byte SupporterLevel { get; set; } //TODO
	
	public DateTime CreationTime { get; set; }
	public DateTime LastLoginTime { get; set; }
	public DateTime LastActivityTime { get; set; }
	
	public int TopPlaysCount { get; set; }
	public byte PreferredMode { get; set; }
	public byte PlayStyle { get; set; }
	
	public string? AwayMessage { get; set; }
	
	public string? UserPageContent { get; set; }
	
	public string? ApiKey { get; set; }

	public ICollection<StatsDto> Stats { get; set; } = null!;
	public ICollection<ScoreDto> Scores { get; set; } = null!;
	public ICollection<LoginDto> LoginsData { get; set; } = null!;
	public ICollection<ClientHashesDto> ClientHashes { get; set; } = null!;
	public ICollection<MessageDto> SentMessages { get; set; } = null!;
	public ICollection<RelationshipDto> Relationships { get; set; } = null!;
	public ICollection<RelationshipDto> IncomingRelationships { get; set; } = null!;
	public ICollection<BeatmapDto> Beatmaps { get; set; } = null!;
	public ICollection<BeatmapsetDto> Beatmapsets { get; set; } = null!;
	public ICollection<ChannelPlayer> PlayerChannels { get; } = [];
	
	[NotMapped]
	public bool IsSupporter => RemainingSupporter > DateTime.UtcNow;
}