using System.Collections;

namespace BizLogic.Helpers;

public class BufferedCollection<T> : IEnumerable<T>
{
    private readonly List<T> _items = [];

    public BufferedCollection(IEnumerable<T> source)
    {
        ArgumentNullException.ThrowIfNull(source);
        _items.AddRange(source);
    }

    public int Count => _items.Count;
    public bool IsEmpty => Count == 0;


    public IEnumerable<T> TakeFirst(int count)
    {
        if (count <= 0) return [];

        var actualCount = Math.Min(count, _items.Count);
        var result = _items.Take(actualCount).ToList();
        _items.RemoveRange(0, actualCount);
        return result;
    }


    public IEnumerable<T> TakeLast(int count)
    {
        if (count <= 0) return [];

        var actualCount = Math.Min(count, _items.Count);
        var result = _items.Skip(_items.Count - actualCount).ToList();
        _items.RemoveRange(_items.Count - actualCount, actualCount);
        return result;
    }


    public T? TakeFirst()
    {
        if (IsEmpty) return default(T);
        var item = _items[0];
        _items.RemoveAt(0);
        return item;
    }


    public T? TakeLast()
    {
        if (IsEmpty) return default(T);
        var item = _items[^1];
        _items.RemoveAt(_items.Count - 1);
        return item;
    }


    public IEnumerable<T> TakeAll()
    {
        var result = _items.ToList();
        _items.Clear();
        return result;
    }


    public IEnumerator<T> GetEnumerator() => _items.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}