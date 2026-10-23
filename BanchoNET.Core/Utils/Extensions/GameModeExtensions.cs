using BanchoNET.Core.Models;

namespace BanchoNET.Core.Utils.Extensions;

public static class GameModeExtensions
{
	public static GameMode FromRegexMatch(this string mode)
	{
		return mode switch
		{
			"Taiko" => GameMode.Taiko,
			"CatchTheBeat" => GameMode.Catch,
			"osu!mania" => GameMode.Mania,
			_ => GameMode.Standard
		};
	}
}