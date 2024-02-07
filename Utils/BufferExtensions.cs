using System.Text;
using BanchoNET.Objects.Channels;

namespace BanchoNET.Utils;

public static class BufferExtensions
{
	public static string ReadOsuString(this BinaryReader br)
	{
		var stringExists = br.ReadByte();
		if (stringExists == 0) return string.Empty;
		if (stringExists != 11)
		{
			//TODO
			return string.Empty;
		}
		
		var total = 0;
		var shift = 0;
		var _byte = br.ReadByte();

		if ((_byte & 0x80) == 0)
		{
			total |= (_byte & 0x7F) << shift;
		}
		else
		{
			var end = false;

			do
			{
				if (shift > 0) 
					_byte = br.ReadByte();

				total |= (_byte & 0x7F) << shift;

				if ((_byte & 0x80) == 0) 
					end = true;

				shift += 7;
			} while (!end);
		}
		
		return Encoding.UTF8.GetString(br.ReadBytes(total));
	}

	public static Message ReadOsuMessage(this BinaryReader br)
	{
		return new Message
		{
			Sender = br.ReadOsuString(),
			Content = br.ReadOsuString(),
			Destination = br.ReadOsuString(),
			SenderId = br.ReadInt32()
		};
	}
	
	/// <summary>
	/// Reads osu! list of 32 bit integers with length encoded as 16 or 32 bit integer
	/// </summary>
	/// <param name="length16bEncoded">Whether list's length is encoded as 16 bit integer, default is 16.</param>
	/// <returns>List of integer values</returns>
	public static IEnumerable<int> ReadOsuList16(this BinaryReader br, bool length16bEncoded = true)
	{
		var returnList = new List<int>();
		var length = length16bEncoded ? br.ReadInt16() : br.ReadInt32();

		while (length > 0)
		{
			returnList.Add(br.ReadInt32());
			length--;
		}

		return returnList;
	}

	public static void WriteOsuString(this BinaryWriter bw, string data)
	{
		if (string.IsNullOrEmpty(data))
		{
			bw.Write((byte)0);
		}
		else
		{
			bw.Write((byte)11);

			var index = 0;
			var bytes = new List<byte>();
			long length = data.Length;

			do
			{
				bytes.Add((byte)(length & 0x7F));

				if ((length >>= 7) != 0)
					bytes[index] |= 0x80;

				index++;
			} while (length > 0);

			foreach (var b in bytes) 
				bw.Write(b);
			
			bw.Write(Encoding.UTF8.GetBytes(data));
		}
	}
}