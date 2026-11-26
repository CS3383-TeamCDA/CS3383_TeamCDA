using System.Collections.Generic;
using EscapeThe90s.Contracts;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace EscapeThe90s.WorldGen
{
    /// <summary>Reusable Unity view for one non-uniform coordinate chunk.</summary>
    public sealed class CoordinateChunkView : MonoBehaviour
    {
        readonly List<Vector2> _outline = new List<Vector2>(WorldChunkCoord.Size * 2 + 3);
        const int Size = WorldChunkCoord.Size;
        static readonly BoundsInt TileBounds = new BoundsInt(0, 0, 0, Size, Size, 1);
        readonly int[] _filledColumns = new int[Size + 1];
        readonly TileBase[] _tiles = new TileBase[Size * Size];
        public WorldChunkKind FillKind { get; private set; }
        GroundChunk _ground;
        GapChunk _gap;
        RampChunk _ramp;
        public Chunk Behavior { get; private set; }
        Grid _grid;
        Transform _tileRoot;
        Tilemap _tilemap;
        TilemapRenderer _renderer;
        PolygonCollider2D _collider;
        public ulong ContentHash { get; private set; }

        void Awake()
        {
            _ground = gameObject.AddComponent<GroundChunk>();
            _gap = gameObject.AddComponent<GapChunk>();
            _ramp = gameObject.AddComponent<RampChunk>();
            _grid = GetComponent<Grid>();
            if (_grid == null) _grid = gameObject.AddComponent<Grid>();
            _grid.cellSize = Vector3.one;
            _tileRoot = transform.Find("Tilemap");
            if (_tileRoot == null)
            {
                var tileObject = new GameObject("Tilemap");
                tileObject.transform.SetParent(transform, false);
                tileObject.transform.localPosition = new Vector3(-0.5f, -0.5f, 0f);
                _tileRoot = tileObject.transform;
            }
            _tilemap = _tileRoot.GetComponent<Tilemap>();
            int groundLayer = LayerMask.NameToLayer("Ground");
            if (groundLayer >= 0) _tileRoot.gameObject.layer = groundLayer;
            if (_tilemap == null) _tilemap = _tileRoot.gameObject.AddComponent<Tilemap>();
            // The mask at (x,y) is centered between data cells x-1/x and y-1/y.
            // The collider root already supplies the matching half-cell offset.
            _tilemap.tileAnchor = Vector3.zero;
            _renderer = _tileRoot.GetComponent<TilemapRenderer>();
            if (_renderer == null) _renderer = _tileRoot.gameObject.AddComponent<TilemapRenderer>();
            _collider = _tileRoot.GetComponent<PolygonCollider2D>();
            if (_collider == null) _collider = _tileRoot.gameObject.AddComponent<PolygonCollider2D>();
            _renderer.sortingOrder = 0;
            ResetForPool();
        }

        public void Populate(WorldChunkData data, IWorldProfile profile, WorldArt art)
        {
            Behavior = data.TerrainKind == ChunkKind.Gap ? (Chunk)_gap : data.TerrainKind == ChunkKind.Ramp ? _ramp : _ground;
            Behavior.PopulateCoordinate(this, data, profile, art);
        }

        internal void RenderTerrain(WorldChunkData data, IWorldProfile profile, WorldArt art)
        {
            ContentHash = data.ContentHash;
            FillKind = data.Kind;
            int x0 = data.Coordinate.MinCellX;
            int y0 = data.Coordinate.MinCellY;
            transform.position = new Vector3(x0, y0, 0f);
            art.ApplyPalette(_renderer, WorldArt.PaletteRow(data.Biome, 0));

            // A render tile uses four adjacent occupancy bits. Cache the one-cell
            // border and use the snapshot for the interior of this pooled chunk.
            for (int x = -1; x < Size; x++)
            {
                int column = profile.IsFilled(x0 + x, y0 - 1) ? 1 : 0;
                for (int y = 0; y < Size; y++)
                {
                    bool ground = x < 0 ? profile.IsFilled(x0 - 1, y0 + y) :
                        data.GetTile(x, y) == TileId.Ground;
                    if (ground) column |= 1 << (y + 1);
                }
                _filledColumns[x + 1] = column;
            }
            for (int y = 0; y < Size; y++)
            {
                int bottom = 1 << y;
                int top = bottom << 1;
                for (int x = 0; x < Size; x++)
                {
                    int left = _filledColumns[x], right = _filledColumns[x + 1];
                    int mask = ((left & top) != 0 ? DualGrid.TL : 0) | ((right & top) != 0 ? DualGrid.TR : 0)
                        | ((left & bottom) != 0 ? DualGrid.BL : 0) | ((right & bottom) != 0 ? DualGrid.BR : 0);
                    _tiles[y * Size + x] = mask == 0 ? null : art.GetGroundTile(mask);
                }
            }
            _tilemap.SetTilesBlock(TileBounds, _tiles);
            BuildCollider(data, profile, x0, y0);
            _collider.enabled = _collider.pathCount > 0;
            gameObject.SetActive(true);
        }

        void BuildCollider(WorldChunkData data, IWorldProfile profile, int x0, int y0)
        {
            int bottomCell = Mathf.Max(y0, data.WorldFloorCellY + 1);
            float bottom = bottomCell - y0;
            _collider.pathCount = 0;
            int localX = 0;
            while (localX < WorldChunkCoord.Size)
            {
                if (!profile.IsFilled(x0 + localX, bottomCell)) { localX++; continue; }
                int start = localX;
                while (localX < WorldChunkCoord.Size && profile.IsFilled(x0 + localX, bottomCell)) localX++;
                int end = localX;
                _outline.Clear();
                _outline.Add(new Vector2(start, bottom));
                _outline.Add(new Vector2(end, bottom));
                float endTop = CoordinateTerrainGeometry.SurfaceTopAtEdge(profile, x0 + end) - y0 + 0.5f;
                AddOutlinePoint(new Vector2(end, Mathf.Clamp(endTop, bottom, WorldChunkCoord.Size)));
                for (int column = end - 1; column >= start; column--)
                {
                    float centerTop = CoordinateTerrainGeometry.SurfaceTopAtCell(profile, x0 + column) - y0 + 0.5f;
                    AddOutlinePoint(new Vector2(column + 0.5f, Mathf.Clamp(centerTop, bottom, WorldChunkCoord.Size)));
                    float edgeTop = CoordinateTerrainGeometry.SurfaceTopAtEdge(profile, x0 + column) - y0 + 0.5f;
                    AddOutlinePoint(new Vector2(column, Mathf.Clamp(edgeTop, bottom, WorldChunkCoord.Size)));
                }
                int path = _collider.pathCount;
                _collider.pathCount = path + 1;
                _collider.SetPath(path, _outline);
            }
        }

        void AddOutlinePoint(Vector2 point)
        {
            int count = _outline.Count;
            if (count >= 2)
            {
                Vector2 a = _outline[count - 1] - _outline[count - 2];
                Vector2 b = point - _outline[count - 1];
                if (Mathf.Abs(a.x * b.y - a.y * b.x) < 0.0001f)
                { _outline[count - 1] = point; return; }
            }
            _outline.Add(point);
        }

        public void ResetForPool()
        {
            if (_collider != null) _collider.enabled = false;
            gameObject.SetActive(false);
        }
    }
}
