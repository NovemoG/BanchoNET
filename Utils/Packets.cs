using BanchoNET.Packets;

namespace BanchoNET.Utils;

public static class Packets
{
	public const string WelcomeMessage = "Welcome to Utopia!"; //TODO load from config
	public const string RestrictedMessage = "You're restricted!";
	public const string MenuIconUrl = "https://raw.githubusercontent.com/FunOrange/osu-trainer/master/osu-trainer/resources/oof.ico";
	public const string MenuOnclickUrl = "https://raw.githubusercontent.com/FunOrange/osu-trainer/master/osu-trainer/resources/oof.ico";
	
	public static byte[] Write(ServerPackets packetId, object[] dataArray)
	{
		return Write((short)packetId, dataArray);
	}
	
	public static byte[] Write(ClientPackets packetId, object[] dataArray)
	{
		return Write((short)packetId, dataArray);
	}
	
	private static byte[] Write(short packetId, object?[] dataArray)
	{
		var buffer = new MemoryStream();
		using var bw = new BinaryWriter(buffer);
		
		bw.Write(packetId);

		for (int i = 0; i < dataArray.Length; i++)
		{
			var data = dataArray[i];
			
			if (data == null) continue;

			switch (Type.GetTypeCode(data.GetType()))
			{
				case TypeCode.Boolean:
					bw.Write((bool)data);
					break;
				case TypeCode.Byte:
					bw.Write((byte)data);
					break;
				case TypeCode.SByte:
					bw.Write((sbyte)data);
					break;
				case TypeCode.Char:
					bw.Write((char)data);
					break;
				case TypeCode.Int16:
					bw.Write((short)data);
					break;
				case TypeCode.Int32:
					bw.Write((int)data);
					break;
				case TypeCode.Int64:
					bw.Write((long)data);
					break;
				case TypeCode.UInt16:
					bw.Write((ushort)data);
					break;
				case TypeCode.UInt32:
					bw.Write((uint)data);
					break;
				case TypeCode.UInt64:
					bw.Write((ulong)data);
					break;
				case TypeCode.Single:
					bw.Write((float)data);
					break;
				case TypeCode.Double:
					bw.Write((double)data);
					break;
				case TypeCode.DateTime:
					//TODO
					break;
				case TypeCode.String:
					bw.WriteString(data.ToString()!);
					break;
				default:
					throw new ArgumentOutOfRangeException(nameof(dataArray), "Tried to parse a not built-in object. This is not supported.");
			}
		}

		return buffer.ToArray();
	}
}