using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace EscapeThe90s.WorldGen
{
    /// <summary>
    /// Phase 2 viewer: draws one generated street as coloured boxes in the Scene view.
    /// Add to an empty GameObject, tweak the fields, watch the street change. No art needed.
    /// </summary>
    public class StreetPreview : MonoBehaviour
    {
        public WorldGenSettings settings;
        public int nodeA = 0, nodeB = 1;
        [Range(6, 60)] public int chunkCount = 24;
        public int biome = 0;
        public BuildingFlags allowed = BuildingFlags.All;
        public int difficultyTier = 0;
        [Range(0, 3)] public int requiredArcades = 1;
        public float unitsPerCell = 0.5f;

        static Color ColorFor(BuildingType t)
        {
            switch (t)
            {
                case BuildingType.Generic: return new Color(0.55f, 0.6f, 0.7f);
                case BuildingType.PizzaParlor: return new Color(0.95f, 0.45f, 0.2f);
                case BuildingType.WeaponStore: return new Color(0.8f, 0.2f, 0.2f);
                case BuildingType.SubwayEntrance: return new Color(0.3f, 0.3f, 0.35f);
                case BuildingType.TowerEntrance: return new Color(0.6f, 0.4f, 0.8f);
                case BuildingType.Arcade: return new Color(0.2f, 0.85f, 0.9f);
                case BuildingType.Bank: return new Color(0.9f, 0.8f, 0.3f);
                default: return new Color(0.25f, 0.25f, 0.25f);
            }
        }

        void OnDrawGizmos()
        {
            BiomeRules[] all = settings != null ? settings.GetRules() : null;
            ulong seed = settings != null ? settings.SeedValue : 12345UL;
            BiomeRules rules = all != null ? all[((biome % all.Length) + all.Length) % all.Length]
                                           : BiomeRules.CreateDefault("Default");

            BuildingType[] required = null;
            if (requiredArcades > 0)
            {
                required = new BuildingType[requiredArcades];
                for (int i = 0; i < required.Length; i++) required[i] = BuildingType.Arcade;
            }
            var req = new StreetRequest
            {
                Id = StableId.ForStreet(new Int2(nodeA, 0), new Int2(nodeB, 1)),
                ChunkCount = chunkCount, Biome = biome, Allowed = allowed,
                Required = required, DifficultyTier = difficultyTier
            };
            StreetLayout layout = StreetGenerator.Generate(seed, req, rules);

            float chunkW = WorldConstants.ChunkWidthCells * unitsPerCell;
            float buildingH = 8f * unitsPerCell * 2f;
            Vector3 origin = transform.position;

            for (int i = 0; i < layout.ChunkCount; i++)
            {
                ChunkData c = layout.Chunks[i];
                Vector3 chunkMin = origin + new Vector3(i * chunkW, 0f, 0f);

                Gizmos.color = c.IsSafe ? new Color(0.4f, 0.4f, 0.4f, 0.5f) : new Color(0.2f, 0.2f, 0.2f, 0.35f);
                Gizmos.DrawWireCube(chunkMin + new Vector3(chunkW * 0.5f, buildingH * 0.5f, 0f), new Vector3(chunkW, buildingH, 0f));

                if (c.Building != BuildingType.None)
                {
                    Color col = ColorFor(c.Building);
                    Gizmos.color = new Color(col.r, col.g, col.b, 0.8f);
                    Vector3 size = new Vector3(chunkW * 0.8f, buildingH, 0.1f);
                    Gizmos.DrawCube(chunkMin + new Vector3(chunkW * 0.5f, buildingH * 0.5f, 0f), size);
#if UNITY_EDITOR
                    Handles.Label(chunkMin + new Vector3(chunkW * 0.1f, buildingH + 0.3f, 0f),
                                  c.Building + (c.Flipped ? " (flip)" : "") + "\nv" + c.Variant + " accent " + c.Accent);
#endif
                }

                Gizmos.color = Color.red;
                for (int e = 0; e < c.Enemies.Length; e++)
                {
                    EnemySlot s = c.Enemies[e];
                    float y = s.OnBuilding ? buildingH + 0.3f : 0.3f;
                    Gizmos.DrawSphere(chunkMin + new Vector3(s.X * chunkW, y, 0f), 0.25f);
                }
            }

            Gizmos.color = Color.white;
            Gizmos.DrawLine(origin, origin + new Vector3(layout.ChunkCount * chunkW, 0f, 0f));
            if (layout.DroppedRequired > 0)
            {
#if UNITY_EDITOR
                Handles.Label(origin + new Vector3(0, -1f, 0), "WARNING: " + layout.DroppedRequired + " required building(s) did not fit");
#endif
            }
        }
    }
}
