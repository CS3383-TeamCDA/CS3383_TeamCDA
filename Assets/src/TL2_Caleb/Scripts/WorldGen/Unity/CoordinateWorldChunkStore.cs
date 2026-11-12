using System;
using System.Collections.Generic;
using EscapeThe90s.Contracts;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace EscapeThe90s.WorldGen
{
    /// <summary>Owns loaded coordinate data, pooled views, and coordinate lifecycle events.</summary>
    public sealed class CoordinateWorldChunkStore
    {
        readonly Transform _viewRoot;
        readonly WorldArt _art;
        readonly IWorldProfile _profile;
        readonly LevelProgress _progress;
        readonly int _buildBudget;
        readonly int _hysteresis;
        readonly Dictionary<WorldChunkCoord, CoordinateChunkView> _loaded;
        readonly List<PendingChunk> _pending;
        readonly List<WorldChunkCoord> _recycle;
        readonly ChunkPool<CoordinateChunkView> _pool;
        static readonly Comparison<PendingChunk> PendingComparer = PendingChunk.Compare;

        public int LoadedCount { get { return _loaded.Count; } }
        public ChunkPoolMetrics PoolMetrics { get { return _pool.Metrics; } }

        /// <summary>Draws loaded chunk bounds, ids, and fill classifications for editor debugging.</summary>
        public void DrawDebugGizmos()
        {
            foreach (KeyValuePair<WorldChunkCoord, CoordinateChunkView> entry in _loaded)
            {
                WorldChunkCoord coordinate = entry.Key;
                WorldChunkKind kind = _profile.GetChunkKind(coordinate);
                Gizmos.color = ColorForKind(kind);
                Vector3 center = new Vector3(coordinate.MinCellX + WorldChunkCoord.Size * 0.5f,
                    coordinate.MinCellY + WorldChunkCoord.Size * 0.5f, 0f);
                Gizmos.DrawWireCube(center, new Vector3(WorldChunkCoord.Size, WorldChunkCoord.Size, 0f));
#if UNITY_EDITOR
                Handles.color = ColorForKind(kind);
                Handles.Label(center, "(" + coordinate.X + ", " + coordinate.Y + ") " + kind);
#endif
            }
        }

        public CoordinateWorldChunkStore(Transform owner, WorldArt art, IWorldProfile profile,
            LevelProgress progress, int buildBudget, int hysteresis, int prewarmCount,
            int maximumWindowSize)
        {
            _art = art;
            _profile = profile;
            _progress = progress;
            _buildBudget = Mathf.Max(1, buildBudget);
            _hysteresis = Mathf.Max(0, hysteresis);
            int capacity = Mathf.Max(16, maximumWindowSize);
            _loaded = new Dictionary<WorldChunkCoord, CoordinateChunkView>(capacity);
            _pending = new List<PendingChunk>(capacity);
            _recycle = new List<WorldChunkCoord>(capacity);
            var root = new GameObject("Coordinate World Chunk Views");
            root.hideFlags = HideFlags.DontSave;
            root.transform.SetParent(owner, false);
            _viewRoot = root.transform;
            _pool = new ChunkPool<CoordinateChunkView>(CreateView);
            if (prewarmCount > 0) _pool.Prewarm(ChunkKind.Ground, prewarmCount);
        }

        public void Stream(WorldStreamWindow window, float originX)
        {
            _pending.Clear();
            for (int y = window.MinY; y <= window.MaxY; y++)
                for (int x = window.MinX; x <= window.MaxX; x++)
                {
                    var coordinate = new WorldChunkCoord(x, y);
                    if (_loaded.ContainsKey(coordinate)) continue;
                    WorldChunkKind kind = _profile.GetChunkKind(coordinate);
                    if (kind == WorldChunkKind.Surface)
                        _pending.Add(new PendingChunk(coordinate, Priority(coordinate, window)));
                    else
                        _loaded.Add(coordinate, null);
                }

            _pending.Sort(PendingComparer);
            int count = Mathf.Min(_buildBudget, _pending.Count);
            for (int i = 0; i < count; i++) LoadSurfaceChunk(_pending[i].Coordinate, originX);
            RecycleOutsideWindow(window);
        }

        void LoadSurfaceChunk(WorldChunkCoord coordinate, float originX)
        {
            WorldChunkData data = _profile.GenerateChunk(coordinate);
            CoordinateChunkView view = _pool.Rent(ChunkKind.Ground);
            view.Populate(data, _profile, _art);
            _loaded.Add(coordinate, view);
            float minX = coordinate.MinCellX - 0.5f;
            float minY = coordinate.MinCellY - 0.5f;
            var bounds = new ChunkBounds(minX, minY,
                minX + WorldChunkCoord.Size, minY + WorldChunkCoord.Size);
            float distance = Mathf.Max(0f, coordinate.MinCellX - originX);
            float difficulty = _progress.GetDifficultyAt(distance);
            var id = ChunkId.ForWorldCoordinate(coordinate.X, coordinate.Y);
            var info = new ChunkSpawnedInfo(id, bounds, difficulty,
                DifficultyProgression.TierAtDifficulty(difficulty), data.Biome,
                ChunkKind.Ground, data.SpawnLocations);
            _progress.NotifyChunkSpawned(info);
        }

        void RecycleOutsideWindow(WorldStreamWindow window)
        {
            _recycle.Clear();
            foreach (KeyValuePair<WorldChunkCoord, CoordinateChunkView> entry in _loaded)
            {
                WorldChunkCoord coordinate = entry.Key;
                if (coordinate.X < window.MinX - _hysteresis || coordinate.X > window.MaxX + _hysteresis ||
                    coordinate.Y < window.MinY - _hysteresis || coordinate.Y > window.MaxY + _hysteresis)
                    _recycle.Add(coordinate);
            }
            for (int i = 0; i < _recycle.Count; i++) Recycle(_recycle[i]);
        }

        void Recycle(WorldChunkCoord coordinate)
        {
            if (!_loaded.TryGetValue(coordinate, out CoordinateChunkView view)) return;
            _loaded.Remove(coordinate);
            if (view == null) return;
            _progress.NotifyChunkRecycled(ChunkId.ForWorldCoordinate(coordinate.X, coordinate.Y));
            view.ResetForPool();
            _pool.Return(view);
        }

        CoordinateChunkView CreateView(ChunkKind kind)
        {
            var chunkObject = new GameObject("Pooled Coordinate Chunk");
            chunkObject.hideFlags = HideFlags.DontSave;
            chunkObject.transform.SetParent(_viewRoot, false);
            CoordinateChunkView view = chunkObject.AddComponent<CoordinateChunkView>();
            view.ResetForPool();
            return view;
        }

        static int Priority(WorldChunkCoord coordinate, WorldStreamWindow window)
        {
            int deltaX = coordinate.X - window.Center.X;
            int deltaY = coordinate.Y - window.Center.Y;
            int behindPenalty = deltaX * window.DirectionX < 0 ? WorldChunkCoord.Size * 4 : 0;
            return Mathf.Abs(deltaX) * 3 + Mathf.Abs(deltaY) * 2 + behindPenalty;
        }

        static Color ColorForKind(WorldChunkKind kind)
        {
            switch (kind)
            {
                case WorldChunkKind.Sky: return new Color(0.2f, 0.8f, 1f, 0.9f);
                case WorldChunkKind.Solid: return new Color(0.9f, 0.5f, 0.2f, 0.9f);
                case WorldChunkKind.Void: return new Color(0.5f, 0.3f, 0.8f, 0.9f);
                default: return new Color(1f, 0.9f, 0.2f, 1f);
            }
        }

        public void Clear()
        {
            _recycle.Clear();
            foreach (WorldChunkCoord coordinate in _loaded.Keys) _recycle.Add(coordinate);
            for (int i = 0; i < _recycle.Count; i++) Recycle(_recycle[i]);
        }

        readonly struct PendingChunk
        {
            public readonly WorldChunkCoord Coordinate;
            readonly int _priority;
            public PendingChunk(WorldChunkCoord coordinate, int priority) { Coordinate = coordinate; _priority = priority; }
            public static int Compare(PendingChunk first, PendingChunk second)
            {
                int priority = first._priority.CompareTo(second._priority);
                return priority != 0 ? priority : first.Coordinate.CompareTo(second.Coordinate);
            }
        }
    }
}
