using System;

namespace TL2_WorldData
{
    /// <summary>
    /// Yes/no behaviours of a tile TYPE. Chunks store tile IDs only, flags live in the registry,
    /// so widening or adding flags never touches chunk data, saves or hashes.
    /// Write each value as 1u << n so adding one is hard to get wrong.
    /// Numeric data (hardness, drops) belongs on the tile definition, not here.
    /// </summary>
    [Flags]
    public enum TileFlags : uint
    {
        None = 0,

        // ---- Movement (foreground only) ----
        Solid   = 1u << 0,   // blocks movement
        Lift    = 1u << 1,   // elevator beam: non-solid, vertically traversable

        // ---- Gameplay (foreground only) ----
        NoSpawn = 1u << 2,   // spawn planner must never place a mob here

        // ---- Generation hints ----
        // CoverAllowed = 1u << 3,
        // Future: Liquid, Hazard, Mineable, Slippery ...
    }

    public static class TileFlagMasks
    {
        /// <summary>Flags that only make sense on foreground tiles. The registry rejects them on background tiles.</summary>
        public const TileFlags Gameplay = TileFlags.Solid | TileFlags.Lift | TileFlags.NoSpawn;
    }
}