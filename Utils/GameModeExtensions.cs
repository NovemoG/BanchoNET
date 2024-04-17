using BanchoNET.Objects;

namespace BanchoNET.Utils;

public static class GameModeExtensions
{
	public static GameMode AsVanilla(this GameMode gameMode) => (GameMode)((int)gameMode % 4);

	public static GameMode FromMods(this GameMode gameMode, Mods mods)
	{
		if ((mods & Mods.Relax) == Mods.Relax)
			gameMode += 4;
		else if ((mods & Mods.Autopilot) == Mods.Autopilot)
			gameMode += 8;

		return gameMode;
	}
}