namespace BanchoNET.Objects.Privileges;

[Flags]
public enum ClientPrivileges
{
	PLAYER = 1 << 0,
	MODERATOR = 1 << 1,
	SUPPORTER = 1 << 2,
	OWNER = 1 << 3,
	DEVELOPER = 1 << 4,
	TOURNAMENT = 1 << 5
}