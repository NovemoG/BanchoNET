using BanchoNET.Objects.Channels;
using BanchoNET.Objects.Multiplayer;
using BanchoNET.Objects.Players;
using BanchoNET.Utils.Extensions;

namespace BanchoNET.Packets;

public sealed partial class ServerPackets
{
	private static readonly Dictionary<DataType, Action<BinaryWriter, object>> ActionsMap = new()
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
		[DataType.Match] = (bw, data) => bw.WriteOsuMatch((LobbyData)data),
		[DataType.Stats] = (bw, data) => bw.WriteUserStats((Player)data),
		[DataType.BotStats] = (bw, data) => bw.WriteBotStats((Player)data),
		[DataType.Presence] = (bw, data) => bw.WriteUserPresence((Player)data),
		[DataType.BotPresence] = (bw, data) => bw.WriteBotPresence((Player)data),
		[DataType.ScoreFrame] = (bw, data) => bw.Write((byte)data),
		[DataType.MapInfoRequest] = (bw, data) => bw.Write((sbyte)data),
		[DataType.MapInfoReply] = (bw, data) => bw.Write((byte)data),
		[DataType.ReplayFrameBundle] = (bw, data) => bw.Write((byte)data),
		[DataType.IntList] = (bw, data) => bw.WriteOsuList32((List<int>)data),
		[DataType.String] = (bw, data) => bw.WriteOsuString(data.ToString()),
		[DataType.Raw] = (bw, data) => bw.Write((byte[])data),
	};
	
	private void WritePacketData(ServerPacketId packetId, params PacketData[] dataArray)
	{
		_binaryWriter.Write((short)packetId);
		_binaryWriter.Write((byte)0);
		
		if (dataArray.Length == 0)
		{
			_binaryWriter.Write((int)0);
			return;
		}
		
		var lengthPosition = _binaryWriter.BaseStream.Position;
		_binaryWriter.Write((int)0);
		
		foreach (var data in dataArray)
			ActionsMap[data.Type](_binaryWriter, data.Data!);

		var endPosition = _binaryWriter.BaseStream.Position;
		var payloadLength = (int)(endPosition - (lengthPosition + 4));

		_binaryWriter.BaseStream.Seek(lengthPosition, SeekOrigin.Begin);
		_binaryWriter.Write(payloadLength);
		_binaryWriter.BaseStream.Seek(endPosition, SeekOrigin.Begin);
	}
}