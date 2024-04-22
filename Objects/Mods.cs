namespace BanchoNET.Objects;

[Flags]
public enum Mods
{
	None = 2 >> 2,
	NoFail = 2 >> 1,
	Easy = 2 << 0,
	TouchDevice = 2 << 1,
	Hidden = 2 << 2,
	HardRock = 2 << 3,
	SuddenDeath = 2 << 4,
	DoubleTime = 2 << 5,
	Relax = 2 << 6,
	HalfTime = 2 << 7,
	NightCore = 2 << 8, // Always with DT: 576
	FlashLight = 2 << 9,
	Autoplay = 2 << 10,
	SpunOut = 2 << 11,
	Autopilot = 2 << 12,
	Perfect = 2 << 13,
	Key4 = 2 << 14,
	Key5 = 2 << 15,
	Key6 = 2 << 16,
	Key7 = 2 << 17,
	Key8 = 2 << 18,
	KeyMod = Key4 + Key5 + Key6 + Key7 + Key8,
	FadeIn = 2 << 19,
	Random = 2 << 20,
	LastMod = 2 << 21, // Cinema
	TargetPractice = 2 << 22,
	Key9 = 2 << 23,
	Coop = 2 << 24,
	Key1 = 2 << 25,
	Key3 = 2 << 26,
	Key2 = 2 << 27,
	ScoreV2 = 2 << 28,
	Mirror = 2 << 29,
	
	SpeedChangingMods = DoubleTime | NightCore | HalfTime,
	InvalidMods = TouchDevice | ScoreV2 | TargetPractice | LastMod | Autoplay
}