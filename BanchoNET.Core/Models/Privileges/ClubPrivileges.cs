namespace BanchoNET.Core.Models.Privileges;

[Flags]
public enum ClubPrivileges : byte
{
	Member = 1,
	Officer = 2,
	Owner = 3
}