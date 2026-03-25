using System.Buffers;
using System.Text;
using MessagePack;
using MessagePack.Formatters;

namespace BanchoNET.Core.Utils;

public class ModSettingsDictionaryFormatter : IMessagePackFormatter<Dictionary<string, object>?>
{
    public void Serialize(
        ref MessagePackWriter writer,
        Dictionary<string, object>? value,
        MessagePackSerializerOptions options
    ) {
        if (value == null) return;

        var primitiveFormatter = PrimitiveObjectFormatter.Instance;
        
        writer.WriteArrayHeader(value.Count);

        foreach (var kvp in value)
        {
            var stringBytes = new ReadOnlySequence<byte>(Encoding.UTF8.GetBytes(kvp.Key));
            writer.WriteString(in stringBytes);
            
            primitiveFormatter.Serialize(ref writer, kvp.Value, options);
        }
    }

    public Dictionary<string, object> Deserialize(
        ref MessagePackReader reader,
        MessagePackSerializerOptions options
    ) {
        var output = new Dictionary<string, object>();
        var itemCount = reader.ReadArrayHeader();

        for (var i = 0; i < itemCount; i++)
        {
            output[reader.ReadString()!] = PrimitiveObjectFormatter.Instance.Deserialize(ref reader, options)!;
        }

        return output;
    }
}