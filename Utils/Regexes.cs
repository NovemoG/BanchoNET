using System.Text.RegularExpressions;

namespace BanchoNET.Utils;

public static class Regexes
{
	public static void InitNowPlayingRegex(string domain)
	{
		NowPlaying = new Regex(
			@$"^\x01ACTION is (?:playing|editing|watching|listening to) \[https://osu\.(?:{domain.Replace(".", "\\.")}|ppy\.sh)/beatmapsets/(?<sid>\d{{1,10}})#/?(?:osu|taiko|fruits|mania)?/(?<bid>\d{{1,10}})/? .+\](?<mods>(?: (?:-|\+|~|\|)\w+(?:~|\|)?)+)?\x01$",
			RegexOptions.Compiled);
	}
	
	static Regexes()
	{
		Username = new Regex(
			@"^[\w \[\]-]{{2,15}}$",
			RegexOptions.Compiled);
		
		Email = new Regex(
			@"^[^@\s]{1,100}@[^@\s\.]{1,45}(?:\.[^@\.\s]{2,15})+$",
			RegexOptions.Compiled);
		
		SingleCharacterType = new Regex(
			@"(\s+.*_+.*|_+.*\s+.*)",
			RegexOptions.Compiled);
		
		Whitespace = new Regex(
			@"\s+",
			RegexOptions.Compiled);
		
		OsuVersion = new Regex(
			@"^b(?<date>\d{8})(?:\.(?<revision>\d))?(?<stream>beta|cuttingedge|tourney|dev)?$",
			RegexOptions.Compiled);
	}

	public static readonly Regex Username;
	public static readonly Regex Email;
	public static readonly Regex SingleCharacterType;
	public static readonly Regex Whitespace;
	public static readonly Regex OsuVersion;
	public static Regex NowPlaying { get; private set; } = null!;
}