namespace BanchoNET.Objects.Beatmaps;

public class BeatmapSet
{
	public int Id { get; set; }
	public List<Beatmap> Beatmaps { get; set; } = [];
	public DateTime LastApiCheck { get; set; }
}