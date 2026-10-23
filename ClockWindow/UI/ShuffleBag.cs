namespace ClockWindow.UI;

/// <summary>
/// "洗牌袋"随机：把所有元素打乱后依次取，取完一轮再重新洗牌。
/// 保证一轮内不重复，并且跨轮时不会让上一轮的最后一个紧接着成为下一轮的第一个。
/// </summary>
internal sealed class ShuffleBag<T>
{
    private readonly T[] _items;
    private readonly Random _random;
    private readonly List<T> _remaining = new();
    private T? _last;
    private bool _hasLast;

    public ShuffleBag(IEnumerable<T> items, Random random)
    {
        _items = items.ToArray();
        _random = random;
    }

    public int Count => _items.Length;

    public T Next()
    {
        if (_items.Length == 0) throw new InvalidOperationException("ShuffleBag 为空");

        if (_remaining.Count == 0)
        {
            _remaining.AddRange(_items);
            for (int i = _remaining.Count - 1; i > 0; i--)
            {
                int j = _random.Next(i + 1);
                (_remaining[i], _remaining[j]) = (_remaining[j], _remaining[i]);
            }
            // 新一轮的第一个（即列表末尾）如果恰好等于上一轮最后一个，把它和开头交换，推迟到本轮最后再取
            if (_hasLast && _remaining.Count > 1 && EqualityComparer<T>.Default.Equals(_remaining[^1], _last))
                (_remaining[0], _remaining[^1]) = (_remaining[^1], _remaining[0]);
        }

        // 从末尾取，取出顺序就是洗牌后的倒序
        var item = _remaining[^1];
        _remaining.RemoveAt(_remaining.Count - 1);
        _last = item;
        _hasLast = true;
        return item;
    }
}
