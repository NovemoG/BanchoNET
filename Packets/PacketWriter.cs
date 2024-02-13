using BanchoNET.Objects.Channels;
using BanchoNET.Objects.Multiplayer;
using BanchoNET.Objects.Players;
using BanchoNET.Utils;

namespace BanchoNET.Packets;

public partial class ServerPackets
{
	private readonly Dictionary<DataType, Action<BinaryWriter, object>> _actionsMap = new()
	{
		[DataType.SByte] = (bw, data) => bw.Write((sbyte)data),
		[DataType.Byte] = (bw, data) => bw.Write((byte)data),
		[DataType.Short] = (bw, data) => bw.Write((short)data),
		[DataType.UShort] = (bw, data) => bw.Write((ushort)data),
		[DataType.Int] = (bw, data) => bw.Write((int)data),
		[DataType.UInt] = (bw, data) => bw.Write((uint)data),
		[DataType.Long] = (bw, data) => bw.Write((long)data),
		[DataType.ULong] = (bw, data) => bw.Write((ulong)data),
		[DataType.Float] = (bw, data) => bw.Write((float)data),
		[DataType.Double] = (bw, data) => bw.Write((double)data),
		[DataType.Message] = (bw, data) => bw.WriteOsuMessage((Message)data),
		[DataType.Channel] = (bw, data) => bw.WriteOsuChannel((Channel)data),
		[DataType.Match] = (bw, data) => bw.WriteOsuMatch((MultiplayerLobby)data),
		[DataType.Stats] = (bw, data) => bw.WriteUserStats((Player)data),
		[DataType.BotStats] = (bw, data) => bw.WriteBotStats((Player)data),
		[DataType.Presence] = (bw, data) => bw.WriteUserPresence((Player)data),
		[DataType.BotPresence] = (bw, data) => bw.WriteBotPresence((Player)data),
		[DataType.ScoreFrame] = (bw, data) => bw.Write((byte)data), //TODO
		[DataType.MapInfoRequest] = (bw, data) => bw.Write((sbyte)data), //TODO
		[DataType.MapInfoReply] = (bw, data) => bw.Write((byte)data), //TODO
		[DataType.ReplayFrameBundle] = (bw, data) => bw.Write((byte)data), //TODO
		[DataType.IntList] = (bw, data) => bw.WriteOsuList32((List<int>)data),
		[DataType.String] = (bw, data) => bw.WriteOsuString(data.ToString()),
		[DataType.Raw] = (bw, data) => bw.Write((byte[])data),
	};
	
	private void WritePacketData(ServerPacketId packetId, params PacketData[]? dataArray)
	{
		var buffer = new MemoryStream();
		using var bw = new BinaryWriter(buffer);
		
		bw.Write((short)packetId);
		bw.Write((byte)0);
		if (dataArray == null)
		{
			bw.Write((int)0);
			_binaryWriter.Write(buffer.ToArray());
			return;
		}
		
		for (int i = 0; i < dataArray.Length; i++)
		{
			var data = dataArray[i];
			Console.WriteLine($"[{GetType().Name}] Parsing data: {data.Data} of type {data.Type}");

			_actionsMap[data.Type](bw, data.Data!);
		}
		
		var dataBytes = buffer.ToArray();
		var returnStream = new MemoryStream();
		
		//Writes length of the packet data inside the stream
		returnStream.Write(dataBytes, 0, 3);
		returnStream.Write(BitConverter.GetBytes(dataBytes.Length - 3));
		returnStream.Write(dataBytes, 3, dataBytes.Length - 3);
		
		_binaryWriter.Write(returnStream.ToArray());
	}
}