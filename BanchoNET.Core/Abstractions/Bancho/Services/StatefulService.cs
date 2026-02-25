using System.Collections.Concurrent;
using Novelog.Abstractions;

namespace BanchoNET.Core.Abstractions.Bancho.Services;

public abstract class StatefulService<TKey, TValue>(ILogger logger)
    where TValue : IHasOnlineId<TKey>, IEquatable<TValue>
    where TKey : IEquatable<TKey>
{
    protected readonly ILogger Logger = logger;
    
    protected readonly ConcurrentDictionary<TKey, TValue> Items = new();

    protected virtual bool TryAdd(TValue value) 
        => Items.TryAdd(value.OnlineId, value);
    
    protected virtual bool TryRemove(TKey key, out TValue? value)
        => Items.TryRemove(key, out value);
    
    protected virtual bool TryGet(TKey key, out TValue? value)
        => Items.TryGetValue(key, out value);
    
    protected virtual TValue GetOrAdd(TKey key, Func<TKey, TValue> factory)
        => Items.GetOrAdd(key, factory);
    
    protected virtual TValue AddOrUpdate(TKey key, Func<TKey, TValue> addFactory, Func<TKey, TValue, TValue> updateFactory)
        => Items.AddOrUpdate(key, addFactory, updateFactory);

    protected virtual IEnumerable<TValue> Values => Items.Values;
    protected virtual int Count => Items.Count;
    protected void Clear() => Items.Clear();
}