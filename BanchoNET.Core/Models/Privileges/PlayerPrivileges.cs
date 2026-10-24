using System.ComponentModel;

namespace BanchoNET.Core.Models.Privileges;

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
	Staff = 1 << 10,
	[Description("Can view and change secrets in the admin panel.")]
	Owner = 1 << 11,
	
	/// <summary>
	/// Every rank used by "this rank or higher" checks, lowest to highest.
	/// </summary>
	Ranks = Nominator | Moderator | Administrator | Developer | Staff | Owner,
}