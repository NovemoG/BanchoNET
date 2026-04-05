using MessagePack;
using MessagePack.Formatters;
using MessagePack.Resolvers;

namespace BanchoNET.Core.Utils.SignalR;

public class SignalRUnionWorkaroundResolver : IFormatterResolver
{
    public static readonly MessagePackSerializerOptions Options =
            MessagePackSerializerOptions.Standard.WithResolver(new SignalRUnionWorkaroundResolver());

        private static readonly IReadOnlyDictionary<Type, IMessagePackFormatter> FormatterMap = CreateFormatterMap();

        private static IReadOnlyDictionary<Type, IMessagePackFormatter> CreateFormatterMap()
        {
            IEnumerable<(Type derivedType, Type baseType)> baseMap = SignalRWorkaroundTypes.BaseTypeMapping;

            // This should not be required. The fallback should work. But something is weird with the way caching is done.
            // For future adventurers, I would not advise looking into this further. It's likely not worth the effort.
            baseMap = baseMap.Concat(baseMap.Select(t => (t.baseType, t.baseType)).Distinct());

            return new Dictionary<Type, IMessagePackFormatter>(baseMap.Select(t =>
            {
                var formatter = (IMessagePackFormatter)Activator.CreateInstance(typeof(TypeRedirectingFormatter<,>).MakeGenericType(t.derivedType, t.baseType))!;
                return new KeyValuePair<Type, IMessagePackFormatter>(t.derivedType, formatter);
            }));
        }

        public IMessagePackFormatter<T> GetFormatter<T>()
        {
            if (FormatterMap.TryGetValue(typeof(T), out var formatter))
                return (IMessagePackFormatter<T>)formatter;

            return StandardResolver.Instance.GetFormatter<T>()!;
        }

        public class TypeRedirectingFormatter<TActual, TBase> : IMessagePackFormatter<TActual>
        {
            private readonly IMessagePackFormatter<TBase> _baseFormatter;

            public TypeRedirectingFormatter()
            {
                _baseFormatter = StandardResolver.Instance.GetFormatter<TBase>()!;
            }

            public void Serialize(ref MessagePackWriter writer, TActual value, MessagePackSerializerOptions options) =>
                _baseFormatter.Serialize(ref writer, (TBase)(object)value!, StandardResolver.Options);

            public TActual Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options) =>
                (TActual)(object)_baseFormatter.Deserialize(ref reader, StandardResolver.Options)!;
        }
}