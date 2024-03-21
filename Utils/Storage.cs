using System.Reflection;

namespace BanchoNET.Utils;

public static class Storage
{
	public static readonly string ExecPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!;
	public static readonly string BeatmapsPath = Path.Combine(ExecPath, "Beatmaps");
}