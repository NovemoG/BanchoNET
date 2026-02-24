using System.Collections.Concurrent;
using Novelog.Abstractions;

namespace BanchoNET.Core.Abstractions.Bancho.Services;

public abstract class StatefulService<TKey, TValue>(ILogger logger)
    where TValue : IHasOnlineId<TKey>, IEquatable<TValue>
    where TKey : IEquatable<TKey>
{
    protected readonly ILogger Logger = logger;
    
    protected readonly ConcurrentDictionary<TKey, TValue> _items = new();
    public IReadOnlyDictionary<TKey, TValue> Items => _items;

    protected virtual bool TryAdd(TKey key, TValue value) 
        => _items.TryAdd(key, value);
    
    protected virtual bool TryRemove(TKey key, out TValue? value)
        => _items.TryRemove(key, out value);
    
    protected virtual bool TryGet(TKey key, out TValue? value)
        => _items.TryGetValue(key, out value);
    
    protected virtual TValue GetOrAdd(TKey key, Func<TKey, TValue> factory)
        => _items.GetOrAdd(key, factory);
    
    protected virtual TValue AddOrUpdate(TKey key, Func<TKey, TValue> addFactory, Func<TKey, TValue, TValue> updateFactory)
        => _items.AddOrUpdate(key, addFactory, updateFactory);

    protected virtual IEnumerable<TValue> Values => _items.Values;
    protected virtual int Count => _items.Count;
    protected void Clear() => _items.Clear();
}