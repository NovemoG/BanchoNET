using System.ComponentModel;
using BanchoNET.Objects.Players;
using BanchoNET.Packets;
using BanchoNET.Utils;

namespace BanchoNET.Services;

public partial class BanchoHandler
{
	public async Task ReadPackets(Stream stream, Player player)
	{
		Console.WriteLine();
		
		using var ms = new MemoryStream();
		await stream.CopyToAsync(ms);
		ms.Position = 0;
		
		using var br = new BinaryReader(ms);
		
		do
		{
			var packetId = (ClientPacketId)br.ReadInt16();
			br.ReadByte();
			var packetSize = br.ReadInt32();
			
			if (packetId == ClientPacketId.Ping) continue;

			var dataBuffer = new byte[packetSize];
			var bytesRead = br.Read(dataBuffer, 0, packetSize);
			
			Console.WriteLine($"[ClientPackets] Packet: {packetId}, Length: {dataBuffer.Length}, Read: {bytesRead}, To read: {packetSize}");
			
			if (bytesRead != packetSize)
				throw new Exception("Packet size mismatch");

			ms.Position -= bytesRead;
			
			PacketHandlerMap.PacketMethodsMap.TryGetValue(packetId, out var method);
			if (method != null)
				await (Task)method.Invoke(this, [player, br])!;
			else
				throw new InvalidEnumArgumentException($"[ClientPackets] Handler of packet {packetId} is not (yet) implemented");
			
		} while (br.BaseStream.Position < br.BaseStream.Length);
	}
}