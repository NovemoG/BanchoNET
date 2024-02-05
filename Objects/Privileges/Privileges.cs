namespace BanchoNET.Objects.Privileges;

[Flags]
public enum Privileges
{
	// 0 means that player is restricted
	UNRESTRICTED = 1 << 0,
	VERIFIED = 1 << 1,
	SUPPORTER = 1 << 2,
	ALUMNI = 1 << 3,
	TOURNEY_MANAGER = 1 << 4,
	NOMINATOR = 1 << 5,
	MODERATOR = 1 << 6,
	ADMINISTRATOR = 1 << 7,
	DEVELOPER = 1 << 8,
	
	STAFF = MODERATOR | ADMINISTRATOR | DEVELOPER,
}