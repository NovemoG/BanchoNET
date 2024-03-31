using System.Reflection;

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
	
	public static readonly string ExecPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!;
	public static readonly string BeatmapsPath = Path.Combine(ExecPath, "Beatmaps");
	public static readonly string ReplaysPath = Path.Combine(ExecPath, "Replays");
	public static readonly string AvatarsPath = Path.Combine(ExecPath, "Avatars");
	public static readonly string ScreenshotsPath = Path.Combine(ExecPath, "Screenshots");
	public static readonly string MedalIconsPath = Path.Combine(ExecPath, "MedalIcons");
	
	public static string GetBeatmapPath(int beatmapId) => Path.Combine(BeatmapsPath, $"{beatmapId}.osu");
	public static string GetReplayPath(long scoreId) => Path.Combine(ReplaysPath, $"{scoreId}.osr");
}