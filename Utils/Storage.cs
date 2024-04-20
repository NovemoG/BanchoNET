namespace BanchoNET.Utils;

public static class Storage
{
	static Storage()
	{
		if (!Directory.Exists(BeatmapsPath))
			Directory.CreateDirectory(BeatmapsPath);
		
		if (!Directory.Exists(ReplaysPath))
			Directory.CreateDirectory(ReplaysPath);
		
		if (!Directory.Exists(AvatarsPath))
			Directory.CreateDirectory(AvatarsPath);
		
		if (!Directory.Exists(ScreenshotsPath))
			Directory.CreateDirectory(ScreenshotsPath);
		
		if (!Directory.Exists(MedalIconsPath))
			Directory.CreateDirectory(MedalIconsPath);
	}
	
	private static readonly string BasePath = AppSettings.DataPath;
	public static readonly string BeatmapsPath = Path.Combine(BasePath, "Beatmaps");
	public static readonly string ReplaysPath = Path.Combine(BasePath, "Replays");
	public static readonly string AvatarsPath = Path.Combine(BasePath, "Avatars");
	public static readonly string ScreenshotsPath = Path.Combine(BasePath, "Screenshots");
	public static readonly string MedalIconsPath = Path.Combine(BasePath, "MedalIcons");
	
	public static string GetBeatmapPath(int beatmapId) => Path.Combine(BeatmapsPath, $"{beatmapId}.osu");
	public static string GetReplayPath(long scoreId) => Path.Combine(ReplaysPath, $"{scoreId}.osr");
}