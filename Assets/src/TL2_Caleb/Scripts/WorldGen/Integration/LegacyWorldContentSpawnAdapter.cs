using System;
using System.Collections.Generic;
using System.Reflection;
using EscapeThe90s.Contracts;
using UnityEngine;

namespace EscapeThe90s.WorldGen.Integration
{
    /// <summary>Reflection bridge kept at the team integration boundary for existing TL3 and TL4 APIs.</summary>
    public sealed class LegacyWorldContentSpawnAdapter : IWorldContentSpawnAdapter
    {
        readonly MonoBehaviour _owner;
        readonly WorldArt _art;
        readonly Transform _target;
        readonly GameObject[] _enemyPrefabs;
        readonly HashSet<MonoBehaviour> _previousPowerUps = new HashSet<MonoBehaviour>();
        Component _enemySpawner;
        Component _powerUpManager;
        MethodInfo _createEnemy;
        MethodInfo _spawnPowerUp;
        Type _enemyKindType;
        Type _powerUpType;

        public LegacyWorldContentSpawnAdapter(MonoBehaviour owner, WorldArt art, Transform target, GameObject[] enemyPrefabs)
        {
            _owner = owner;
            _art = art;
            _target = target;
            _enemyPrefabs = enemyPrefabs;
            SetupEnemySpawner();
            SetupPowerUpManager();
        }

        public GameObject Spawn(SpawnLocation location, Vector3 worldPosition, int biome)
        {
            if (location.Kind == SpawnLocationKind.Enemy) return SpawnEnemy(location.ContentKindIndex, worldPosition);
            if (location.Kind == SpawnLocationKind.PowerUp) return SpawnPowerUp(location.ContentKindIndex, worldPosition);
            if (location.Kind == SpawnLocationKind.BuildingAnchor) return SpawnBuilding(location, worldPosition, biome);
            return null;
        }

        GameObject SpawnBuilding(SpawnLocation location, Vector3 groundPosition, int biome)
        {
            if (_art == null) return null;
            BuildingType type = (BuildingType)location.ContentKindIndex;
            Sprite sprite = _art.GetBuildingSprite(type, 0);
            if (sprite == null) return null;

            var building = new GameObject("World Building " + type);
            building.transform.position = groundPosition + Vector3.up * sprite.bounds.extents.y;
            SpriteRenderer renderer = building.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.sortingOrder = -1;
            _art.ApplyPalette(renderer, WorldArt.PaletteRow(biome, location.SlotIndex));
            return building;
        }

        GameObject SpawnEnemy(int kind, Vector3 position)
        {
            if (_enemySpawner == null || _createEnemy == null || _enemyPrefabs == null ||
                kind < 0 || kind >= _enemyPrefabs.Length || _enemyPrefabs[kind] == null) return null;
            object result = _createEnemy.Invoke(_enemySpawner,
                new[] { Enum.ToObject(_enemyKindType, kind), (object)position });
            GameObject enemy = result as GameObject;
            if (enemy == null && result is Component) enemy = ((Component)result).gameObject;
            if (enemy == null) return null;
            SetEnemyTarget(enemy);
            return enemy;
        }

        GameObject SpawnPowerUp(int kind, Vector3 position)
        {
            if (_powerUpManager == null || _spawnPowerUp == null) return null;
            _previousPowerUps.Clear();
            MonoBehaviour[] before = GeneratedWorldComposition.FindSceneBehaviours(_owner.gameObject.scene);
            for (int i = 0; i < before.Length; i++)
                if (before[i] != null && before[i].GetType().Name == "PowerUpItem") _previousPowerUps.Add(before[i]);
            _spawnPowerUp.Invoke(_powerUpManager,
                new[] { (object)position, Enum.ToObject(_powerUpType, kind) });
            MonoBehaviour[] after = GeneratedWorldComposition.FindSceneBehaviours(_owner.gameObject.scene);
            for (int i = 0; i < after.Length; i++)
            {
                MonoBehaviour item = after[i];
                if (item == null || item.GetType().Name != "PowerUpItem" || _previousPowerUps.Contains(item) ||
                    (item.transform.position - position).sqrMagnitude > 0.04f) continue;
                foreach (SpriteRenderer renderer in item.GetComponentsInChildren<SpriteRenderer>(true)) renderer.sortingOrder = 6;
                return item.gameObject;
            }
            return null;
        }

        void SetupEnemySpawner()
        {
            Type spawnerType = null;
            MonoBehaviour[] components = GeneratedWorldComposition.FindAllBehaviours();
            for (int i = 0; i < components.Length; i++)
                if (components[i] != null && components[i].GetType().Name == "EnemySpawner")
                { _enemySpawner = components[i]; spawnerType = components[i].GetType(); break; }
            if (spawnerType == null) spawnerType = Type.GetType("EnemySpawner, TL4Assembly", false);
            if (spawnerType == null || _enemyPrefabs == null) return;
            if (_enemySpawner == null) _enemySpawner = _owner.GetComponent(spawnerType);
            if (_enemySpawner == null) _enemySpawner = _owner.gameObject.AddComponent(spawnerType);

            FieldInfo prefabField = spawnerType.GetField("prefabs", BindingFlags.Instance | BindingFlags.NonPublic);
            if (prefabField != null && prefabField.FieldType.IsArray)
            {
                Type baseType = prefabField.FieldType.GetElementType();
                Array prefabs = Array.CreateInstance(baseType, _enemyPrefabs.Length);
                for (int i = 0; i < _enemyPrefabs.Length; i++)
                {
                    if (_enemyPrefabs[i] == null) continue;
                    Component prefab = _enemyPrefabs[i].GetComponent(baseType);
                    if (prefab != null) prefabs.SetValue(prefab, i);
                }
                prefabField.SetValue(_enemySpawner, prefabs);
            }
            MethodInfo rate = spawnerType.GetMethod("SetSpawnRate", BindingFlags.Instance | BindingFlags.Public);
            if (rate != null) rate.Invoke(_enemySpawner, new object[] { 0f });
            MethodInfo[] methods = spawnerType.GetMethods(BindingFlags.Instance | BindingFlags.Public);
            for (int i = 0; i < methods.Length; i++)
            {
                ParameterInfo[] parameters = methods[i].GetParameters();
                if (methods[i].Name == "CreateEnemy" && parameters.Length == 2 && parameters[1].ParameterType == typeof(Vector3))
                { _createEnemy = methods[i]; _enemyKindType = parameters[0].ParameterType; break; }
            }
        }

        void SetupPowerUpManager()
        {
            MonoBehaviour[] components = GeneratedWorldComposition.FindSceneBehaviours(_owner.gameObject.scene);
            for (int i = 0; i < components.Length; i++)
            {
                MonoBehaviour component = components[i];
                if (component == null || component.GetType().Name != "PowerUpManager" ||
                    component.GetType().Namespace != "EscapeThe90s.PowerUps") continue;
                _powerUpManager = component;
                MethodInfo[] methods = component.GetType().GetMethods(BindingFlags.Instance | BindingFlags.Public);
                for (int j = 0; j < methods.Length; j++)
                {
                    ParameterInfo[] parameters = methods[j].GetParameters();
                    if (methods[j].Name == "SpawnPowerUp" && parameters.Length == 2 && parameters[0].ParameterType == typeof(Vector3))
                    { _spawnPowerUp = methods[j]; _powerUpType = parameters[1].ParameterType; return; }
                }
            }
        }

        void SetEnemyTarget(GameObject enemy)
        {
            MonoBehaviour[] components = enemy.GetComponents<MonoBehaviour>();
            for (int i = 0; i < components.Length; i++)
            {
                if (components[i] == null) continue;
                MethodInfo method = components[i].GetType().GetMethod("SetTarget", BindingFlags.Instance | BindingFlags.Public,
                    null, new[] { typeof(Transform) }, null);
                if (method != null) { method.Invoke(components[i], new object[] { _target }); return; }
            }
        }
    }
}
