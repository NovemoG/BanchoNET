namespace BanchoNET.Core.Abstractions;

public interface IHasOnlineId<out T>
    where T : IEquatable<T>
{
    T OnlineId { get; }
}