using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using EscapeThe90s.Contracts;

namespace EscapeThe90s.WorldGen
{
    /// <summary>
    /// Builds one generated street out of real tiles, sprites and colliders.
    ///  - Follow == null: every chunk is built at once (handy for inspecting a whole street in the Scene view).
    ///  - Follow assigned: only chunks within LoadRadiusChunks of it exist; others load/unload as it moves (play mode).
    /// Right-click the component header and choose "Rebuild". Generated objects are not saved with the scene.
    /// </summary>
    public class StreetView : MonoBehaviour
    {
        public WorldGenSettings settings;
        public WorldArt art;
        [Tooltip("Optional: the player (or any transform). Chunks stream in/out around it.")]
        public Transform follow;
        [Range(1, 6)] public int loadRadiusChunks = 2;
        [Tooltip("Set the main camera to 640x360 pixel scale (orthographic size 11.25) when building.")]
        public bool setupCamera = true;

        [Header("Street")]
        public int nodeA = 0, nodeB = 1;
        [Range(4, 60)] public int chunkCount = 24;
        public int biome = 0;
        public BuildingFlags allowed = BuildingFlags.All;
        public int difficultyTier = 0;
        [Range(0, 3)] public int requiredArcades = 1;
        public bool curbsAndSteps = true;

        GameObject _root, _gridGo;
        StreetLayout _layout;
        StreetTerrain _terrain;
        bool _explicitLayout;
        readonly Dictionary<int, Chunk> _chunks = new Dictionary<int, Chunk>();
        readonly Dictionary<int, List<GameObject>> _chunkOwnedObjects = new Dictionary<int, List<GameObject>>();
        readonly List<int> _scratch = new List<int>();
        readonly LevelProgress _progress = new LevelProgress();
        ChunkPool<Chunk> _chunkPool;
        readonly LevelStreamer _streamer = new LevelStreamer();
        Transform _sourceTransform;
        IPlayerPositionSource _fallbackPositionSource;

        /// <summary>Progress and streamed chunk lifecycle for enemy, pickup, and gameplay systems.</summary>
        public ILevelProgress Progress { get { return _progress; } }
        /// <summary>Optional team-owned position adapter; Follow is wrapped automatically when this is unset.</summary>
        public IPlayerPositionSource PositionSource { get; set; }

        public int WidthCells { get { return _layout == null ? 0 : _layout.ChunkCount * WorldConstants.ChunkWidthCells; } }
        public int LoadedChunkCount { get { return _chunks.Count; } }
        public ChunkPoolMetrics PoolMetrics { get { return _chunkPool == null ? new ChunkPoolMetrics(0, 0, 0, 0, 0) : _chunkPool.Metrics; } }
        public StreetLayout Layout { get { return _layout; } }
        public bool IsChunkLoaded(int chunkIndex) { return _chunks.ContainsKey(chunkIndex); }

        public bool TryGetWorldXBounds(out float minX, out float maxX)
        {
            minX = maxX = 0f;
            if (_root == null || _layout == null) return false;
            float width = WidthCells;
            float x0 = _root.transform.TransformPoint(Vector3.zero).x;
            float x1 = _root.transform.TransformPoint(new Vector3(width, 0f, 0f)).x;
            minX = Mathf.Min(x0, x1);
            maxX = Mathf.Max(x0, x1);
            return true;
        }

        /// <summary>Registers a world-space object to be removed with its streamed terrain chunk.</summary>
        public bool RegisterChunkObject(int chunkIndex, GameObject instance)
        {
            if (instance == null || !_chunks.ContainsKey(chunkIndex)) return false;
            List<GameObject> instances;
            if (!_chunkOwnedObjects.TryGetValue(chunkIndex, out instances))
            {
                instances = new List<GameObject>();
                _chunkOwnedObjects.Add(chunkIndex, instances);
            }
            instances.Add(instance);
            return true;
        }

        /// <summary>Camera y that puts the street surface 3 cells (48 px) above the bottom edge, so exactly the visible ground rows fill the bottom.</summary>
        public static float CameraY(float orthographicSize) { return orthographicSize - WorldConstants.GroundDepthCells; }
        public static float DefaultOrthographicSize { get { return WorldConstants.ViewHeightPixels / 2f / WorldConstants.CellPixels; } }

        void Start() { if (Application.isPlaying && !_explicitLayout) Rebuild(); }

        void Update()
        {
            if (Application.isPlaying && _layout != null && GetPositionSource() != null) UpdateWindow();
        }

        [ContextMenu("Rebuild")]
        public void Rebuild()
        {
            _explicitLayout = false;
            if (settings == null || art == null || art.spriteMaterial == null || art.groundSheet == null)
            {
                Debug.LogWarning("StreetView: assign Settings and Art (with material + ground sheet).");
                return;
            }
            Clear();

            BuildingType[] required = null;
            if (requiredArcades > 0)
            {
                required = new BuildingType[requiredArcades];
                for (int i = 0; i < required.Length; i++) required[i] = BuildingType.Arcade;
            }
            var request = new StreetRequest
            {
                Id = StableId.ForStreet(new Int2(nodeA, 0), new Int2(nodeB, 1)),
                ChunkCount = chunkCount, Biome = biome, Allowed = allowed,
                Required = required, DifficultyTier = difficultyTier
            };
            WorldNetwork world = settings.CreateWorldNetwork();
            BuildLayout(world.GetStreet(request), false);
        }

        /// <summary>Displays an already-generated world street from the region network.</summary>
        public void ShowWorldStreet(WorldStreet street, bool reverse)
        {
            if (street == null) { Debug.LogWarning("StreetView: cannot display a null world street."); return; }
            Debug.Log("[AI demo] EscapeThe90s.WorldGen.StreetView.ShowWorldStreet");
            _explicitLayout = true;
            BuildLayout(street.Layout, reverse);
        }

        void BuildLayout(StreetLayout layout, bool reverse)
        {
            if (settings == null || art == null || art.spriteMaterial == null || art.groundSheet == null)
            {
                Debug.LogWarning("StreetView: assign Settings and Art (with material + ground sheet).");
                return;
            }
            Clear();
            _layout = layout;
            _terrain = new StreetTerrain(settings.SeedValue, _layout, curbsAndSteps);
            EnsureChunkPool();
            PrewarmVisibleChunkKinds();

            _root = new GameObject("Generated Street");
            _root.hideFlags = HideFlags.DontSave;
            _root.transform.SetParent(transform, false);
            if (reverse)
            {
                _root.transform.localScale = new Vector3(-1f, 1f, 1f);
                _root.transform.localPosition = new Vector3(WidthCells, 0f, 0f);
            }
            _gridGo = new GameObject("Grid");
            _gridGo.hideFlags = HideFlags.DontSave;
            _gridGo.transform.SetParent(_root.transform, false);
            var grid = _gridGo.AddComponent<Grid>();
            grid.cellSize = new Vector3(1f, 1f, 0f);   // 1 cell = 1 unit (sprites are 16 PPU, so a cell is 16 px)

            if (GetPositionSource() == null)
                for (int i = 0; i < _layout.ChunkCount; i++) LoadChunk(i);
            else
                UpdateWindow();

            if (setupCamera) SetupCamera();
        }

        void UpdateWindow()
        {
            ChunkStreamWindow window = _streamer.GetWindow(_root.transform, GetPositionSource(),
                _layout.ChunkCount, loadRadiusChunks);
            _progress.AdvanceDistance(Mathf.Max(0f, window.StreetLocalX));
            for (int i = window.FirstChunk; i <= window.LastChunk; i++)
                if (!_chunks.ContainsKey(i)) LoadChunk(i);

            _scratch.Clear();
            foreach (var kv in _chunks)
                if (_streamer.ShouldRecycle(kv.Key, window.Center, loadRadiusChunks)) _scratch.Add(kv.Key);
            for (int k = 0; k < _scratch.Count; k++)
            {
                int chunkIndex = _scratch[k];
                RecycleChunk(chunkIndex);
            }
        }

        void SetupCamera()
        {
            Camera cam = Camera.main;
            if (cam == null) return;
            cam.orthographic = true;
            cam.orthographicSize = DefaultOrthographicSize;
            if (follow == null)   // with a target, CameraFollow positions the camera
                cam.transform.position = new Vector3(WorldConstants.ViewWidthPixels / 2f / WorldConstants.CellPixels,
                                                     CameraY(cam.orthographicSize), -10f);
        }

        IPlayerPositionSource GetPositionSource()
        {
            if (PositionSource != null) return PositionSource;
            if (follow == null) { _sourceTransform = null; _fallbackPositionSource = null; return null; }
            if (_sourceTransform != follow || _fallbackPositionSource == null)
            {
                _sourceTransform = follow;
                _fallbackPositionSource = new TransformPlayerPositionSource(follow);
            }
            return _fallbackPositionSource;
        }

        void LoadChunk(int index)
        {
            ChunkData content = _layout.Chunks[index];
            ChunkSpec spec = GetChunkSpec(index, content);
            Chunk chunk = _chunkPool.Rent(spec.Kind);
            chunk.Populate(_gridGo.transform, _layout, _terrain, content, spec, art,
                index == _layout.ChunkCount - 1);
            _chunks[index] = chunk;
            NotifyChunkSpawned(index);
        }

        ChunkSpec GetChunkSpec(int index, ChunkData content)
        {
            if (_layout.ChunkSpecs != null && index < _layout.ChunkSpecs.Length) return _layout.ChunkSpecs[index];
            return new ChunkSpec(content, ChunkKind.Ground, TerrainStamp.Flat, _layout.Kind, _layout.Biome,
                _progress.GetCurrentDifficulty(), null);
        }

        void EnsureChunkPool()
        {
            if (_chunkPool == null) _chunkPool = new ChunkPool<Chunk>(CreatePooledChunk);
        }

        Chunk CreatePooledChunk(ChunkKind kind)
        {
            var go = new GameObject("Pooled Chunk (" + kind + ")");
            go.hideFlags = HideFlags.DontSave;
            go.transform.SetParent(transform, false);
            Chunk view;
            switch (kind)
            {
                case ChunkKind.Gap: view = go.AddComponent<GapChunk>(); break;
                case ChunkKind.Ramp: view = go.AddComponent<RampChunk>(); break;
                case ChunkKind.Plaza: view = go.AddComponent<PlazaChunk>(); break;
                default: view = go.AddComponent<GroundChunk>(); break;
            }
            view.Initialize();
            return view;
        }

        void PrewarmVisibleChunkKinds()
        {
            int visibleCapacity = GetPositionSource() == null ? _layout.ChunkCount : Mathf.Min(_layout.ChunkCount, loadRadiusChunks * 2 + 3);
            var counts = new Dictionary<ChunkKind, int>();
            for (int i = 0; i < _layout.ChunkCount; i++)
            {
                ChunkKind kind = GetChunkSpec(i, _layout.Chunks[i]).Kind;
                int count;
                counts.TryGetValue(kind, out count);
                if (count < visibleCapacity) counts[kind] = count + 1;
            }
            foreach (KeyValuePair<ChunkKind, int> pair in counts) _chunkPool.Prewarm(pair.Key, pair.Value);
        }

        void NotifyChunkSpawned(int index)
        {
            ChunkSpec spec = GetChunkSpec(index, _layout.Chunks[index]);
            float x0 = index * WorldConstants.ChunkWidthCells;
            float x1 = x0 + WorldConstants.ChunkWidthCells;
            Vector3 min = _root.transform.TransformPoint(new Vector3(x0, -WorldConstants.SlabDepthCells, 0f));
            Vector3 max = _root.transform.TransformPoint(new Vector3(x1, Mathf.Max(1, _terrain.MaxHeight + 1), 0f));
            var id = new ChunkId(_layout.Id.Value, index);
            _progress.NotifyChunkSpawned(new ChunkSpawnedInfo(id,
                new ChunkBounds(Mathf.Min(min.x, max.x), min.y, Mathf.Max(min.x, max.x), max.y),
                spec.Difficulty, DifficultyProgression.TierAtDifficulty(spec.Difficulty), spec.Biome,
                spec.Kind, spec.SpawnLocations));
        }

        void RecycleChunk(int index)
        {
            if (!_chunks.TryGetValue(index, out Chunk chunk)) return;
            _progress.NotifyChunkRecycled(new ChunkId(_layout.Id.Value, index));
            DestroyChunkObjects(index);
            chunk.ResetForPool();
            chunk.transform.SetParent(transform, false);
            _chunkPool.Return(chunk);
            _chunks.Remove(index);
        }

        static void DestroyObject(GameObject go)
        {
            if (Application.isPlaying) Destroy(go); else DestroyImmediate(go);
        }

        void DestroyChunkObjects(int chunkIndex)
        {
            List<GameObject> instances;
            if (!_chunkOwnedObjects.TryGetValue(chunkIndex, out instances)) return;
            foreach (GameObject instance in instances)
                if (instance != null) DestroyObject(instance);
            _chunkOwnedObjects.Remove(chunkIndex);
        }

        void Clear()
        {
            if (_layout != null)
            {
                _scratch.Clear();
                foreach (int index in _chunks.Keys) _scratch.Add(index);
                for (int i = 0; i < _scratch.Count; i++) RecycleChunk(_scratch[i]);
            }
            var ownedChunks = new List<int>(_chunkOwnedObjects.Keys);
            foreach (int chunkIndex in ownedChunks) DestroyChunkObjects(chunkIndex);
            _chunks.Clear();
            _layout = null;
            if (_root == null)
            {
                var existing = transform.Find("Generated Street");
                if (existing != null) _root = existing.gameObject;
            }
            if (_root == null) return;
            DestroyObject(_root);
            _root = null;
        }

        void OnDisable() { if (!Application.isPlaying) Clear(); }
    }
}
