namespace BanchoNET.Core.Utils;

public static class Storage
{
	static Storage()
	{
		CreateDirectoryIfNotExists(BeatmapsPath);
		CreateDirectoryIfNotExists(BeatmapsetsPath);
		CreateDirectoryIfNotExists(ReplaysPath);
		CreateDirectoryIfNotExists(AvatarsPath);
		CreateDirectoryIfNotExists(ProfileCoversPath);
		CreateDirectoryIfNotExists(ScreenshotsPath);
		CreateDirectoryIfNotExists(MedalIconsPath);
		CreateDirectoryIfNotExists(LogsPath);
		CreateDirectoryIfNotExists(TempPath);
		CreateDirectoryIfNotExists(LazerPath);
	}

	private const string BasePath = "/app/files";
	private static readonly string BeatmapsPath = Path.Combine(BasePath, "Beatmaps");
	private static readonly string BeatmapsetsPath = Path.Combine(BasePath, "Beatmapsets");
	private static readonly string ReplaysPath = Path.Combine(BasePath, "Replays");
	private static readonly string AvatarsPath = Path.Combine(BasePath, "Avatars");
	private static readonly string ProfileCoversPath = Path.Combine(BasePath, "ProfileCovers");
	private static readonly string ScreenshotsPath = Path.Combine(BasePath, "Screenshots");
	private static readonly string MedalIconsPath = Path.Combine(BasePath, "MedalIcons");
	private static readonly string LogsPath = Path.Combine(BasePath, "Logs");
	private static readonly string TempPath = Path.Combine(BasePath, "Temp");

	private const string BaseLazerPath = "/data/lazer";
	public static readonly string LazerPath = Path.Combine(BaseLazerPath, "Lazer");
	public static readonly string LazerTempPath = Path.Combine(BaseLazerPath, "Temp");
	
	public static string GetBeatmapPath(int beatmapId) => Path.Combine(BeatmapsPath, $"{beatmapId}.osu");
	public static string GetBeatmapsetPath(int beatmapsetId) => Path.Combine(BeatmapsetsPath, $"{beatmapsetId}.osz");
	public static string GetTempBeatmapsetPath() => Path.Combine(TempPath, $"{Guid.NewGuid()}.osz");
	public static string GetReplayPath(long scoreId) => Path.Combine(ReplaysPath, $"{scoreId}.osr");
	public static string GetMajorOsuVersionFilePath() => Path.Combine(BasePath, "major_osu_versions.txt");
	public static string GetLogFilePath(string filename) => Path.Combine(LogsPath, filename);

	public static string GetAvatarPath(int playerId, string extension) => Path.Combine(AvatarsPath, $"{playerId}.{extension}");
	public static string[] GetAvatarFiles(int playerId) => Directory.GetFiles(AvatarsPath, $"{playerId}.*");
	public static string GetProfileCoverPath(int playerId, string extension) => Path.Combine(ProfileCoversPath, $"{playerId}.{extension}");
	public static string[] GetProfileCoverFiles(int playerId) => Directory.GetFiles(ProfileCoversPath, $"{playerId}.*");
	public static string GetTempUploadPath(string extension) => Path.Combine(TempPath, $"{Guid.NewGuid()}.{extension}");

	public static void DeleteProfileImages(int playerId)
	{
		foreach (var file in GetAvatarFiles(playerId).Concat(GetProfileCoverFiles(playerId)))
		{
			try
			{
				File.Delete(file);
			}
			catch (IOException e)
			{
				Logger.Shared.LogError($"Failed to delete \"{file}\"", e, caller: nameof(Storage));
			}
		}
	}
	
	private static void CreateDirectoryIfNotExists(string path)
	{
		if (Directory.Exists(path)) return;
        
		Logger.Shared.LogInfo($"Creating directory: \"{path}\"", nameof(Storage));
		Directory.CreateDirectory(path);
	}
}