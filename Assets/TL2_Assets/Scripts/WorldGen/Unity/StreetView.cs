using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

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
        readonly Dictionary<int, GameObject> _chunks = new Dictionary<int, GameObject>();
        readonly Dictionary<int, List<GameObject>> _chunkOwnedObjects = new Dictionary<int, List<GameObject>>();
        readonly List<int> _scratch = new List<int>();

        public int WidthCells { get { return _layout == null ? 0 : _layout.ChunkCount * WorldConstants.ChunkWidthCells; } }
        public int LoadedChunkCount { get { return _chunks.Count; } }
        public StreetLayout Layout { get { return _layout; } }
        public bool IsChunkLoaded(int chunkIndex) { return _chunks.ContainsKey(chunkIndex); }

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
            if (Application.isPlaying && _layout != null && follow != null) UpdateWindow();
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
            LevelService level = settings.CreateService();
            BuildLayout(level.GetStreet(request), false);
        }

        /// <summary>Displays an already-generated world street from the region network.</summary>
        public void ShowWorldStreet(WorldStreet street, bool reverse)
        {
            if (street == null) { Debug.LogWarning("StreetView: cannot display a null world street."); return; }
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

            if (follow == null)
                for (int i = 0; i < _layout.ChunkCount; i++) LoadChunk(i);
            else
                UpdateWindow();

            if (setupCamera) SetupCamera();
        }

        void UpdateWindow()
        {
            float x = _root.transform.InverseTransformPoint(follow.position).x;
            int center = ChunkWindow.ChunkIndexAt(x, _layout.ChunkCount);

            int lo = Mathf.Max(0, center - loadRadiusChunks);
            int hi = Mathf.Min(_layout.ChunkCount - 1, center + loadRadiusChunks);
            for (int i = lo; i <= hi; i++)
                if (!_chunks.ContainsKey(i)) LoadChunk(i);

            _scratch.Clear();
            foreach (var kv in _chunks)
                if (ChunkWindow.ShouldUnload(kv.Key, center, loadRadiusChunks)) _scratch.Add(kv.Key);
            for (int k = 0; k < _scratch.Count; k++)
            {
                int chunkIndex = _scratch[k];
                DestroyChunkObjects(chunkIndex);
                DestroyObject(_chunks[chunkIndex]);
                _chunks.Remove(chunkIndex);
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

        void LoadChunk(int index)
        {
            _chunks[index] = BuildChunk(_gridGo.transform, _layout, _terrain, _layout.Chunks[index], index == _layout.ChunkCount - 1);
        }

        GameObject BuildChunk(Transform parent, StreetLayout layout, StreetTerrain terrain, ChunkData chunk, bool last)
        {
            int w = WorldConstants.ChunkWidthCells;
            int x0 = chunk.Index * w;

            var chunkGo = new GameObject("Chunk " + chunk.Index);
            chunkGo.hideFlags = HideFlags.DontSave;
            chunkGo.transform.SetParent(parent, false);
            chunkGo.transform.localPosition = new Vector3(x0, 0f, 0f);

            // Ground art: render tiles are centred on cell corners, so the tilemap is shifted half a cell.
            var groundGo = new GameObject("Ground");
            groundGo.hideFlags = HideFlags.DontSave;
            groundGo.transform.SetParent(chunkGo.transform, false);
            groundGo.transform.localPosition = new Vector3(-0.5f, -0.5f, 0f);
            var tilemap = groundGo.AddComponent<Tilemap>();
            var tr = groundGo.AddComponent<TilemapRenderer>();
            tr.sortingOrder = 0;
            int lastX = last ? w : w - 1;   // the final chunk also owns the tile on the street's right end
            for (int lx = 0; lx <= lastX; lx++)
                for (int ry = -WorldConstants.GroundDepthCells; ry <= terrain.MaxHeight; ry++)
                {
                    int mask = DualGrid.MaskAt(terrain, x0 + lx, ry);
                    if (mask != 0) tilemap.SetTile(new Vector3Int(lx, ry, 0), art.GetGroundTile(mask));
                }
            art.ApplyPalette(tr, WorldArt.PaletteRow(layout.Biome, 0));

            // Physics: exact boxes from the data grid (never from the tile art).
            foreach (ColliderBox b in TerrainColliders.ForChunk(terrain, chunk.Index))
                AddBox(chunkGo, b.X0, b.X1, b.Y0, b.Y1);
            // Network play uses these walls as safe points where the player chooses an intersection exit.
            if (chunk.Index == 0) AddBox(chunkGo, -1, 0, -WorldConstants.SlabDepthCells, 40);
            if (last) AddBox(chunkGo, w, w + 1, -WorldConstants.SlabDepthCells, 40);

            // Building, centred in the chunk, standing on the surface (sprite pivot = bottom centre).
            if (chunk.Building != BuildingType.None)
            {
                Sprite sprite = art.GetBuildingSprite(chunk.Building, chunk.Variant);
                if (sprite != null)
                {
                    var bGo = new GameObject(chunk.Building.ToString());
                    bGo.hideFlags = HideFlags.DontSave;
                    bGo.transform.SetParent(chunkGo.transform, false);
                    bGo.transform.localPosition = new Vector3(w * 0.5f, 0f, 0f);
                    var sr = bGo.AddComponent<SpriteRenderer>();
                    sr.sprite = sprite;
                    sr.flipX = chunk.Flipped;
                    sr.sortingOrder = 1;
                    art.ApplyPalette(sr, WorldArt.PaletteRow(layout.Biome, chunk.Accent));
                }
            }
            return chunkGo;
        }

        static void AddBox(GameObject go, int x0, int x1, int y0, int y1)
        {
            var col = go.AddComponent<BoxCollider2D>();
            col.size = new Vector2(x1 - x0, y1 - y0);
            col.offset = new Vector2((x0 + x1) * 0.5f, (y0 + y1) * 0.5f);
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
