using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using EscapeThe90s.Contracts;

namespace EscapeThe90s.WorldGen
{
    /// <summary>Reusable Unity chunk base; concrete kinds provide their runtime type through the pool.</summary>
    public abstract class Chunk : MonoBehaviour
    {
        const int InitialColliderCapacity = 36;

        Tilemap _tilemap;
        TilemapRenderer _tilemapRenderer;
        SpriteRenderer _buildingRenderer;
        readonly List<BoxCollider2D> _colliders = new List<BoxCollider2D>(InitialColliderCapacity);

        public abstract ChunkKind Kind { get; }

        public void Initialize()
        {
            if (_tilemap != null) return;
            var ground = new GameObject("Ground");
            ground.hideFlags = HideFlags.DontSave;
            ground.transform.SetParent(transform, false);
            ground.transform.localPosition = new Vector3(-0.5f, -0.5f, 0f);
            _tilemap = ground.AddComponent<Tilemap>();
            _tilemapRenderer = ground.AddComponent<TilemapRenderer>();
            _tilemapRenderer.sortingOrder = 0;

            var building = new GameObject("Building");
            building.hideFlags = HideFlags.DontSave;
            building.transform.SetParent(transform, false);
            _buildingRenderer = building.AddComponent<SpriteRenderer>();
            _buildingRenderer.sortingOrder = 1;
            EnsureColliderCapacity(InitialColliderCapacity);
            ResetForPool();
        }

        public void Populate(Transform parent, StreetLayout layout, StreetTerrain terrain, ChunkData content,
            ChunkSpec spec, WorldArt art, bool last)
        {
            Initialize();
            if (spec.Kind != Kind) throw new InvalidOperationException("Pooled chunk kind does not match its generated spec.");
            transform.SetParent(parent, false);
            transform.localPosition = new Vector3(content.Index * WorldConstants.ChunkWidthCells, 0f, 0f);
            gameObject.name = "Chunk " + content.Index + " (" + Kind + ")";
            gameObject.SetActive(true);

            int width = WorldConstants.ChunkWidthCells;
            int x0 = content.Index * width;
            int lastX = last ? width : width - 1;
            for (int lx = 0; lx <= lastX; lx++)
                for (int ry = -WorldConstants.GroundDepthCells; ry <= terrain.MaxHeight; ry++)
                {
                    int worldX = x0 + lx;
                    if (!CanRenderColumn(terrain, worldX)) continue;
                    int mask = DualGrid.MaskAt(terrain, worldX, ry);
                    if (mask != 0) _tilemap.SetTile(new Vector3Int(lx, ry, 0), art.GetGroundTile(mask));
                }
            art.ApplyPalette(_tilemapRenderer, WorldArt.PaletteRow(layout.Biome, 0));

            List<ColliderBox> boxes = TerrainColliders.ForChunk(terrain, content.Index);
            EnsureColliderCapacity(boxes.Count + 2);
            for (int i = 0; i < boxes.Count; i++) SetCollider(i, boxes[i]);
            int colliderIndex = boxes.Count;
            if (content.Index == 0) SetCollider(colliderIndex++, new ColliderBox(-1, 0, -WorldConstants.SlabDepthCells, 40));
            if (last) SetCollider(colliderIndex++, new ColliderBox(width, width + 1, -WorldConstants.SlabDepthCells, 40));
            for (int i = colliderIndex; i < _colliders.Count; i++) _colliders[i].enabled = false;

            if (content.Building != BuildingType.None)
            {
                _buildingRenderer.sprite = art.GetBuildingSprite(content.Building, content.Variant);
                _buildingRenderer.flipX = content.Flipped;
                _buildingRenderer.transform.localPosition = new Vector3(width * 0.5f, 0f, 0f);
                _buildingRenderer.enabled = _buildingRenderer.sprite != null;
                art.ApplyPalette(_buildingRenderer, WorldArt.PaletteRow(layout.Biome, content.Accent));
            }
        }

        protected virtual bool CanRenderColumn(StreetTerrain terrain, int worldX) { return true; }

        public void ResetForPool()
        {
            if (_tilemap != null) _tilemap.ClearAllTiles();
            if (_buildingRenderer != null)
            {
                _buildingRenderer.sprite = null;
                _buildingRenderer.enabled = false;
                _buildingRenderer.flipX = false;
            }
            for (int i = 0; i < _colliders.Count; i++) _colliders[i].enabled = false;
            gameObject.SetActive(false);
        }

        void SetCollider(int index, ColliderBox box)
        {
            BoxCollider2D collider = _colliders[index];
            collider.size = new Vector2(box.X1 - box.X0, box.Y1 - box.Y0);
            collider.offset = new Vector2((box.X0 + box.X1) * 0.5f, (box.Y0 + box.Y1) * 0.5f);
            collider.enabled = true;
        }

        void EnsureColliderCapacity(int count)
        {
            while (_colliders.Count < count) _colliders.Add(gameObject.AddComponent<BoxCollider2D>());
        }
    }
}
