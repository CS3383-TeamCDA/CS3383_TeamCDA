using System.Collections.Generic;
using EscapeThe90s.Contracts;
using EscapeThe90s.WorldGen;
using UnityEngine;

namespace EscapeThe90s.WorldGen.Integration
{
    /// <summary>Consumes coordinate chunk contracts and scopes spawned objects to their owner chunk.</summary>
    public sealed class GeneratedCoordinateContent : MonoBehaviour, System.IDisposable
    {
        readonly Dictionary<ChunkId, List<GameObject>> _ownedObjects = new Dictionary<ChunkId, List<GameObject>>(128);
        readonly Stack<List<GameObject>> _instanceLists = new Stack<List<GameObject>>(128);
        CoordinateWorldStreamer _streamer;
        ILevelProgress _progress;
        IWorldContentSpawnAdapter _spawner;

        public void Initialize(CoordinateWorldStreamer streamer, WorldArt art, Transform target, GameObject[] enemyPrefabs)
        {
            Dispose();
            _streamer = streamer;
            _progress = streamer == null ? null : streamer.Progress;
            _spawner = new LegacyWorldContentSpawnAdapter(this, art, target, enemyPrefabs,
                streamer == null ? null : streamer.Profile);
            if (_progress == null) return;
            _progress.OnChunkSpawned += OnChunkSpawned;
            _progress.OnChunkRecycled += OnChunkRecycled;
        }

        void OnChunkSpawned(ChunkSpawnedInfo info)
        {
            if (info == null || !info.Id.IsWorldCoordinate || _ownedObjects.ContainsKey(info.Id)) return;
            if (info.SpawnLocations.Count == 0) return;
            List<GameObject> instances = _instanceLists.Count == 0 ? new List<GameObject>(3) : _instanceLists.Pop();
            _ownedObjects.Add(info.Id, instances);
            for (int i = 0; i < info.SpawnLocations.Count; i++)
            {
                SpawnLocation spawn = info.SpawnLocations[i];
                float x = info.Bounds.MinX + spawn.NormalizedX * WorldChunkCoord.Size;
                int cellX = Mathf.RoundToInt(x);
                int surfaceHeight = _streamer.SurfaceHeightAt(cellX);
                float y = spawn.Kind == SpawnLocationKind.BuildingAnchor
                    ? surfaceHeight + 0.5f
                    : surfaceHeight + 0.85f;
                if (spawn.Kind == SpawnLocationKind.Enemy && spawn.ContentKindIndex == 1) y += 3f;
                if (spawn.Kind == SpawnLocationKind.Enemy && spawn.ContentKindIndex == 2) y -= 0.25f;
                if (spawn.Kind == SpawnLocationKind.PowerUp) y += 0.9f;
                GameObject instance = _spawner.Spawn(spawn, new Vector3(x, y, 0f), info.Biome);
                if (instance != null) instances.Add(instance);
            }
        }

        void OnChunkRecycled(ChunkId id)
        {
            if (!_ownedObjects.TryGetValue(id, out List<GameObject> instances)) return;
            for (int i = 0; i < instances.Count; i++) if (instances[i] != null) Destroy(instances[i]);
            _ownedObjects.Remove(id);
            instances.Clear();
            _instanceLists.Push(instances);
        }

        public void Dispose()
        {
            if (_progress != null)
            {
                _progress.OnChunkSpawned -= OnChunkSpawned;
                _progress.OnChunkRecycled -= OnChunkRecycled;
            }
            foreach (List<GameObject> instances in _ownedObjects.Values)
                for (int i = 0; i < instances.Count; i++) if (instances[i] != null) Destroy(instances[i]);
            _ownedObjects.Clear();
            _instanceLists.Clear();
            _spawner = null;
            _progress = null;
            _streamer = null;
        }

        void OnDestroy() { Dispose(); }
    }
}
