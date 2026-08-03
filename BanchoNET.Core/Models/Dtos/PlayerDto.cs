using System.ComponentModel.DataAnnotations.Schema;
using BanchoNET.Core.Models.Players;

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
	public string? Title { get; set; }
	
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
	public GameMode PreferredMode { get; set; }
	public Playstyle PlayStyle { get; set; }

	public string? AwayMessage { get; set; }
	public string? UserPageContent { get; set; }
	public string? ApiKey { get; set; }

	public string? UserFrom { get; set; }
	public string? UserInterests { get; set; }
	public string? UserOcc { get; set; }
	public string? UserTwitter { get; set; }
	public string? UserDiscord { get; set; }
	public string? UserWebsite { get; set; }
	public string? UserSig { get; set; }
	public bool UserNotify { get; set; } = true;

	public string? AvatarExtension { get; set; }
	public DateTime? AvatarUpdatedAt { get; set; }
	public int? CoverPresetId { get; set; }
	public string? CoverFile { get; set; }
	public DateTime? CoverUpdatedAt { get; set; }

	public ICollection<StatsDto> Stats { get; set; } = null!;
	public ICollection<LoginDto> LoginsData { get; set; } = null!;
	public ICollection<ClientHashesDto> ClientHashes { get; set; } = null!;
	public ICollection<MessageDto> SentMessages { get; set; } = [];
	public ICollection<RelationshipDto> Relationships { get; set; } = [];
	public ICollection<RelationshipDto> IncomingRelationships { get; set; } = [];
	public ICollection<ChannelPlayer> PlayerChannels { get; } = [];
	
	public ICollection<ScoreDto> Scores { get; set; } = [];
	public ICollection<ReplayWatches> WatchedReplays { get; set; } = [];

	public ICollection<BeatmapPlays> PlayedBeatmaps { get; set; } = [];
	public ICollection<BeatmapsetFavorite> FavoriteBeatmapsets { get; set; } = [];
	public ICollection<BeatmapsetDto> Beatmapsets { get; set; } = [];
	
	public ICollection<CommentDto> Comments { get; set; } = [];
	public ICollection<ThreadFollows> ThreadFollows { get; set; } = [];
	public ICollection<CommentVote> CommentVotes { get; set; } = [];
	
	public PlayerProfileCustomizationDto? ProfileCustomization { get; set; }
	public ICollection<PlayerNotificationOptionDto> NotificationOptions { get; set; } = [];

	public long SkillsId { get; set; }
	public SkillsDto Skills { get; set; } = new();
	
	[NotMapped]
	public bool IsSupporter => RemainingSupporter > DateTime.UtcNow;
}