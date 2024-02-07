using BanchoNET.Utils;

namespace BanchoNET.Packets;

public partial class ServerPackets
{
	private readonly Dictionary<TypeCode, Action<BinaryWriter, object>> _writeActionsMap = new()
	{
		{ TypeCode.Boolean, (bw, data) => bw.Write((bool)data) },
		{ TypeCode.Byte, (bw, data) => bw.Write((byte)data) },
		{ TypeCode.SByte, (bw, data) => bw.Write((sbyte)data) },
		{ TypeCode.Char, (bw, data) => bw.Write((char)data) },
		{ TypeCode.Int16, (bw, data) => bw.Write((short)data) },
		{ TypeCode.Int32, (bw, data) => bw.Write((int)data) },
		{ TypeCode.Int64, (bw, data) => bw.Write((long)data) },
		{ TypeCode.UInt16, (bw, data) => bw.Write((ushort)data) },
		{ TypeCode.UInt32, (bw, data) => bw.Write((uint)data) },
		{ TypeCode.UInt64, (bw, data) => bw.Write((ulong)data) },
		{ TypeCode.Single, (bw, data) => bw.Write((float)data) },
		{ TypeCode.Double, (bw, data) => bw.Write((double)data) },
		{ TypeCode.String, (bw, data) => bw.WriteOsuString(data.ToString()!) }
	};
	
	private void WritePacketData(ServerPacketId packetId, params object[]? dataArray)
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
			var typeCode = Type.GetTypeCode(data.GetType());
			
			if (_writeActionsMap.TryGetValue(typeCode, out var action))
				action(bw, data);
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