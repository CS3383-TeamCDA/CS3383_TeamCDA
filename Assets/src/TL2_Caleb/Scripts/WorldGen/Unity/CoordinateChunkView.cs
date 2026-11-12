using UnityEngine;
using UnityEngine.Tilemaps;

namespace EscapeThe90s.WorldGen
{
    /// <summary>Reusable Unity view for one non-uniform coordinate chunk.</summary>
    public sealed class CoordinateChunkView : MonoBehaviour
    {
        readonly Vector2[] _outline = new Vector2[WorldChunkCoord.Size * 2 + 2];
        Grid _grid;
        Transform _tileRoot;
        Tilemap _tilemap;
        TilemapRenderer _renderer;
        PolygonCollider2D _collider;

        void Awake()
        {
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
            if (_tilemap == null) _tilemap = _tileRoot.gameObject.AddComponent<Tilemap>();
            _renderer = _tileRoot.GetComponent<TilemapRenderer>();
            if (_renderer == null) _renderer = _tileRoot.gameObject.AddComponent<TilemapRenderer>();
            _collider = _tileRoot.GetComponent<PolygonCollider2D>();
            if (_collider == null) _collider = _tileRoot.gameObject.AddComponent<PolygonCollider2D>();
            _renderer.sortingOrder = 0;
            ResetForPool();
        }

        public void Populate(WorldChunkData data, IWorldProfile profile, WorldArt art)
        {
            _tilemap.ClearAllTiles();
            WorldChunkCoord coordinate = data.Coordinate;
            int x0 = coordinate.MinCellX;
            int y0 = coordinate.MinCellY;
            transform.position = new Vector3(x0, y0, 0f);
            gameObject.name = "World Chunk " + coordinate;
            gameObject.SetActive(true);
            art.ApplyPalette(_renderer, WorldArt.PaletteRow(data.Biome, 0));

            for (int localY = 0; localY < WorldChunkCoord.Size; localY++)
                for (int localX = 0; localX < WorldChunkCoord.Size; localX++)
                {
                    int worldX = x0 + localX;
                    int worldY = y0 + localY;
                    int mask = DualGrid.MaskAt(profile, worldX, worldY);
                    if (mask != 0) _tilemap.SetTile(new Vector3Int(localX, localY, 0), art.GetGroundTile(mask));
                }

            BuildCollider(data, profile, x0, y0);
        }

        void BuildCollider(WorldChunkData data, IWorldProfile profile, int x0, int y0)
        {
            int bottomCell = Mathf.Max(y0, data.WorldFloorCellY + 1);
            float bottom = bottomCell - y0;
            _outline[0] = new Vector2(0f, bottom);
            _outline[1] = new Vector2(WorldChunkCoord.Size, bottom);
            int index = 2;
            int y1 = data.Coordinate.MaxCellY;
            for (int localX = WorldChunkCoord.Size - 1; localX >= 0; localX--)
            {
                int surface = Mathf.Min(profile.GetSurfaceHeight(x0 + localX), y1);
                float top = surface < bottomCell ? bottom : surface - y0 + 1f;
                _outline[index++] = new Vector2(localX + 1f, top);
                _outline[index++] = new Vector2(localX, top);
            }
            _collider.pathCount = 1;
            _collider.SetPath(0, _outline);
        }

        public void ResetForPool()
        {
            if (_tilemap != null) _tilemap.ClearAllTiles();
            if (_collider != null) _collider.pathCount = 0;
            gameObject.SetActive(false);
        }
    }
}
