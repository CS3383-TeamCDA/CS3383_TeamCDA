using System.Collections.Generic;
using System;
using System.Reflection;
using EscapeThe90s.WorldGen;
using UnityEngine;

namespace EscapeThe90s.WorldGen.Demo
{
/// <summary>Playable Phase 10 demo: walk generated streets, reach intersections, and choose where to go next.</summary>
[DefaultExecutionOrder(-1000)]
public class WorldNetworkDemo : MonoBehaviour
{
    public WorldGenSettings settings;
    public WorldArt art;
    public GameObject playerPrefab;
    [Tooltip("TL4 enemy prefabs in EnemyType order: Ground, Flying, Turret.")]
    public GameObject[] enemyPrefabsByKind;
    public StreetView streetView;
    public Transform player;
    [Min(0f)] public float demoPlayerSpeed = 16f;
    [Range(1, 5)] public int mapRadiusRegions = 2;
    public int startNodeX;
    public int startNodeY;

    WorldNetwork _world;
    StreetView _streetView;
    Transform _player;
    Rigidbody2D _playerBody;
    CameraFollow _cameraFollow;
    WorldStreet _currentStreet;
    Int2 _entryNode;
    Int2 _destinationNode;
    Int2 _mapCenter;
    Int2 _activeIntersection;
    IntersectionExit[] _mapExits = new IntersectionExit[0];
    int _selectedExit;
    bool _demoInitialized;
    float _entryX;
    float _destinationX;
    bool _atIntersection;
    bool _mapReady;
    bool _ownsPlayer;
    bool _currentStreetReversed;
    bool _atVirtualIntersection;
    Component _enemySpawner;
    readonly Dictionary<int, List<GameObject>> _streetEnemiesByChunk = new Dictionary<int, List<GameObject>>();
    Component _powerUpManager;
    MethodInfo _spawnPowerUp;
    Type _powerUpType;
    readonly Dictionary<int, List<GameObject>> _streetPowerUpsByChunk = new Dictionary<int, List<GameObject>>();
    string _message = "";
    readonly List<StreetEdge> _mapEdges = new List<StreetEdge>();
    readonly List<Int2> _mapArcades = new List<Int2>();
    readonly List<Int2> _mapStations = new List<Int2>();

    void Awake()
    {
        _streetView = streetView != null ? streetView : GetComponentInChildren<StreetView>();
        if (_streetView != null) _streetView.enabled = false;
        if (player != null) player.gameObject.SetActive(false);
    }

    void Start() { TryInitializeAfterGameStarts(); }

    void TryInitializeAfterGameStarts()
    {
        if (_demoInitialized || !IsGameplayStarted()) return;
        if (settings == null || art == null)
        {
            Debug.LogWarning("WorldNetworkDemo: assign World Gen Settings and World Art. Use Escape the 90s > Create World Network Demo to set up the scene.");
            return;
        }
        _demoInitialized = true;
        _world = new WorldNetwork(settings.SeedValue, CreateDemoBiomeRules(),
            settings.minNetworkStreetChunks, settings.maxNetworkStreetChunks);
        _streetView = streetView != null ? streetView : GetComponentInChildren<StreetView>();
        if (_streetView == null)
        {
            var streetGo = new GameObject("Playable Street");
            streetGo.transform.SetParent(transform, false);
            _streetView = streetGo.AddComponent<StreetView>();
        }
        _streetView.settings = settings;
        _streetView.art = art;
        _streetView.setupCamera = false;
        CreatePlayerAndCamera();
        SetupPowerUpManager();

        Int2 start = new Int2(startNodeX, startNodeY);
        if (_world.GetIntersection(start).Length == 0) start = FindNearestStreetNode(start);
        BuildMap(start);
        IntersectionExit[] exits = _world.GetIntersection(start);
        if (exits.Length == 0)
        {
            _message = "No street exits at the selected start node.";
            return;
        }
        for (int i = 0; i < exits.Length; i++)
            if (!exits[i].IsSubway) { EnterStreet(start, exits[i].Destination); return; }
        TravelExit(start, exits[0]);
    }

    void Update()
    {
        if (!_demoInitialized)
        {
            TryInitializeAfterGameStarts();
            if (!_demoInitialized) return;
        }
        if (_world == null || _currentStreet == null || _player == null) return;
        UpdateIntersectionPrompt();
        SyncStreetContent();
        if (!_atIntersection) return;

        int direction = ReadVerticalPress();
        if (direction != 0 && _mapExits.Length > 0)
            _selectedExit = (_selectedExit + direction + _mapExits.Length) % _mapExits.Length;

        if (ConfirmPressed() && _mapExits.Length > 0)
        {
            TravelExit(_activeIntersection, _mapExits[_selectedExit]);
        }
    }

    bool IsGameplayStarted()
    {
        foreach (MonoBehaviour component in FindSceneMonoBehaviours())
        {
            if (component == null || component.GetType().Name != "GameStateManager") continue;
            PropertyInfo currentState = component.GetType().GetProperty("CurrentState", BindingFlags.Instance | BindingFlags.Public);
            object state = currentState == null ? null : currentState.GetValue(component, null);
            return state != null && state.GetType().Name == "PlayingState";
        }
        return true; // Standalone worldgen scenes without the team menu start immediately.
    }

    BiomeRules[] CreateDemoBiomeRules()
    {
        BiomeRules[] source = settings.GetRules();
        var demoRules = new BiomeRules[source.Length];
        for (int i = 0; i < source.Length; i++)
        {
            BiomeRules rule = source[i];
            var buildings = new BuildingRule[rule.Buildings == null ? 0 : rule.Buildings.Length];
            for (int b = 0; b < buildings.Length; b++)
            {
                BuildingRule old = rule.Buildings[b];
                buildings[b] = new BuildingRule
                {
                    Type = old.Type, FillWeight = old.FillWeight, PerStreetPercent = old.PerStreetPercent,
                    MaxPerStreet = old.MaxPerStreet, VariantCount = old.VariantCount,
                    EnemyAnchors = old.EnemyAnchors, AllowFlip = old.AllowFlip
                };
            }
            int minEnemies = Mathf.Max(2, rule.EnemiesMinPerChunk);
            demoRules[i] = new BiomeRules
            {
                Name = rule.Name, BuildingDensityPercent = rule.BuildingDensityPercent,
                AccentCount = rule.AccentCount, EnemiesMinPerChunk = minEnemies,
                EnemiesMaxPerChunk = Mathf.Max(minEnemies, rule.EnemiesMaxPerChunk),
                TiersPerExtraEnemy = rule.TiersPerExtraEnemy,
                EnemyHardCap = Mathf.Max(3, rule.EnemyHardCap),
                EnemyKindWeights = new[] { 1, 1, 1 }, Buildings = buildings
            };
        }
        return demoRules;
    }

    void TravelExit(Int2 from, IntersectionExit exit)
    {
        if (_atVirtualIntersection)
        {
            if (exit.IsSubway) TravelToStation(exit.Destination);
            else EnterStreet(from, exit.Destination);
            return;
        }
        if (!exit.IsSubway)
        {
            EnterStreet(from, exit.Destination);
            return;
        }

        TravelToStation(exit.Destination);
    }

    void TravelToStation(Int2 station)
    {
        _activeIntersection = station;
        _mapExits = _world.GetIntersection(station);
        _selectedExit = 0;
        _atVirtualIntersection = true;
        _atIntersection = _mapExits.Length > 0;
        BuildMap(station);
        _message = "Subway to " + station + ". Choose one of its graph exits.";
    }

    Int2 FindNearestStreetNode(Int2 origin)
    {
        Int2 center = _world.Regions.RegionAt(origin);
        Int2 best = origin;
        long bestDistance = long.MaxValue;
        for (int y = center.Y - 1; y <= center.Y + 1; y++)
            for (int x = center.X - 1; x <= center.X + 1; x++)
                foreach (Int2 node in _world.Regions.GetStreets(new Int2(x, y)).Nodes)
                {
                    long dx = (long)node.X - origin.X, dy = (long)node.Y - origin.Y;
                    long distance = dx * dx + dy * dy;
                    if (distance < bestDistance || (distance == bestDistance && node.CompareTo(best) < 0))
                    { best = node; bestDistance = distance; }
                }
        return best;
    }

    void CreatePlayerAndCamera()
    {
        GameObject playerObject;
        if (player != null) playerObject = player.gameObject;
        else if (playerPrefab != null)
        {
            playerObject = Instantiate(playerPrefab);
            playerObject.name = playerPrefab.name + " (World Network Demo)";
            _ownsPlayer = true;
        }
        else
        {
            playerObject = new GameObject("Demo Player");
            playerObject.transform.SetParent(transform, false);
            player = playerObject.transform;
            _ownsPlayer = true;
        }
        _playerBody = playerObject.GetComponent<Rigidbody2D>();
        bool addedBody = _playerBody == null;
        if (addedBody) _playerBody = playerObject.AddComponent<Rigidbody2D>();
        if (addedBody)
        {
            _playerBody.gravityScale = 3f;
            _playerBody.constraints = RigidbodyConstraints2D.FreezeRotation;
            _playerBody.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        }
        var capsule = playerObject.GetComponent<CapsuleCollider2D>();
        bool addedCapsule = capsule == null;
        if (addedCapsule) capsule = playerObject.AddComponent<CapsuleCollider2D>();
        DemoWalker walker = playerObject.GetComponent<DemoWalker>();
        bool teamPlayer = playerObject.GetComponent("TempPlayerController") != null;
        if (walker == null && !teamPlayer) walker = playerObject.AddComponent<DemoWalker>();
        if (walker != null)
        {
            walker.moveSpeed = 18f;
            capsule.size = new Vector2(0.75f, 1.25f);
            capsule.offset = new Vector2(0f, 0.625f);
            capsule.sharedMaterial = new PhysicsMaterial2D("WorldNetworkDemoNoFriction") { friction = 0f, bounciness = 0f };
        }
        if (teamPlayer) ConfigureTeamPlayer(playerObject, demoPlayerSpeed);
        playerObject.tag = "Player";
        AttachPowerUpTarget(playerObject);
        SpriteRenderer playerRenderer = playerObject.GetComponent<SpriteRenderer>();
        if (playerRenderer != null) playerRenderer.sortingOrder = 5;
        _player = playerObject.transform;
        InjectDynamicPlayerDependencies(playerObject);

        Camera camera = Camera.main;
        if (camera == null)
        {
            var cameraGo = new GameObject("Main Camera");
            cameraGo.tag = "MainCamera";
            camera = cameraGo.AddComponent<Camera>();
        }
        camera.orthographic = true;
        camera.orthographicSize = StreetView.DefaultOrthographicSize;
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(0.07f, 0.05f, 0.13f);
        // The existing Cinemachine rig has no bounds for generated streets. Use our
        // pixel-snapped follow camera so each generated route supplies its true extent.
        Component brain = camera.GetComponent("CinemachineBrain");
        if (brain is Behaviour) ((Behaviour)brain).enabled = false;
        _cameraFollow = camera.GetComponent<CameraFollow>();
        if (_cameraFollow == null) _cameraFollow = camera.gameObject.AddComponent<CameraFollow>();
        _cameraFollow.target = _player;
        _cameraFollow.street = _streetView;
        _streetView.follow = _player;
        SetupEnemySpawner();
    }

    static void InjectDynamicPlayerDependencies(GameObject playerObject)
    {
        foreach (MonoBehaviour component in FindSceneMonoBehaviours())
        {
            if (component == null || component.GetType().Name != "ServiceContainer") continue;
            MethodInfo inject = component.GetType().GetMethod("InjectDynamicObject", BindingFlags.Instance | BindingFlags.Public);
            if (inject != null) inject.Invoke(component, new object[] { playerObject });
            return;
        }
    }

    static void AttachPowerUpTarget(GameObject playerObject)
    {
        Type adapterType = Type.GetType(
            "EscapeThe90s.WorldGen.Integration.GeneratedPlayerPowerUpTarget, EscapeThe90s.WorldGen.Integration", false);
        if (adapterType == null)
        {
            Debug.LogWarning("WorldNetworkDemo: power-up adapter was not loaded; pickups will not affect player stats.");
            return;
        }
        if (playerObject.GetComponent(adapterType) == null) playerObject.AddComponent(adapterType);
    }

    static void BindTeamCinemachineTarget(Transform target)
    {
        foreach (MonoBehaviour component in FindSceneMonoBehaviours())
        {
            if (component == null || component.GetType().Name != "CameraTargetBinder") continue;
            FieldInfo cameraField = component.GetType().GetField("_cinemachineCamera", BindingFlags.Instance | BindingFlags.NonPublic);
            object virtualCamera = cameraField == null ? null : cameraField.GetValue(component);
            if (virtualCamera == null) continue;
            PropertyInfo follow = virtualCamera.GetType().GetProperty("Follow");
            if (follow != null && follow.CanWrite) follow.SetValue(virtualCamera, target, null);
        }
    }

    static MonoBehaviour[] FindSceneMonoBehaviours()
    {
#if UNITY_2023_1_OR_NEWER
        return FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include);
#else
        return FindObjectsOfType<MonoBehaviour>(true);
#endif
    }

    static void ConfigureTeamPlayer(GameObject playerObject, float moveSpeed)
    {
        Component controller = playerObject.GetComponent("TempPlayerController");
        if (controller == null) return;
        const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
        FieldInfo groundLayer = controller.GetType().GetField("_groundLayer", flags);
        if (groundLayer != null && groundLayer.FieldType == typeof(LayerMask))
        {
            int defaultLayer = LayerMask.NameToLayer("Default");
            LayerMask mask = 1 << (defaultLayer < 0 ? 0 : defaultLayer);
            groundLayer.SetValue(controller, mask);
        }
        FieldInfo speed = controller.GetType().GetField("_maxSpeedX", flags);
        if (speed != null && speed.FieldType == typeof(float) && moveSpeed > 0f)
            speed.SetValue(controller, moveSpeed);
    }

    void EnterStreet(Int2 from, Int2 to)
    {
        _currentStreet = _world.GetStreet(from, to);
        if (_currentStreet == null)
        {
            _message = "That intersection exit has no generated street.";
            return;
        }
        _entryNode = from;
        _destinationNode = to;
        _atVirtualIntersection = false;
        bool reverse = from.Equals(_currentStreet.Edge.B);
        _currentStreetReversed = reverse;
        EnsureAllDemoEnemyKinds(_currentStreet.Layout);
        float width = _currentStreet.Layout.ChunkCount * WorldConstants.ChunkWidthCells;
        _entryX = reverse ? width - 7f : 7f;
        _destinationX = reverse ? 7f : width - 7f;
        _player.position = new Vector3(_entryX, 3f, 0f);
        StopPlayer();
        // StreetView owns and destroys registered enemies when it rebuilds the old street.
        _streetEnemiesByChunk.Clear();
        _streetPowerUpsByChunk.Clear();
        _streetView.follow = _player;
        _streetView.enabled = true;
        _streetView.ShowWorldStreet(_currentStreet, reverse);
        _mapCenter = from;
        BuildMap(from);
        _atIntersection = false;
        _mapExits = new IntersectionExit[0];
        _message = "Walk to the far end of the street to reach the next intersection.";
        if (_cameraFollow != null) _cameraFollow.street = _streetView;
        _player.gameObject.SetActive(true);
        SyncStreetContent();
    }

    static void EnsureAllDemoEnemyKinds(StreetLayout layout)
    {
        int nextKind = 0;
        foreach (ChunkData chunk in layout.Chunks)
        {
            if (chunk.Enemies == null) continue;
            for (int i = 0; i < chunk.Enemies.Length; i++)
            {
                EnemySlot slot = chunk.Enemies[i];
                slot.KindIndex = nextKind++ % 3; // Ground, Flying, Turret
                chunk.Enemies[i] = slot;
            }
        }
    }

    void UpdateIntersectionPrompt()
    {
        if (_atVirtualIntersection)
        {
            _atIntersection = _mapExits.Length > 0;
            return;
        }
        float x = _player.position.x;
        float toEntry = Mathf.Abs(x - _entryX);
        float toDestination = Mathf.Abs(x - _destinationX);
        const float promptDistance = 7.5f;
        bool nearEntry = toEntry <= promptDistance;
        bool nearDestination = toDestination <= promptDistance;
        if (!nearEntry && !nearDestination)
        {
            _atIntersection = false;
            return;
        }

        bool atEntry = toEntry <= toDestination;
        Int2 node = atEntry ? _entryNode : _destinationNode;
        if (!_atIntersection || !_activeIntersection.Equals(node))
        {
            _activeIntersection = node;
            IntersectionExit[] allExits = _world.GetIntersection(node);
            var onwardRoutes = new List<IntersectionExit>(allExits.Length);
            foreach (IntersectionExit exit in allExits)
                if (exit.IsSubway || !exit.StreetId.Equals(_currentStreet.Request.Id))
                    onwardRoutes.Add(exit);
            _mapExits = onwardRoutes.ToArray();
            _selectedExit = 0;
            if (_mapExits.Length == 0)
                _message = "No other routes meet this end. Walk back along the street to return.";
            else
                _message = "Choose a route connected to this end of the street.";
        }
        _atIntersection = _mapExits.Length > 0;
    }

    void SetupEnemySpawner()
    {
        Type spawnerType = null;
        foreach (MonoBehaviour component in FindSceneMonoBehaviours())
            if (component != null && component.GetType().Name == "EnemySpawner")
            { _enemySpawner = component; spawnerType = component.GetType(); break; }
        if (spawnerType == null) spawnerType = Type.GetType("EnemySpawner, TL4Assembly", false);
        if (spawnerType == null || enemyPrefabsByKind == null || enemyPrefabsByKind.Length == 0) return;
        bool hasPrefab = false;
        foreach (GameObject prefab in enemyPrefabsByKind) if (prefab != null) { hasPrefab = true; break; }
        if (!hasPrefab) return;
        if (_enemySpawner == null) _enemySpawner = GetComponent(spawnerType);
        if (_enemySpawner == null)
            foreach (MonoBehaviour component in FindSceneMonoBehaviours())
                if (component != null && component.GetType() == spawnerType) { _enemySpawner = component; break; }
        if (_enemySpawner == null) _enemySpawner = gameObject.AddComponent(spawnerType);

        FieldInfo prefabField = spawnerType.GetField("prefabs", BindingFlags.Instance | BindingFlags.NonPublic);
        if (prefabField != null && prefabField.FieldType.IsArray)
        {
            Type baseType = prefabField.FieldType.GetElementType();
            Array prefabs = Array.CreateInstance(baseType, enemyPrefabsByKind.Length);
            for (int i = 0; i < enemyPrefabsByKind.Length; i++)
            {
                GameObject prefab = enemyPrefabsByKind[i];
                if (prefab == null) continue;
                Component enemyBase = prefab.GetComponent(baseType);
                if (enemyBase != null) prefabs.SetValue(enemyBase, i);
            }
            prefabField.SetValue(_enemySpawner, prefabs);
        }

        MethodInfo configurePoints = spawnerType.GetMethod("ConfigureSpawnPoints", BindingFlags.Instance | BindingFlags.Public);
        if (configurePoints != null)
            configurePoints.Invoke(_enemySpawner, new object[] { Array.CreateInstance(typeof(Transform), 0), 8 });
        InvokeSpawnerMethod("SetSpawnRate", 0f);
    }

    void SyncStreetContent()
    {
        SyncStreetEnemies();
        SyncStreetPowerUps();
    }

    void SyncStreetEnemies()
    {
        if (_enemySpawner == null || enemyPrefabsByKind == null || enemyPrefabsByKind.Length == 0 ||
            _currentStreet == null || _streetView == null || _streetView.Layout == null) return;
        foreach (ChunkData chunk in _currentStreet.Layout.Chunks)
        {
            if (!_streetView.IsChunkLoaded(chunk.Index)) continue;
            if (_streetEnemiesByChunk.ContainsKey(chunk.Index)) continue;
            SpawnEnemiesForChunk(chunk);
        }

        var unloaded = new List<int>();
        foreach (KeyValuePair<int, List<GameObject>> pair in _streetEnemiesByChunk)
            if (!_streetView.IsChunkLoaded(pair.Key)) unloaded.Add(pair.Key);
        foreach (int chunkIndex in unloaded) _streetEnemiesByChunk.Remove(chunkIndex);
    }

    void SetupPowerUpManager()
    {
        Type managerType = null;
        foreach (MonoBehaviour component in FindSceneMonoBehaviours())
            if (component != null && component.GetType().Name == "PowerUpManager" &&
                component.GetType().Namespace == "EscapeThe90s.PowerUps" && component.gameObject.scene == gameObject.scene)
            { _powerUpManager = component; managerType = component.GetType(); break; }
        if (managerType == null) return;

        foreach (MethodInfo method in managerType.GetMethods(BindingFlags.Instance | BindingFlags.Public))
        {
            ParameterInfo[] parameters = method.GetParameters();
            if (method.Name == "SpawnPowerUp" && parameters.Length == 2 && parameters[0].ParameterType == typeof(Vector3))
            {
                _spawnPowerUp = method;
                _powerUpType = parameters[1].ParameterType;
                FieldInfo despawnDistance = managerType.GetField("despawnDistanceBehind", BindingFlags.Instance | BindingFlags.NonPublic);
                if (despawnDistance != null && despawnDistance.FieldType == typeof(float))
                    despawnDistance.SetValue(_powerUpManager, 100000f);
                return;
            }
        }
    }

    void SyncStreetPowerUps()
    {
        if (_powerUpManager == null || _spawnPowerUp == null || _currentStreet == null ||
            _streetView == null || _streetView.Layout == null) return;
        foreach (ChunkData chunk in _currentStreet.Layout.Chunks)
        {
            if (!_streetView.IsChunkLoaded(chunk.Index) || _streetPowerUpsByChunk.ContainsKey(chunk.Index)) continue;
            SpawnPowerUpForChunk(chunk);
        }

        var unloaded = new List<int>();
        foreach (KeyValuePair<int, List<GameObject>> pair in _streetPowerUpsByChunk)
            if (!_streetView.IsChunkLoaded(pair.Key)) unloaded.Add(pair.Key);
        foreach (int chunkIndex in unloaded) _streetPowerUpsByChunk.Remove(chunkIndex);
    }

    void SpawnPowerUpForChunk(ChunkData chunk)
    {
        var spawned = new List<GameObject>();
        _streetPowerUpsByChunk[chunk.Index] = spawned;
        // Street-end chunks remain safe; place a deterministic pickup in about every other interior chunk.
        if (chunk.IsSafe || ((_currentStreet.Request.Id.Value + (ulong)chunk.Index) & 1UL) == 0UL) return;

        ulong hash = _currentStreet.Request.Id.Value ^ ((ulong)(uint)chunk.Index * 0x9E3779B97F4A7C15UL);
        int typeIndex = (int)(hash % 3UL);
        int width = WorldConstants.ChunkWidthCells;
        float fraction = 0.3f + (float)((hash >> 8) % 4000UL) / 10000f;
        float localX = chunk.Index * width + fraction * width;
        float streetWidth = _currentStreet.Layout.ChunkCount * width;
        float x = _currentStreetReversed ? streetWidth - localX : localX;
        float y = 1.25f;
        if (chunk.Building != BuildingType.None)
        {
            Sprite sprite = art.GetBuildingSprite(chunk.Building, chunk.Variant);
            if (sprite != null) y = sprite.bounds.max.y + 0.75f;
        }
        Vector3 position = new Vector3(x, y, 0f);

        var before = new HashSet<MonoBehaviour>();
        foreach (MonoBehaviour component in FindSceneMonoBehaviours())
            if (component != null && component.GetType().Name == "PowerUpItem" && component.gameObject.scene == gameObject.scene)
                before.Add(component);

        _spawnPowerUp.Invoke(_powerUpManager, new[] { (object)position, Enum.ToObject(_powerUpType, typeIndex) });

        foreach (MonoBehaviour component in FindSceneMonoBehaviours())
        {
            if (component == null || component.GetType().Name != "PowerUpItem" ||
                component.gameObject.scene != gameObject.scene || before.Contains(component)) continue;
            if ((component.transform.position - position).sqrMagnitude > 0.04f) continue;
            PropertyInfo typeProperty = component.GetType().GetProperty("Type");
            if (typeProperty == null || Convert.ToInt32(typeProperty.GetValue(component, null)) != typeIndex) continue;
            foreach (SpriteRenderer renderer in component.GetComponentsInChildren<SpriteRenderer>(true)) renderer.sortingOrder = 6;
            if (_streetView.RegisterChunkObject(chunk.Index, component.gameObject)) spawned.Add(component.gameObject);
            else Destroy(component.gameObject);
        }
    }

    void SpawnEnemiesForChunk(ChunkData chunk)
    {
        var spawned = new List<GameObject>();
        _streetEnemiesByChunk[chunk.Index] = spawned;
        if (_enemySpawner == null || enemyPrefabsByKind == null || enemyPrefabsByKind.Length == 0 || chunk.Enemies == null) return;
        InvokeSpawnerMethod("SetSpawnLevel", Mathf.Max(1, _currentStreet.Request.DifficultyTier + 1));
        Type spawnerType = _enemySpawner.GetType();
        MethodInfo create = null;
        foreach (MethodInfo method in spawnerType.GetMethods(BindingFlags.Instance | BindingFlags.Public))
        {
            ParameterInfo[] parameters = method.GetParameters();
            if (method.Name == "CreateEnemy" && parameters.Length == 2 && parameters[1].ParameterType == typeof(Vector3))
            { create = method; break; }
        }
        if (create == null) return;

        Type enemyType = create.GetParameters()[0].ParameterType;
        int width = WorldConstants.ChunkWidthCells;
        float streetWidth = _currentStreet.Layout.ChunkCount * width;
        float buildingHeight = 0f;
        if (chunk.Building != BuildingType.None)
        {
            Sprite sprite = art.GetBuildingSprite(chunk.Building, chunk.Variant);
            if (sprite != null) buildingHeight = sprite.bounds.max.y;
        }
        foreach (EnemySlot slot in chunk.Enemies)
        {
            int prefabIndex = Mathf.Abs(slot.KindIndex) % enemyPrefabsByKind.Length;
            if (enemyPrefabsByKind[prefabIndex] == null) continue;
            float localX = chunk.Index * width + slot.X * width;
            float x = _currentStreetReversed ? streetWidth - localX : localX;
            float y = slot.OnBuilding ? buildingHeight + 0.2f : 0.35f;
            if (prefabIndex == 1) y = Mathf.Max(y, 3f); // flying enemies begin visibly above the street
            Vector3 position = new Vector3(x, y, 0f);
            object result = create.Invoke(_enemySpawner, new object[] { Enum.ToObject(enemyType, prefabIndex), position });
            GameObject enemyObject = result as GameObject;
            if (enemyObject == null && result is Component) enemyObject = ((Component)result).gameObject;
            if (enemyObject == null) continue;
            SetEnemyTarget(enemyObject, _player);
            if (!_streetView.RegisterChunkObject(chunk.Index, enemyObject))
            {
                Destroy(enemyObject);
                continue;
            }
            spawned.Add(enemyObject);
        }
    }

    void ClearStreetEnemies()
    {
        var chunks = new List<int>(_streetEnemiesByChunk.Keys);
        foreach (int chunkIndex in chunks) DestroyChunkEnemies(chunkIndex);
    }

    void DestroyChunkEnemies(int chunkIndex)
    {
        List<GameObject> enemies;
        if (!_streetEnemiesByChunk.TryGetValue(chunkIndex, out enemies)) return;
        foreach (GameObject enemy in enemies)
            if (enemy != null) Destroy(enemy);
        _streetEnemiesByChunk.Remove(chunkIndex);
    }

    void InvokeSpawnerMethod(string methodName, object value)
    {
        MethodInfo method = _enemySpawner.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.Public);
        if (method != null) method.Invoke(_enemySpawner, new[] { value });
    }

    static void SetEnemyTarget(GameObject enemy, Transform target)
    {
        foreach (MonoBehaviour component in enemy.GetComponents<MonoBehaviour>())
        {
            if (component == null) continue;
            MethodInfo setTarget = component.GetType().GetMethod("SetTarget", BindingFlags.Instance | BindingFlags.Public,
                null, new[] { typeof(Transform) }, null);
            if (setTarget != null) { setTarget.Invoke(component, new object[] { target }); return; }
        }
    }

    void BuildMap(Int2 center)
    {
        _mapEdges.Clear(); _mapArcades.Clear(); _mapStations.Clear();
        Int2 centerRegion = _world.Regions.RegionAt(center);
        var edgeIds = new HashSet<StableId>();
        var arcades = new HashSet<Int2>();
        var stations = new HashSet<Int2>();
        for (int y = centerRegion.Y - mapRadiusRegions; y <= centerRegion.Y + mapRadiusRegions; y++)
            for (int x = centerRegion.X - mapRadiusRegions; x <= centerRegion.X + mapRadiusRegions; x++)
            {
                RegionStreets streets = _world.Regions.GetStreets(new Int2(x, y));
                foreach (StreetEdge edge in streets.Edges)
                    if (edgeIds.Add(edge.Id)) _mapEdges.Add(edge);
                foreach (Int2 node in streets.ArcadeNodes) arcades.Add(node);
                foreach (Int2 node in streets.SubwayNodes) stations.Add(node);
            }
        _mapArcades.AddRange(arcades);
        _mapStations.AddRange(stations);
        _mapCenter = center;
        _mapReady = true;
    }

    Int2 FindMapPlayerPoint()
    {
        if (_atVirtualIntersection) return _mapCenter;
        if (_currentStreet == null || _player == null) return _mapCenter;
        float t = Mathf.InverseLerp(_entryX, _destinationX, _player.position.x);
        return new Int2(Mathf.RoundToInt(Mathf.Lerp(_entryNode.X, _destinationNode.X, t)),
                        Mathf.RoundToInt(Mathf.Lerp(_entryNode.Y, _destinationNode.Y, t)));
    }

    void OnGUI()
    {
        if (_world == null) return;
        DrawMiniMap();
        GUILayout.BeginArea(new Rect(12, 12, 365, 155), GUI.skin.box);
        GUILayout.Label("ESCAPE THE 90s  /  STREET NETWORK");
        if (_currentStreet != null)
        {
            BiomeRules rules = settings.GetRules()[_currentStreet.Layout.Biome];
            GUILayout.Label("Street biome: " + rules.Name + "    chunks: " + _currentStreet.Layout.ChunkCount);
            GUILayout.Label("Buildings: " + BuildingSummary(_currentStreet.Layout));
        }
        GUILayout.Label("Walk " + (_destinationX < _entryX ? "←" : "→") + " with A/D     Space jump");
        if (_atIntersection)
            GUILayout.Label("At " + _activeIntersection + " — pick an exit in the route graph.");
        else if (!string.IsNullOrEmpty(_message)) GUILayout.Label(_message);
        GUILayout.EndArea();
        if (_atIntersection) DrawIntersectionChooser();
    }

    void DrawIntersectionChooser()
    {
        const float width = 380f, height = 270f;
        Rect panel = new Rect((Screen.width - width) * 0.5f, (Screen.height - height) * 0.5f, width, height);
        GUI.Box(panel, "");
        Rect local = new Rect(panel.x + 10f, panel.y + 8f, panel.width - 20f, panel.height - 16f);
        GUI.BeginGroup(local);
        GUI.Label(new Rect(4f, 0f, local.width - 8f, 24f), "CHOOSE A ROUTE  •  " + _activeIntersection);

        Vector2 center = new Vector2(92f, local.height * 0.55f);
        float radius = 56f;
        for (int i = 0; i < _mapExits.Length; i++)
        {
            Int2 delta = new Int2(_mapExits[i].Destination.X - _activeIntersection.X,
                                   _mapExits[i].Destination.Y - _activeIntersection.Y);
            Vector2 direction = new Vector2(delta.X, -delta.Y);
            if (direction.sqrMagnitude < 0.01f) direction = Vector2.up;
            direction.Normalize();
            int sameDirectionIndex = 0;
            for (int j = 0; j < i; j++)
            {
                Int2 previous = new Int2(_mapExits[j].Destination.X - _activeIntersection.X,
                                          _mapExits[j].Destination.Y - _activeIntersection.Y);
                Vector2 previousDirection = new Vector2(previous.X, -previous.Y);
                if (previousDirection.sqrMagnitude > 0.01f && Vector2.Dot(direction, previousDirection.normalized) > 0.98f)
                    sameDirectionIndex++;
            }
            float angleOffset = (sameDirectionIndex - 0.5f) * 0.16f;
            direction = Quaternion.Euler(0f, 0f, angleOffset * Mathf.Rad2Deg) * direction;
            Vector2 point = center + direction * radius;
            Color routeColor = i == _selectedExit ? new Color(1f, 0.83f, 0.34f) : new Color(0.55f, 0.68f, 0.83f);
            DrawGuiLine(center, point, routeColor, i == _selectedExit ? 4f : 2f);
            Rect nodeRect = new Rect(point.x - 22f, point.y - 16f, 44f, 32f);
            string label = (_mapExits[i].IsSubway ? "M" : "S") + (i + 1);
            Color old = GUI.backgroundColor;
            GUI.backgroundColor = i == _selectedExit ? new Color(0.45f, 0.33f, 0.12f) : new Color(0.15f, 0.18f, 0.28f);
            if (GUI.Button(nodeRect, label)) _selectedExit = i;
            GUI.backgroundColor = old;

            Rect row = new Rect(190f, 36f + i * 25f, local.width - 195f, 23f);
            string kind = _mapExits[i].IsSubway ? "Metro" : "Street";
            if (GUI.Button(row, label + "  " + kind + " → " + _mapExits[i].Destination)) _selectedExit = i;
        }

        GUI.Box(new Rect(center.x - 29f, center.y - 14f, 58f, 28f), "HERE");
        GUI.Label(new Rect(4f, local.height - 23f, local.width - 8f, 20f), "Click a route or use ↑ / ↓, then Enter");
        GUI.EndGroup();
    }

    void DrawMiniMap()
    {
        if (!_mapReady) return;
        Rect panel = new Rect(Screen.width - 250f, 12f, 238f, 238f);
        GUI.Box(panel, "WORLD MAP");
        Rect content = new Rect(panel.x + 9f, panel.y + 27f, panel.width - 18f, panel.height - 38f);
        GUI.BeginGroup(content);
        float diameter = (mapRadiusRegions * 2 + 1) * WorldConstants.RegionSizeNodes;
        float scale = Mathf.Min(content.width, content.height) / (diameter + 4f);
        Vector2 mapCenter = new Vector2(content.width * 0.5f, content.height * 0.5f);
        Color old = GUI.color;
        foreach (StreetEdge edge in _mapEdges)
        {
            Vector2 a = MapPoint(edge.A, mapCenter, scale), b = MapPoint(edge.B, mapCenter, scale);
            Color color = edge.External ? new Color(0.64f, 0.77f, 0.95f) : new Color(0.3f, 0.34f, 0.44f);
            if (_currentStreet != null && edge.Id.Equals(_currentStreet.Request.Id)) color = Color.white;
            DrawGuiLine(a, b, color, edge.External ? 2.2f : 1.5f);
        }
        foreach (Int2 station in _mapStations) DrawGuiDot(MapPoint(station, mapCenter, scale), 5f, new Color(0.2f, 0.95f, 0.9f));
        foreach (Int2 arcade in _mapArcades) DrawGuiDot(MapPoint(arcade, mapCenter, scale), 6f, new Color(1f, 0.58f, 0.25f));
        DrawGuiDot(MapPoint(FindMapPlayerPoint(), mapCenter, scale), 7f, Color.white);
        GUI.color = old;
        GUI.EndGroup();
    }

    Vector2 MapPoint(Int2 node, Vector2 center, float scale)
    { return new Vector2(center.x + (node.X - _mapCenter.X) * scale, center.y - (node.Y - _mapCenter.Y) * scale); }

    static void DrawGuiLine(Vector2 a, Vector2 b, Color color, float thickness)
    {
        Vector2 delta = b - a;
        Matrix4x4 old = GUI.matrix;
        Color oldColor = GUI.color;
        GUIUtility.RotateAroundPivot(Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg, a);
        GUI.color = color;
        GUI.DrawTexture(new Rect(a.x, a.y - thickness * 0.5f, delta.magnitude, thickness), Texture2D.whiteTexture);
        GUI.matrix = old;
        GUI.color = oldColor;
    }

    static void DrawGuiDot(Vector2 point, float size, Color color)
    {
        Color old = GUI.color;
        GUI.color = color;
        GUI.DrawTexture(new Rect(point.x - size * 0.5f, point.y - size * 0.5f, size, size), Texture2D.whiteTexture);
        GUI.color = old;
    }

    static string BuildingSummary(StreetLayout layout)
    {
        var counts = new Dictionary<BuildingType, int>();
        foreach (ChunkData chunk in layout.Chunks)
        {
            if (chunk.Building == BuildingType.None) continue;
            int count;
            counts.TryGetValue(chunk.Building, out count);
            counts[chunk.Building] = count + 1;
        }
        if (counts.Count == 0) return "none";
        var parts = new List<string>();
        foreach (KeyValuePair<BuildingType, int> pair in counts) parts.Add(pair.Key + " ×" + pair.Value);
        return string.Join(", ", parts.ToArray());
    }

    void StopPlayer()
    {
        if (_playerBody == null) return;
#if UNITY_6000_0_OR_NEWER
        _playerBody.linearVelocity = Vector2.zero;
#else
        _playerBody.velocity = Vector2.zero;
#endif
    }

    static int ReadVerticalPress()
    {
#if ENABLE_INPUT_SYSTEM
        var keyboard = UnityEngine.InputSystem.Keyboard.current;
        if (keyboard == null) return 0;
        if (keyboard.upArrowKey.wasPressedThisFrame || keyboard.wKey.wasPressedThisFrame) return -1;
        if (keyboard.downArrowKey.wasPressedThisFrame || keyboard.sKey.wasPressedThisFrame) return 1;
#else
        if (Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.W)) return -1;
        if (Input.GetKeyDown(KeyCode.DownArrow) || Input.GetKeyDown(KeyCode.S)) return 1;
#endif
        return 0;
    }

    static bool ConfirmPressed()
    {
#if ENABLE_INPUT_SYSTEM
        var keyboard = UnityEngine.InputSystem.Keyboard.current;
        return keyboard != null && (keyboard.enterKey.wasPressedThisFrame || keyboard.numpadEnterKey.wasPressedThisFrame || keyboard.eKey.wasPressedThisFrame);
#else
        return Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter) || Input.GetKeyDown(KeyCode.E);
#endif
    }

    void OnDestroy()
    {
        ClearStreetEnemies();
        foreach (List<GameObject> pickups in _streetPowerUpsByChunk.Values)
            foreach (GameObject pickup in pickups) if (pickup != null) Destroy(pickup);
        _streetPowerUpsByChunk.Clear();
        if (_ownsPlayer && _player != null)
        {
            if (Application.isPlaying) Destroy(_player.gameObject);
            else DestroyImmediate(_player.gameObject);
        }
    }
}
}
