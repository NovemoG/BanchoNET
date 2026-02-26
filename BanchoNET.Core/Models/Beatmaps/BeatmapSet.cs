using BanchoNET.Core.Models.Dtos;
using BanchoNET.Core.Utils.Extensions;

namespace BanchoNET.Core.Models.Beatmaps;

public class BeatmapSet : IBeatmapSet,
	IEquatable<BeatmapSet>
{
	public int OnlineId => Id;
	public int Id { get; set; }
	public List<Beatmap> Beatmaps { get; } = [];
	
	/// <summary>
	/// Only if map is not ranked/approved on official server
	/// </summary>
	public DateTime LastApiCheck { get; set; }
	public bool HasRankedMapsOfficially { get; set; }

	#region Constructors

	public BeatmapSet(List<ApiBeatmap> apiBeatmaps)
	{
		Id = apiBeatmaps[0].BeatmapsetId;
		LastApiCheck = DateTime.Now;

		foreach (var beatmap in apiBeatmaps)
		{
			var map = new Beatmap(beatmap);
			
			Beatmaps.Add(map);
			map.Set = this;
		}
		
		HasRankedMapsOfficially = Beatmaps[0].IsRankedOfficially;
	}
	
	public BeatmapSet(List<OsuApiBeatmap> apiBeatmaps)
	{
		LastApiCheck = DateTime.Now;
		
		foreach (var beatmap in apiBeatmaps)
		{
			var map = new Beatmap(beatmap);
			
			Beatmaps.Add(map);
			map.Set = this;
		}

		HasRankedMapsOfficially = Beatmaps[0].IsRankedOfficially;
		Id = Beatmaps[0].SetId;
	}

	public BeatmapSet(List<BeatmapDto> dbBeatmaps)
	{
		Id = dbBeatmaps[0].SetId;
		
		foreach (var beatmap in dbBeatmaps)
		{
			var map = new Beatmap(beatmap);
			
			Beatmaps.Add(map);
			map.Set = this;
		}
	}

	#endregion

	#region IEquatable

	public bool Equals(BeatmapSet? other) => this.MatchesOnlineId(other);
    
	public override bool Equals(
		object? obj
	) {
		return ReferenceEquals(this, obj) || obj is BeatmapSet other && Equals(other);
	}
	public override int GetHashCode() => Id.GetHashCode();

	#endregion
}