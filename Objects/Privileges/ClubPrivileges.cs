namespace BanchoNET.Objects.Privileges;

[Flags]
public enum ClubPrivileges
{
	MEMBER = 1,
	OFFICER = 2,
	OWNER = 3
}