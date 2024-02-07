using System.Text.RegularExpressions;

namespace BanchoNET.Utils;

public static partial class Regexes
{
	[GeneratedRegex(@"^b(?<date>\d{8})(?:\.(?<revision>\d))?(?<stream>beta|cuttingedge|tourney|dev)?$", RegexOptions.Compiled)]
	public static partial Regex OsuVersion();

	[GeneratedRegex(@"^[\w \[\]-]{2,15}$", RegexOptions.Compiled)]
	public static partial Regex Username();

	[GeneratedRegex(@"^[^@\s]{1,100}@[^@\s\.]{1,45}(?:\.[^@\.\s]{2,15})+$", RegexOptions.Compiled)]
	public static partial Regex Email();
	
	[GeneratedRegex(@"(\s+.*_+.*|_+.*\s+.*)", RegexOptions.Compiled)]
	public static partial Regex SingleCharacterType();
	
	//TODO load domain from config (?)
	[GeneratedRegex(@"^\x01ACTION is (?:playing|editing|watching|listening to) \[https://osu\.(?:novemo\.net|ppy\.sh)/beatmapsets/(P<sid>\d{1,10})#/?(?:osu|taiko|fruits|mania)?/(P<bid>\d{1,10})/? .+\](P<mods>(?: (?:-|\+|~|\|)\w+(?:~|\|)?)+)?\x01$", RegexOptions.Compiled)]
	public static partial Regex NowPlaying();
}