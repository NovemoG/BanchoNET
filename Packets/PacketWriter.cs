using BanchoNET.Utils;

namespace BanchoNET.Packets;

public partial class ServerPackets
{
	private void WritePacketData(ServerPacketId packetId, IReadOnlyList<object?> dataArray)
	{
		var buffer = new MemoryStream();
		var bw = new BinaryWriter(buffer);
		
		bw.Write((short)packetId);
		bw.Write((byte)0);

		var writeActions = new Dictionary<TypeCode, Action<object>>
		{
			{ TypeCode.Boolean, data => bw.Write((bool)data) },
			{ TypeCode.Byte, data => bw.Write((byte)data) },
			{ TypeCode.SByte, data => bw.Write((sbyte)data) },
			{ TypeCode.Char, data => bw.Write((char)data) },
			{ TypeCode.Int16, data => bw.Write((short)data) },
			{ TypeCode.Int32, data => bw.Write((int)data) },
			{ TypeCode.Int64, data => bw.Write((long)data) },
			{ TypeCode.UInt16, data => bw.Write((ushort)data) },
			{ TypeCode.UInt32, data => bw.Write((uint)data) },
			{ TypeCode.UInt64, data => bw.Write((ulong)data) },
			{ TypeCode.Single, data => bw.Write((float)data) },
			{ TypeCode.Double, data => bw.Write((double)data) },
			{ TypeCode.String, data => bw.WriteString(data.ToString()!) }
		};
		
		for (int i = 0; i < dataArray.Count; i++)
		{
			var data = dataArray[i];
			if (data == null) continue;

			var typeCode = Type.GetTypeCode(data.GetType());
			
			if (writeActions.TryGetValue(typeCode, out var action))
				action(data);
		}

		//TODO optimize it (?)
		var dataBytes = buffer.ToArray();
		var returnStream = new MemoryStream();
		
		returnStream.Write(dataBytes, 0, 3);
		returnStream.Write(BitConverter.GetBytes(dataBytes.Length - 3));
		returnStream.Write(dataBytes, 3, dataBytes.Length - 3);
		
		BinaryWriter.Write(returnStream.ToArray());
	}
}