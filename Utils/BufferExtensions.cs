using System.Text;

namespace BanchoNET.Utils;

public static class BufferExtensions
{
	public static string ReadString(this BinaryReader br)
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
				{
					_byte = br.ReadByte();
				}

				total |= (_byte & 0x7F) << shift;

				if ((_byte & 0x80) == 0)
				{
					end = true;
				}

				shift += 7;
			} while (!end);
		}
			
		return Encoding.UTF8.GetString(br.ReadBytes(total));
	}

	public static void WriteString(this BinaryWriter bw, string data)
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
				{
					bytes[index] |= 0x80;
				}

				index++;
			} while (length > 0);

			foreach (var b in bytes)
			{
				bw.Write(b);
			}
			
			bw.Write(Encoding.UTF8.GetBytes(data));
		}
	}
}