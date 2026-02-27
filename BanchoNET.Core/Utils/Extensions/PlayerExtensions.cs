using BanchoNET.Core.Models.Players;
using BanchoNET.Core.Models.Privileges;

namespace BanchoNET.Core.Utils.Extensions;

public static class PlayerExtensions
{
	private static readonly Dictionary<PlayerPrivileges, ClientPrivileges> ClientPrivilegesMap = new()
	{
		{ PlayerPrivileges.Unrestricted, ClientPrivileges.Player },
		{ PlayerPrivileges.Supporter, ClientPrivileges.Supporter },
		{ PlayerPrivileges.Moderator, ClientPrivileges.Moderator },
		{ PlayerPrivileges.Administrator, ClientPrivileges.Developer },
		{ PlayerPrivileges.Developer, ClientPrivileges.Owner },
	};
	
	extension(
		User player
	) {
		public ClientPrivileges ToBanchoPrivileges()
		{
			var retPriv = 0;
			var privs = (int)player.Privileges;
		
			foreach (var priv in Enum.GetValues<PlayerPrivileges>())
			{
				if ((privs & (int)priv) == (int)priv && ClientPrivilegesMap.TryGetValue(priv, out var value)) 
					retPriv |= (int)value;
			}

			return (ClientPrivileges)retPriv;
		}

		public bool CanUseCommand(
			PlayerPrivileges requiredPrivileges
		) {
			return player.Privileges.HasAnyPrivilege(requiredPrivileges) &&
			       player.Privileges.CompareHighestPrivileges(requiredPrivileges);
		}

		public bool BlockedByPlayer(
			int targetId
		) {
			return player.Blocked.Contains(targetId);
		}
	}
}