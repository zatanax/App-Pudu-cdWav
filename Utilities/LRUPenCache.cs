namespace App.Utilities
{
    /// <summary>
    /// LRU (Least Recently Used) cache for Pen objects to prevent memory leaks.
    /// Automatically disposes least recently used pens when capacity is exceeded.
    /// </summary>
    public class LRUPenCache : IDisposable
    {
        private readonly int _capacity;
        private readonly Dictionary<Color, LinkedListNode<(Color Key, Pen Value)>> _cache;
        private readonly LinkedList<(Color Key, Pen Value)> _lruList;
        private bool _disposed;

        public LRUPenCache(int capacity = 50)
        {
            _capacity = capacity;
            _cache = new Dictionary<Color, LinkedListNode<(Color, Pen)>>(capacity);
            _lruList = new LinkedList<(Color, Pen)>();
        }

        public Pen GetPen(Color color)
        {
            if (_cache.TryGetValue(color, out var node))
            {
                // Move to front (most recently used)
                _lruList.Remove(node);
                _lruList.AddFirst(node);
                return node.Value.Value;
            }

            // Create new pen
            var pen = new Pen(color, 1);

            // Evict oldest if at capacity
            if (_cache.Count >= _capacity)
            {
                var oldest = _lruList.Last;
                if (oldest != null)
                {
                    _cache.Remove(oldest.Value.Key);
                    oldest.Value.Value.Dispose();
                    _lruList.RemoveLast();
                }
            }

            // Add new pen to cache
            var newNode = _lruList.AddFirst((color, pen));
            _cache[color] = newNode;

            return pen;
        }

        public void Clear()
        {
            foreach (var node in _lruList)
            {
                node.Value.Dispose();
            }
            _cache.Clear();
            _lruList.Clear();
        }

        public int Count => _cache.Count;

        public void Dispose()
        {
            if (_disposed) return;
            Clear();
            _disposed = true;
            GC.SuppressFinalize(this);
        }
    }
}
