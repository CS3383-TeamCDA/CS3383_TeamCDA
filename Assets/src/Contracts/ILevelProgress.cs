using System;

namespace EscapeThe90s.Contracts
{
    /// <summary>Shared read and event contract for level progress and streamed chunk lifecycle.</summary>
    public interface ILevelProgress
    {
        float GetCurrentDistance();
        float GetCurrentDifficulty();
        void SetDifficulty(float difficulty);
        event Action<ChunkSpawnedInfo> OnChunkSpawned;
        event Action<ChunkId> OnChunkRecycled;
    }
}
