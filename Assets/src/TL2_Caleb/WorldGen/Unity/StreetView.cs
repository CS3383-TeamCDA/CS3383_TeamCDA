using UnityEngine;
using UnityEngine.Tilemaps;

namespace EscapeThe90s.WorldGen
{
    /// <summary>
    /// Phase 3: builds one generated street out of real tiles and sprites (one Tilemap + building sprite per chunk).
    /// Right-click the component header and choose "Rebuild", or just press Play.
    /// The generated objects are not saved with the scene (HideFlags.DontSave).
    /// Phase 4 replaces "build everything" with load/unload around the player.
    /// </summary>
    public class StreetView : MonoBehaviour
    {
        public WorldGenSettings settings;
        public WorldArt art;
        public int nodeA = 0, nodeB = 1;
        [Range(6, 60)] public int chunkCount = 24;
        public int biome = 0;
        public BuildingFlags allowed = BuildingFlags.All;
        public int difficultyTier = 0;
        [Range(0, 3)] public int requiredArcades = 1;
        public bool curbsAndSteps = true;

        GameObject _root;

        void Start() { if (Application.isPlaying) Rebuild(); }

        [ContextMenu("Rebuild")]
        public void Rebuild()
        {
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
            StreetLayout layout = level.GetStreet(request);
            var terrain = new StreetTerrain(settings.SeedValue, layout, curbsAndSteps);

            _root = new GameObject("Generated Street");
            _root.hideFlags = HideFlags.DontSave;
            _root.transform.SetParent(transform, false);
            var gridGo = new GameObject("Grid");
            gridGo.hideFlags = HideFlags.DontSave;
            gridGo.transform.SetParent(_root.transform, false);
            var grid = gridGo.AddComponent<Grid>();
            grid.cellSize = new Vector3(1f, 1f, 0f);   // 1 cell = 1 unit; set sprite PPU to 16 so a cell is 16 px

            for (int i = 0; i < layout.ChunkCount; i++)
                BuildChunk(gridGo.transform, layout, terrain, layout.Chunks[i], i == layout.ChunkCount - 1);
        }

        void BuildChunk(Transform parent, StreetLayout layout, StreetTerrain terrain, ChunkData chunk, bool last)
        {
            int w = WorldConstants.ChunkWidthCells;
            int x0 = chunk.Index * w;

            var chunkGo = new GameObject("Chunk " + chunk.Index);
            chunkGo.hideFlags = HideFlags.DontSave;
            chunkGo.transform.SetParent(parent, false);
            chunkGo.transform.localPosition = new Vector3(x0, 0f, 0f);

            // Ground: render tiles are centred on cell corners, so the tilemap is shifted half a cell.
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

            // Building, centred in the chunk, standing on the street surface (sprite pivot = bottom centre).
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
        }

        void Clear()
        {
            if (_root == null)
            {
                var existing = transform.Find("Generated Street");
                if (existing != null) _root = existing.gameObject;
            }
            if (_root == null) return;
            if (Application.isPlaying) Destroy(_root); else DestroyImmediate(_root);
            _root = null;
        }

        void OnDisable() { if (!Application.isPlaying) Clear(); }
    }
}
