namespace BanchoNET.Objects.Privileges;

[Flags]
public enum Privileges
{
	// 0 means that player is restricted
	Unrestricted = 1 << 0,
	Verified = 1 << 1,
	Supporter = 1 << 2,
	Alumni = 1 << 3,
	TourneyManager = 1 << 4,
	Nominator = 1 << 5,
	Moderator = 1 << 6,
	Administrator = 1 << 7,
	Developer = 1 << 8,
	
	Staff = Moderator | Administrator | Developer,
}