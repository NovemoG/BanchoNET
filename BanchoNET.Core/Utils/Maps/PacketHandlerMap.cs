using System.Reflection;
using BanchoNET.Core.Packets;
using Novelog;

namespace BanchoNET.Core.Utils.Maps;

public static class PacketHandlerMap
{
	public static readonly Dictionary<ClientPacketId, MethodInfo> PacketMethodsMap = new();
	
	static PacketHandlerMap()
	{
		Logger.Shared.LogDebug("Mapping packet method handlers...", nameof(PacketHandlerMap));

		var handlerType = Assembly.GetExecutingAssembly().GetType("BanchoNET.Services.ClientPacketsHandler");
		if (handlerType is null)
			throw new MissingMemberException("ClientPacketsHandler type not found in executing assembly");
		
		foreach (var packetName in Enum.GetValues<ClientPacketId>())
		{
			var method = handlerType.GetMethod(packetName.ToString(), BindingFlags.NonPublic | BindingFlags.Instance);

			if (method != null)
				PacketMethodsMap.Add(packetName, method);
		}
	}
}