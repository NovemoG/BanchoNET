using System.ComponentModel;

namespace BanchoNET.Objects.Privileges;

[Flags]
public enum PlayerPrivileges : short
{
	[Description("if player does not have 1 it means that he is restricted")]
	Unrestricted = 1 << 0,
	Verified = 1 << 1,
	Supporter = 1 << 2,
	TourneyManager = 1 << 4,
	[Description("Player with this privilege can modify statuses of beatmaps")]
	Nominator = 1 << 5,
	[Description("Player with this privilege can submit scores with a command")]
	Submitter = 1 << 6,
	Moderator = 1 << 7,
	Administrator = 1 << 8,
	Developer = 1 << 9,
	
	Staff = Moderator | Administrator | Developer,
}