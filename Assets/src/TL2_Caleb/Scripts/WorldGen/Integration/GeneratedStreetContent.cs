using System;
using System.Collections.Generic;
using System.Reflection;
using EscapeThe90s.Contracts;
using EscapeThe90s.WorldGen;
using UnityEngine;

namespace EscapeThe90s.WorldGen.Integration
{
    /// <summary>Composes enemy and pickup systems with generated street chunk lifecycle events.</summary>
    public sealed class GeneratedStreetContent : MonoBehaviour, IDisposable
    {
        readonly Dictionary<int, List<GameObject>> _enemiesByChunk = new Dictionary<int, List<GameObject>>();
        readonly Dictionary<int, List<GameObject>> _pickupsByChunk = new Dictionary<int, List<GameObject>>();
        StreetView _streetView;
        WorldStreet _street;
        WorldArt _art;
        Transform _target;
        GameObject[] _enemyPrefabs;
        Component _enemySpawner;
        Component _powerUpManager;
        MethodInfo _spawnPowerUp;
        Type _powerUpType;
        ulong _streetId;
        bool _reversed;

        public void Initialize(StreetView streetView, WorldArt art, Transform target, GameObject[] enemyPrefabs)
        {
            _streetView = streetView;
            _art = art;
            _target = target;
            _enemyPrefabs = enemyPrefabs;
            SetupEnemySpawner();
            SetupPowerUpManager();
        }

        public void BindStreet(WorldStreet street, bool reversed)
        {
            Unbind();
            _street = street;
            _reversed = reversed;
            _streetId = street == null ? 0UL : street.Request.Id.Value;
            _enemiesByChunk.Clear();
            _pickupsByChunk.Clear();
            if (_streetView == null || street == null) return;
            EnsureAllEnemyKinds(street.Layout);
            _streetView.Progress.OnChunkSpawned += OnChunkSpawned;
            _streetView.Progress.OnChunkRecycled += OnChunkRecycled;
        }

        void OnChunkSpawned(ChunkSpawnedInfo info)
        {
            if (_street == null || info == null || info.Id.StreetId != _streetId ||
                info.Id.Index < 0 || info.Id.Index >= _street.Layout.Chunks.Length) return;
            ChunkData chunk = _street.Layout.Chunks[info.Id.Index];
            SpawnEnemies(chunk);
            SpawnPickup(chunk);
        }

        void OnChunkRecycled(ChunkId id)
        {
            if (id.StreetId != _streetId) return;
            // StreetView destroys registered objects immediately after publishing recycle.
            _enemiesByChunk.Remove(id.Index);
            _pickupsByChunk.Remove(id.Index);
        }

        void SetupEnemySpawner()
        {
            Type spawnerType = null;
            foreach (MonoBehaviour component in GeneratedWorldComposition.FindAllBehaviours())
                if (component != null && component.GetType().Name == "EnemySpawner")
                { _enemySpawner = component; spawnerType = component.GetType(); break; }
            if (spawnerType == null) spawnerType = Type.GetType("EnemySpawner, TL4Assembly", false);
            if (spawnerType == null || _enemyPrefabs == null || _enemyPrefabs.Length == 0) return;
            bool hasPrefab = false;
            foreach (GameObject prefab in _enemyPrefabs) if (prefab != null) { hasPrefab = true; break; }
            if (!hasPrefab) return;
            if (_enemySpawner == null) _enemySpawner = GetComponent(spawnerType);
            if (_enemySpawner == null)
                foreach (MonoBehaviour component in GeneratedWorldComposition.FindAllBehaviours())
                    if (component != null && component.GetType() == spawnerType) { _enemySpawner = component; break; }
            if (_enemySpawner == null) _enemySpawner = gameObject.AddComponent(spawnerType);

            FieldInfo prefabField = spawnerType.GetField("prefabs", BindingFlags.Instance | BindingFlags.NonPublic);
            if (prefabField != null && prefabField.FieldType.IsArray)
            {
                Type baseType = prefabField.FieldType.GetElementType();
                Array prefabs = Array.CreateInstance(baseType, _enemyPrefabs.Length);
                for (int i = 0; i < _enemyPrefabs.Length; i++)
                {
                    GameObject prefab = _enemyPrefabs[i];
                    if (prefab == null) continue;
                    Component enemyBase = prefab.GetComponent(baseType);
                    if (enemyBase != null) prefabs.SetValue(enemyBase, i);
                }
                prefabField.SetValue(_enemySpawner, prefabs);
            }
            MethodInfo configure = spawnerType.GetMethod("ConfigureSpawnPoints", BindingFlags.Instance | BindingFlags.Public);
            if (configure != null) configure.Invoke(_enemySpawner, new object[] { Array.CreateInstance(typeof(Transform), 0), 8 });
            InvokeSpawner("SetSpawnRate", 0f);
        }

        void SetupPowerUpManager()
        {
            Type managerType = null;
            foreach (MonoBehaviour component in GeneratedWorldComposition.FindSceneBehaviours(gameObject.scene))
                if (component.GetType().Name == "PowerUpManager" && component.GetType().Namespace == "EscapeThe90s.PowerUps")
                { _powerUpManager = component; managerType = component.GetType(); break; }
            if (managerType == null) return;
            foreach (MethodInfo method in managerType.GetMethods(BindingFlags.Instance | BindingFlags.Public))
            {
                ParameterInfo[] parameters = method.GetParameters();
                if (method.Name != "SpawnPowerUp" || parameters.Length != 2 || parameters[0].ParameterType != typeof(Vector3)) continue;
                _spawnPowerUp = method;
                _powerUpType = parameters[1].ParameterType;
                FieldInfo despawnDistance = managerType.GetField("despawnDistanceBehind", BindingFlags.Instance | BindingFlags.NonPublic);
                if (despawnDistance != null && despawnDistance.FieldType == typeof(float))
                    despawnDistance.SetValue(_powerUpManager, 100000f);
                return;
            }
        }

        void SpawnPickup(ChunkData chunk)
        {
            var spawned = new List<GameObject>();
            _pickupsByChunk[chunk.Index] = spawned;
            if (_powerUpManager == null || _spawnPowerUp == null || chunk.IsSafe ||
                ((_street.Request.Id.Value + (ulong)chunk.Index) & 1UL) == 0UL) return;
            ulong hash = _street.Request.Id.Value ^ ((ulong)(uint)chunk.Index * 0x9E3779B97F4A7C15UL);
            int typeIndex = (int)(hash % 3UL);
            int width = WorldConstants.ChunkWidthCells;
            float fraction = 0.3f + (float)((hash >> 8) % 4000UL) / 10000f;
            float localX = chunk.Index * width + fraction * width;
            float streetWidth = _street.Layout.ChunkCount * width;
            float x = _reversed ? streetWidth - localX : localX;
            float y = 1.25f;
            if (chunk.Building != BuildingType.None)
            {
                Sprite sprite = _art.GetBuildingSprite(chunk.Building, chunk.Variant);
                if (sprite != null) y = sprite.bounds.max.y + 0.75f;
            }
            Vector3 position = new Vector3(x, y, 0f);
            var before = new HashSet<MonoBehaviour>();
            foreach (MonoBehaviour component in GeneratedWorldComposition.FindSceneBehaviours(gameObject.scene))
                if (component.GetType().Name == "PowerUpItem") before.Add(component);
            _spawnPowerUp.Invoke(_powerUpManager, new[] { (object)position, Enum.ToObject(_powerUpType, typeIndex) });
            foreach (MonoBehaviour component in GeneratedWorldComposition.FindSceneBehaviours(gameObject.scene))
            {
                if (component.GetType().Name != "PowerUpItem" || before.Contains(component) ||
                    (component.transform.position - position).sqrMagnitude > 0.04f) continue;
                PropertyInfo typeProperty = component.GetType().GetProperty("Type");
                if (typeProperty == null || Convert.ToInt32(typeProperty.GetValue(component, null)) != typeIndex) continue;
                foreach (SpriteRenderer renderer in component.GetComponentsInChildren<SpriteRenderer>(true)) renderer.sortingOrder = 6;
                if (_streetView.RegisterChunkObject(chunk.Index, component.gameObject)) spawned.Add(component.gameObject);
                else Destroy(component.gameObject);
            }
        }

        void SpawnEnemies(ChunkData chunk)
        {
            var spawned = new List<GameObject>();
            _enemiesByChunk[chunk.Index] = spawned;
            if (_enemySpawner == null || _enemyPrefabs == null || _enemyPrefabs.Length == 0 || chunk.Enemies == null) return;
            InvokeSpawner("SetSpawnLevel", Mathf.Max(1, _street.Request.DifficultyTier + 1));
            MethodInfo create = null;
            foreach (MethodInfo method in _enemySpawner.GetType().GetMethods(BindingFlags.Instance | BindingFlags.Public))
            {
                ParameterInfo[] parameters = method.GetParameters();
                if (method.Name == "CreateEnemy" && parameters.Length == 2 && parameters[1].ParameterType == typeof(Vector3))
                { create = method; break; }
            }
            if (create == null) return;
            Type enemyType = create.GetParameters()[0].ParameterType;
            int width = WorldConstants.ChunkWidthCells;
            float streetWidth = _street.Layout.ChunkCount * width;
            float buildingHeight = 0f;
            if (chunk.Building != BuildingType.None)
            {
                Sprite sprite = _art.GetBuildingSprite(chunk.Building, chunk.Variant);
                if (sprite != null) buildingHeight = sprite.bounds.max.y;
            }
            foreach (EnemySlot slot in chunk.Enemies)
            {
                int prefabIndex = Mathf.Abs(slot.KindIndex) % _enemyPrefabs.Length;
                if (_enemyPrefabs[prefabIndex] == null) continue;
                float localX = chunk.Index * width + slot.X * width;
                float x = _reversed ? streetWidth - localX : localX;
                float y = slot.OnBuilding ? buildingHeight + 0.2f : 0.35f;
                if (prefabIndex == 1) y = Mathf.Max(y, 3f);
                object result = create.Invoke(_enemySpawner, new object[] { Enum.ToObject(enemyType, prefabIndex), new Vector3(x, y, 0f) });
                GameObject enemy = result as GameObject;
                if (enemy == null && result is Component) enemy = ((Component)result).gameObject;
                if (enemy == null) continue;
                SetEnemyTarget(enemy, _target);
                if (!_streetView.RegisterChunkObject(chunk.Index, enemy)) { Destroy(enemy); continue; }
                spawned.Add(enemy);
            }
        }

        static void EnsureAllEnemyKinds(StreetLayout layout)
        {
            int nextKind = 0;
            foreach (ChunkData chunk in layout.Chunks)
            {
                if (chunk.Enemies == null) continue;
                for (int i = 0; i < chunk.Enemies.Length; i++)
                {
                    EnemySlot slot = chunk.Enemies[i];
                    slot.KindIndex = nextKind++ % 3; // Ground, Flying, Turret.
                    chunk.Enemies[i] = slot;
                }
            }
        }

        void InvokeSpawner(string methodName, object value)
        {
            if (_enemySpawner == null) return;
            MethodInfo method = _enemySpawner.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.Public);
            if (method != null) method.Invoke(_enemySpawner, new[] { value });
        }

        static void SetEnemyTarget(GameObject enemy, Transform target)
        {
            foreach (MonoBehaviour component in enemy.GetComponents<MonoBehaviour>())
            {
                if (component == null) continue;
                MethodInfo method = component.GetType().GetMethod("SetTarget", BindingFlags.Instance | BindingFlags.Public,
                    null, new[] { typeof(Transform) }, null);
                if (method != null) { method.Invoke(component, new object[] { target }); return; }
            }
        }

        public void Dispose()
        {
            Unbind();
            foreach (List<GameObject> objects in _enemiesByChunk.Values)
                foreach (GameObject instance in objects) if (instance != null) Destroy(instance);
            foreach (List<GameObject> objects in _pickupsByChunk.Values)
                foreach (GameObject instance in objects) if (instance != null) Destroy(instance);
            _enemiesByChunk.Clear();
            _pickupsByChunk.Clear();
        }

        void Unbind()
        {
            if (_streetView == null) return;
            _streetView.Progress.OnChunkSpawned -= OnChunkSpawned;
            _streetView.Progress.OnChunkRecycled -= OnChunkRecycled;
        }

        void OnDestroy() { Dispose(); }
    }
}
