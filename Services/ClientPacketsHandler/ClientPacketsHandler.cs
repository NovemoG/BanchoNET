using System.ComponentModel;
using BanchoNET.Abstractions.Services;
using BanchoNET.Commands;
using BanchoNET.Objects.Players;
using BanchoNET.Packets;
using BanchoNET.Services.Repositories;
using BanchoNET.Utils;
using BanchoNET.Utils.Maps;

namespace BanchoNET.Services.ClientPacketsHandler;

public partial class ClientPacketsHandler(
	PlayersRepository players,
	ScoresRepository scores,
	HistoriesRepository histories,
	BeatmapsRepository beatmaps,
	MessagesRepository messages,
	CommandProcessor commands,
	ILobbyScoresQueue scoresQueue)
{
	private readonly BanchoSession _session = BanchoSession.Instance;
	private readonly string[] _ignoredChannels = ["#highlight", "#userlog"];
	
	public async Task ReadPackets(Stream stream, Player player)
	{
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
			
			if (bytesRead != packetSize)
				throw new Exception("Packet size mismatch");

			ms.Position -= bytesRead;
			
			if (PacketHandlerMap.PacketMethodsMap.TryGetValue(packetId, out var method))
				await (Task)method.Invoke(this, [player, br])!;
			else
				throw new InvalidEnumArgumentException($"[ClientPackets] Handler of packet {packetId} is not (yet) " +
				                                       $"implemented or packet does not exist");
			
		} while (br.BaseStream.Position < br.BaseStream.Length);
	}
}