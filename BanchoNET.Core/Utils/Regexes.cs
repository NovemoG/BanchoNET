using System.Text.RegularExpressions;

namespace BanchoNET.Core.Utils;

public static partial class Regexes
{
	static Regexes()
	{
		Username = UsernameRegex();
		Email = EmailRegex();
		OsuVersion = OsuVersionRegex();
		NumSeparator = NumSeparatorRegex();
		
		NowPlaying = new Regex(
			@$"^\x01ACTION is (?:playing|editing|watching|listening to) \[https://osu\.(?:{AppSettings.Domain.Replace(".", "\\.")}|ppy\.sh)/beatmapsets/(?<sid>\d{{1,10}})#/?(?:osu|taiko|fruits|mania)?/(?<bid>\d{{1,10}})/? .+\](?: <(?<mode_vn>Taiko|CatchTheBeat|osu!mania)>)?(?<mods>(?: (?:-|\+|~|\|)\w+(?:~|\|)?)+)?\x01$",
			RegexOptions.Compiled);
	}

	public static readonly Regex Username;
	public static readonly Regex Email;
	public static readonly Regex OsuVersion;
	public static readonly Regex NumSeparator;
	public static readonly Regex NowPlaying;

    [GeneratedRegex(@"^[\w \[\]-]{2,15}$", RegexOptions.Compiled)]
    private static partial Regex UsernameRegex();
    
    [GeneratedRegex(@"^[^@\s]{1,100}@[^@\s\.]{1,45}(?:\.[^@\.\s]{2,15})+$", RegexOptions.Compiled)]
    private static partial Regex EmailRegex();
    
    [GeneratedRegex(@"^b(?<date>\d{8})(?:\.(?<revision>\d))?(?<stream>beta|cuttingedge|tourney|dev)?$", RegexOptions.Compiled)]
    private static partial Regex OsuVersionRegex();
    
    [GeneratedRegex(".{3}", RegexOptions.Compiled | RegexOptions.RightToLeft)]
    private static partial Regex NumSeparatorRegex();
}