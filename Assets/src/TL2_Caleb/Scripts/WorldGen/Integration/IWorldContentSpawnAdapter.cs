using EscapeThe90s.Contracts;
using UnityEngine;

namespace EscapeThe90s.WorldGen.Integration
{
    /// <summary>Unity integration boundary for translating generated spawn instructions to legacy gameplay systems.</summary>
    public interface IWorldContentSpawnAdapter
    {
        /// <summary>Creates one generated content item at its world position using the owning biome.</summary>
        GameObject Spawn(SpawnLocation location, Vector3 worldPosition, int biome);
    }
}
