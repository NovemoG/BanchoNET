using Novelog;

namespace BanchoNET.Core.Utils;

public static class Storage
{
	static Storage()
	{
		CreateDirectoryIfNotExists(BeatmapsPath);
		CreateDirectoryIfNotExists(ReplaysPath);
		CreateDirectoryIfNotExists(AvatarsPath);
		CreateDirectoryIfNotExists(ScreenshotsPath);
		CreateDirectoryIfNotExists(MedalIconsPath);
		CreateDirectoryIfNotExists(LogsPath);
	}

	private const string BasePath = "/app/files";
	private static readonly string BeatmapsPath = Path.Combine(BasePath, "Beatmaps");
	private static readonly string ReplaysPath = Path.Combine(BasePath, "Replays");
	private static readonly string AvatarsPath = Path.Combine(BasePath, "Avatars");
	private static readonly string ScreenshotsPath = Path.Combine(BasePath, "Screenshots");
	private static readonly string MedalIconsPath = Path.Combine(BasePath, "MedalIcons");
	private static readonly string LogsPath = Path.Combine(BasePath, "Logs");
	
	public static string GetBeatmapPath(int beatmapId) => Path.Combine(BeatmapsPath, $"{beatmapId}.osu");
	public static string GetReplayPath(long scoreId) => Path.Combine(ReplaysPath, $"{scoreId}.osr");
	public static string GetMajorOsuVersionFilePath() => Path.Combine(BasePath, "major_osu_versions.txt");
	public static string GetLogFilePath(string filename) => Path.Combine(LogsPath, filename);
	
	private static void CreateDirectoryIfNotExists(string path)
	{
		if (Directory.Exists(path)) return;
        
		Logger.Shared.LogInfo($"Creating directory: \"{path}\"", nameof(Storage));
		Directory.CreateDirectory(path);
	}
}