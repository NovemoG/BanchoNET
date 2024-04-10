namespace BanchoNET.Objects.Players;

[Flags]
public enum ClientFlags
{
	Clean = 1 >> 1,
	SpeedHackDetected = 1 << 1,
	IncorrectModValue = 1 << 2,
	MultipleOsuClients = 1 << 3,
	ChecksumFailure = 1 << 4,
	FlashlightChecksumIncorrect = 1 << 5,
	OsuExecutableChecksum = 1 << 6,
	MissingProcessesInList = 1 << 7 ,
	FlashlightImageHack = 1 << 8,
	SpinnerHack = 1 << 9,
	TransparentWindow = 1 << 10,
	FastPress = 1 << 11,
	RawMouseDiscrepancy = 1 << 12,
	RawKeyboardDiscrepancy = 1 << 13
}

[Flags]
public enum LastFmfLags
{
	RunWithLdFlag = 1 << 14,
	ConsoleOpen = 1 << 15,
	ExtraThreads = 1 << 16,
	HqAssembly = 1 << 17,
	HqFile = 1 << 18,
	RegistryEdits = 1 << 19,
	Sdl2Library = 1 << 20,
	OpensslLibrary = 1 << 21,
	AqnMenuSample = 1 << 22
}