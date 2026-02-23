using System.Reflection;
using BanchoNET.Packets;
using BanchoNET.Services.ClientPacketsHandler;

namespace BanchoNET.Utils.Maps;

public static class PacketHandlerMap
{
	public static readonly Dictionary<ClientPacketId, MethodInfo> PacketMethodsMap = new();
	
	static PacketHandlerMap()
	{
		Logger.Shared.LogDebug("Mapping packet method handlers...", nameof(PacketHandlerMap));
		
		var handlerType = typeof(ClientPacketsHandler);
		foreach (var packetName in Enum.GetValues<ClientPacketId>())
		{
			var method = handlerType.GetMethod(packetName.ToString(), BindingFlags.NonPublic | BindingFlags.Instance);

			if (method != null)
				PacketMethodsMap.Add(packetName, method);
		}
	}
}