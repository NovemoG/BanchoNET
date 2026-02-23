namespace BanchoNET.Core.Utils;

public static class ULEB128
{
	public static int ReadULEB128(this BinaryReader br)
	{
		var total = 0;
		var shift = 0;
		var @byte = br.ReadByte();

		if ((@byte & 0x80) == 0)
			total |= (@byte & 0x7F) << shift;
		else
		{
			var end = false;

			do
			{
				if (shift > 0) 
					@byte = br.ReadByte();

				total |= (@byte & 0x7F) << shift;

				if ((@byte & 0x80) == 0) 
					end = true;

				shift += 7;
			} while (!end);
		}

		return total;
	}
	
	public static void WriteULEB128(this BinaryWriter bw, int number)
	{
		if (number == 0)
		{
			bw.Write((byte)0);
			return;
		}
		
		var index = 0;
		var bytes = new List<byte>();
			
		do
		{
			bytes.Add((byte)(number & 0x7F));

			if ((number >>= 7) != 0)
				bytes[index] |= 0x80;

			index++;
		} while (number > 0);

		foreach (var b in bytes) 
			bw.Write(b);
		
		using var br = new BinaryReader(new MemoryStream(bytes.ToArray()));
	}
}