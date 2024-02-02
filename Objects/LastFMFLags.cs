namespace BanchoNET.Objects;

[Flags]
public enum LastFMFLags
{
	RUN_WITH_LD_FLAG = 1 << 14,
	CONSOLE_OPEN = 1 << 15,
	EXTRA_THREADS = 1 << 16,
	HQ_ASSEMBLY = 1 << 17,
	HQ_FILE = 1 << 18,
	REGISTRY_EDITS = 1 << 19,
	SDL2_LIBRARY = 1 << 20,
	OPENSSL_LIBRARY = 1 << 21,
	AQN_MENU_SAMPLE = 1 << 22
}