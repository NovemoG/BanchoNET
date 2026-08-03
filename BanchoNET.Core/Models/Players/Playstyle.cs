namespace BanchoNET.Core.Models.Players;

[Flags]
public enum Playstyle : byte
{
	None = 0,
	Mouse = 1,
	Keyboard = 2,
	Tablet = 4,
	Touch = 8
}