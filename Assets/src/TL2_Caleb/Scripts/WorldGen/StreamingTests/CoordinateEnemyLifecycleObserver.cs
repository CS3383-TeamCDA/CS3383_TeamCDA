#if UNITY_EDITOR
using System.Collections.Generic;
using EscapeThe90s.Contracts;
using UnityEngine;

namespace EscapeThe90s.WorldGen.Tests
{
    // The scene composition initializes in Update at -1000. Subscribe after composition,
    // before the streamer's first LateUpdate, so the initial spawn events are observed.
    [DefaultExecutionOrder(-500)]
    public sealed class CoordinateEnemyLifecycleObserver : MonoBehaviour
    {
        private readonly List<EnemyBase> _pendingEnemies = new List<EnemyBase>(16);
        private readonly Dictionary<ChunkId, ChunkSpawnedInfo> _spawned = new Dictionary<ChunkId, ChunkSpawnedInfo>(64);
        private readonly Dictionary<ChunkId, List<EnemyBase>> _owned = new Dictionary<ChunkId, List<EnemyBase>>(16);
        private readonly HashSet<ChunkId> _recycled = new HashSet<ChunkId>();
        private CoordinateWorldStreamer _streamer;
        private EnemySpawner _spawner;
        private ILevelProgress _progress;

        public void Initialize(CoordinateWorldStreamer streamer, EnemySpawner spawner)
        {
            _streamer = streamer;
            _spawner = spawner;
            _spawner.OnEnemySpawned += OnEnemySpawned;
        }

        public bool TryGetSpawn(ChunkId id, out ChunkSpawnedInfo info) => _spawned.TryGetValue(id, out info);
        public bool WasRecycled(ChunkId id) => _recycled.Contains(id);
        public IReadOnlyList<EnemyBase> GetOwnedEnemies(ChunkId id) => _owned[id];

        private void Update()
        {
            if (_progress != null || _streamer == null || _streamer.Progress == null) return;
            _progress = _streamer.Progress;
            _progress.OnChunkSpawned += OnChunkSpawned;
            _progress.OnChunkRecycled += OnChunkRecycled;
        }

        private void OnEnemySpawned(EnemyBase enemy)
        {
            if (enemy != null) _pendingEnemies.Add(enemy);
        }

        private void OnChunkSpawned(ChunkSpawnedInfo info)
        {
            _spawned.Add(info.Id, info);
            var enemies = new List<EnemyBase>(1);
            _owned.Add(info.Id, enemies);
            // Content consumes this same event before this observer. Associate the spawn
            // notification by its public type and exact published world-cell anchor.
            for (int i = 0; i < info.SpawnLocations.Count; i++)
            {
                SpawnLocation spawn = info.SpawnLocations[i];
                if (spawn.Kind != SpawnLocationKind.Enemy) continue;
                float expectedX = info.Bounds.MinX + spawn.NormalizedX * WorldChunkCoord.Size;
                for (int j = _pendingEnemies.Count - 1; j >= 0; j--)
                {
                    EnemyBase enemy = _pendingEnemies[j];
                    if (enemy == null || (int)enemy.Type != spawn.ContentKindIndex ||
                        Mathf.Abs(enemy.transform.position.x - expectedX) > 0.001f) continue;
                    enemies.Add(enemy);
                    _pendingEnemies.RemoveAt(j);
                }
            }
        }

        private void OnChunkRecycled(ChunkId id) => _recycled.Add(id);

        private void OnDisable()
        {
            if (_spawner != null) _spawner.OnEnemySpawned -= OnEnemySpawned;
            if (_progress != null)
            {
                _progress.OnChunkSpawned -= OnChunkSpawned;
                _progress.OnChunkRecycled -= OnChunkRecycled;
            }
            _progress = null;
        }
    }
}
#endif
