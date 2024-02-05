using BanchoNET.Objects.Players;

namespace BanchoNET.Packets;

public class ClientPackets : Packet
{
	public async Task<ClientPackets> CopyStream(Stream stream)
	{
		await stream.CopyToAsync(DataBuffer);
		DataBuffer.Position = 0;
		return this;
	}

	public void ReadPackets(Player player)
	{
		using var br = new BinaryReader(DataBuffer);
		
		while (DataBuffer.Position < DataBuffer.Length)
		{
			var packetId = (ClientPacketId)br.ReadInt16();
			br.ReadByte();
			var packetSize = br.ReadInt32();
			
			var dataBuffer = new byte[packetSize];
			br.Read(dataBuffer, 0, packetSize);
			
			Console.WriteLine($"Packet: {packetId}, Length: {dataBuffer.Length}");
			//player.Enqueue(packetId, dataBuffer);
		}
	}
}