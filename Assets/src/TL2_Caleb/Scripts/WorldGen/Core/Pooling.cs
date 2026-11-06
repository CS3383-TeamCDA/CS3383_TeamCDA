using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace EscapeThe90s.WorldGen
{
    /// <summary>Snapshot counters used to verify that warmed chunk pools reuse objects.</summary>
    public readonly struct ChunkPoolMetrics
    {
        public readonly int Created;
        public readonly int RentRequests;
        public readonly int PoolHits;
        public readonly int Returned;
        public readonly int Active;
        public float HitRate { get { return RentRequests == 0 ? 0f : PoolHits / (float)RentRequests; } }

        public ChunkPoolMetrics(int created, int rentRequests, int poolHits, int returned, int active)
        { Created = created; RentRequests = rentRequests; PoolHits = poolHits; Returned = returned; Active = active; }
    }

    /// <summary>Engine-free reusable object pool keyed by chunk kind; the Unity layer supplies the factory.</summary>
    public sealed class ChunkPool<T> where T : class
    {
        readonly Func<ChunkKind, T> _factory;
        readonly Dictionary<ChunkKind, Stack<T>> _available = new Dictionary<ChunkKind, Stack<T>>();
        readonly Dictionary<T, ChunkKind> _leased = new Dictionary<T, ChunkKind>(ReferenceComparer<T>.Instance);
        int _created;
        int _rentRequests;
        int _poolHits;
        int _returned;

        public ChunkPool(Func<ChunkKind, T> factory)
        { _factory = factory ?? throw new ArgumentNullException("factory"); }

        public ChunkPoolMetrics Metrics
        { get { return new ChunkPoolMetrics(_created, _rentRequests, _poolHits, _returned, _leased.Count); } }

        public void Prewarm(ChunkKind kind, int count)
        {
            if (count < 0) throw new ArgumentOutOfRangeException("count");
            Stack<T> pool = GetPool(kind);
            for (int i = 0; i < count; i++) pool.Push(Create(kind));
        }

        public T Rent(ChunkKind kind)
        {
            _rentRequests++;
            Stack<T> pool = GetPool(kind);
            T item;
            if (pool.Count > 0) { item = pool.Pop(); _poolHits++; }
            else item = Create(kind);
            _leased.Add(item, kind);
            return item;
        }

        public bool Return(T item)
        {
            if (ReferenceEquals(item, null)) return false;
            ChunkKind kind;
            if (!_leased.TryGetValue(item, out kind)) return false;
            _leased.Remove(item);
            GetPool(kind).Push(item);
            _returned++;
            return true;
        }

        Stack<T> GetPool(ChunkKind kind)
        {
            Stack<T> pool;
            if (_available.TryGetValue(kind, out pool)) return pool;
            pool = new Stack<T>();
            _available.Add(kind, pool);
            return pool;
        }

        T Create(ChunkKind kind)
        {
            T item = _factory(kind);
            if (ReferenceEquals(item, null)) throw new InvalidOperationException("Chunk pool factory returned null.");
            _created++;
            return item;
        }

        sealed class ReferenceComparer<TItem> : IEqualityComparer<TItem> where TItem : class
        {
            public static readonly ReferenceComparer<TItem> Instance = new ReferenceComparer<TItem>();
            public bool Equals(TItem x, TItem y) { return ReferenceEquals(x, y); }
            public int GetHashCode(TItem obj) { return RuntimeHelpers.GetHashCode(obj); }
        }
    }
}
