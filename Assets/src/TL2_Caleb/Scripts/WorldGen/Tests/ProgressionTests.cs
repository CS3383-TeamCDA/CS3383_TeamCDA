using System;
using EscapeThe90s.WorldGen;
using NUnit.Framework;

namespace EscapeThe90s.WorldGen.Tests
{
    public class ProgressionTests
    {
        [Test, Category("M6")]
        public void DifficultyProgression_IsSmoothAndCanBeRebased()
        {
            var progression = new DifficultyProgression();
            Assert.AreEqual(0.5f, progression.DifficultyAt(100f), 0.0001f);
            progression.Rebase(100f, 3.25f);
            Assert.AreEqual(3.25f, progression.DifficultyAt(100f), 0.0001f);
            Assert.AreEqual(4.25f, progression.DifficultyAt(300f), 0.0001f);
        }

        [Test, Category("M6")]
        public void SetDifficulty_ClampsInvalidValues()
        {
            var progress = new LevelProgress();
            progress.AdvanceDistance(120f);
            progress.SetDifficulty(float.NaN);
            Assert.AreEqual(0f, progress.GetCurrentDifficulty());
            progress.SetDifficulty(float.PositiveInfinity);
            Assert.AreEqual(DifficultyProgression.DefaultMaxDifficulty, progress.GetCurrentDifficulty());
            progress.SetDifficulty(15f);
            Assert.AreEqual(DifficultyProgression.DefaultMaxDifficulty, progress.GetCurrentDifficulty());
        }

        [Test, Category("M6")]
        public void ProgressAndChunkLifecycle_AreMonotonicAndFireOnce()
        {
            var progress = new LevelProgress();
            progress.AdvanceDistance(120f);
            progress.AdvanceDistance(40f);
            Assert.AreEqual(120f, progress.GetCurrentDistance());

            int spawned = 0, recycled = 0;
            progress.OnChunkSpawned += _ => spawned++;
            progress.OnChunkRecycled += _ => recycled++;
            var id = new ChunkId(new StableId(12), 3);
            var info = new ChunkSpawnedInfo(id, new ChunkBounds(0f, 0f, 32f, 8f), 0.6f, 0, 1,
                ChunkKind.Ground, new SpawnLocation[0]);

            Assert.IsTrue(progress.NotifyChunkSpawned(info));
            Assert.IsFalse(progress.NotifyChunkSpawned(info));
            Assert.IsTrue(progress.NotifyChunkRecycled(id));
            Assert.IsFalse(progress.NotifyChunkRecycled(id));
            Assert.AreEqual(1, spawned);
            Assert.AreEqual(1, recycled);
        }

        [Test, Category("M6")]
        public void ChunkSpawnedInfo_ExposesReadOnlyLocations()
        {
            var location = new SpawnLocation(SpawnLocationKind.Enemy, 0.5f, 0, 1, -1);
            var info = new ChunkSpawnedInfo(new ChunkId(new StableId(7), 0),
                new ChunkBounds(0f, 0f, 32f, 8f), 0.25f, 0, 0, ChunkKind.Ground,
                new[] { location });
            var list = info.SpawnLocations as System.Collections.Generic.IList<SpawnLocation>;
            Assert.IsNotNull(list);
            Assert.Throws<NotSupportedException>(() => list[0] = new SpawnLocation(SpawnLocationKind.PowerUp, 0.2f, 0, 0, -1));
        }
    }
}
