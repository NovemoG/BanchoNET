namespace BanchoNET.Objects.Multiplayer;

[Flags]
public enum SlotStatus
{
	Open = 1 << 0,
	Locked = 1 << 1,
	NotReady = 1 << 2,
	Ready = 1 << 3,
	NoMap = 1 << 4,
	Playing = 1 << 5,
	Complete = 1 << 6,
	Quit = 1 << 7
}