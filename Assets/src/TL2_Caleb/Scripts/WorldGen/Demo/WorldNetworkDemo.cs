using System.Collections.Generic;
using EscapeThe90s.WorldGen;
using UnityEngine;
using UnityEngine.InputSystem;

namespace EscapeThe90s.WorldGen.Demo
{
/// <summary>Playable Phase 10 demo: walk generated streets, reach intersections, and choose where to go next.</summary>
[DefaultExecutionOrder(-1000)]
public class WorldNetworkDemo : MonoBehaviour
{
    public WorldGenSettings settings;
    public WorldArt art;
    public GameObject playerPrefab;
    [Tooltip("Input Actions asset supplied by the game integration for the player prefab.")]
    public InputActionAsset playerInputActions;
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
    readonly StreetIntersectionController _intersectionController = new StreetIntersectionController();
    bool _demoInitialized;
    float _entryX;
    float _destinationX;
    bool _mapReady;
    bool _ownsPlayer;
    EscapeThe90s.WorldGen.Integration.GeneratedStreetContent _generatedContent;
    string _message = "";
    readonly List<StreetEdge> _mapEdges = new List<StreetEdge>();
    readonly List<Int2> _mapArcades = new List<Int2>();
    readonly List<Int2> _mapStations = new List<Int2>();
    readonly RouteChooserView _routeChooser = new RouteChooserView();
    readonly WorldMapOverlay _mapOverlay = new WorldMapOverlay();
    readonly DemoCameraRig _cameraRig = new DemoCameraRig();


    void Awake()
    {
        _streetView = streetView != null ? streetView : GetComponentInChildren<StreetView>();
        if (_streetView != null) _streetView.enabled = false;
        if (player != null)
        {
            // Keep the scene player from awakening with the stale input-asset reference
            // serialized on the team prefab. Configure it through the demo integration first.
            player.gameObject.SetActive(false);
            ConfigurePlayerInput(player);
        }
    }

    void Start() { TryInitializeAfterGameStarts(); }

    void TryInitializeAfterGameStarts()
    {
        if (_demoInitialized || !EscapeThe90s.WorldGen.Integration.GeneratedWorldComposition.IsGameplayStarted()) return;
        if (settings == null || art == null)
        {
            Debug.LogWarning("WorldNetworkDemo: assign World Gen Settings and World Art. Use Escape the 90s > Create World Network Demo to set up the scene.");
            return;
        }
        _demoInitialized = true;
        DemoWorldBootstrap bootstrap = DemoWorldBootstrap.Create(settings, new Int2(startNodeX, startNodeY));
        _world = bootstrap.World;
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
        CreatePlayer();
        _generatedContent = GetComponent<EscapeThe90s.WorldGen.Integration.GeneratedStreetContent>();
        if (_generatedContent == null) _generatedContent = gameObject.AddComponent<EscapeThe90s.WorldGen.Integration.GeneratedStreetContent>();
        _generatedContent.Initialize(_streetView, art, _player, enemyPrefabsByKind);

        Int2 start = bootstrap.StartNode;
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
        string prompt = _intersectionController.UpdateStreetPrompt(_world, _currentStreet, _player,
            _entryNode, _destinationNode, _entryX, _destinationX);
        if (prompt != null) _message = prompt;
        if (!_intersectionController.IsAtIntersection) return;

        _intersectionController.SelectBy(ReadVerticalPress());
        if (ConfirmPressed() && _intersectionController.Exits.Length > 0)
            TravelExit(_intersectionController.ActiveIntersection,
                _intersectionController.Exits[_intersectionController.SelectedExit]);
    }

    void TravelExit(Int2 from, IntersectionExit exit)
    {
        if (_intersectionController.IsVirtualIntersection)
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
        _intersectionController.ShowVirtualIntersection(_world, station);
        BuildMap(station);
        _message = "Subway to " + station + ". Choose one of its graph exits.";
    }

    void CreatePlayer()
    {
        DemoPlayerRig rig = DemoPlayerRig.Create(transform, player, playerPrefab, demoPlayerSpeed);
        _player = rig.Player;
        _playerBody = rig.Body;
        _ownsPlayer = rig.OwnsPlayer;
        ConfigurePlayerInput(_player);
        _cameraFollow = _cameraRig.Attach(_player, _streetView);
        _streetView.follow = _player;
    }

    void ConfigurePlayerInput(Transform target)
    {
        if (target == null) return;
        PlayerInput input = target.GetComponent<PlayerInput>();
        if (input == null) input = target.gameObject.AddComponent<PlayerInput>();

        if (playerInputActions == null)
        {
            Debug.LogError("WorldNetworkDemo: Assign the InputSystem_Actions asset before starting the scene.", this);
            return;
        }

        input.actions = playerInputActions;
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
        _intersectionController.ResetForStreet();
        bool reverse = from.Equals(_currentStreet.Edge.B);
        float width = _currentStreet.Layout.ChunkCount * WorldConstants.ChunkWidthCells;
        _entryX = reverse ? width - 7f : 7f;
        _destinationX = reverse ? 7f : width - 7f;
        _player.position = new Vector3(_entryX, 3f, 0f);
        StopPlayer();
        // StreetView owns and destroys registered enemies when it rebuilds the old street.
        _streetView.follow = _player;
        _streetView.enabled = true;
        _generatedContent.BindStreet(_currentStreet, reverse);
        _streetView.ShowWorldStreet(_currentStreet, reverse);
        _mapCenter = from;
        BuildMap(from);
        _message = "Walk to the far end of the street to reach the next intersection.";
        if (_cameraFollow != null) _cameraFollow.street = _streetView;
        _player.gameObject.SetActive(true);
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
        if (_intersectionController.IsVirtualIntersection) return _mapCenter;
        if (_currentStreet == null || _player == null) return _mapCenter;
        float t = Mathf.InverseLerp(_entryX, _destinationX, _player.position.x);
        return new Int2(Mathf.RoundToInt(Mathf.Lerp(_entryNode.X, _destinationNode.X, t)),
                        Mathf.RoundToInt(Mathf.Lerp(_entryNode.Y, _destinationNode.Y, t)));
    }

    void OnGUI()
    {
        if (_world == null) return;
        _mapOverlay.Draw(_mapEdges, _mapArcades, _mapStations, _mapCenter, mapRadiusRegions, _currentStreet, FindMapPlayerPoint(), _mapReady);
        GUILayout.BeginArea(new Rect(12, 12, 365, 155), GUI.skin.box);
        GUILayout.Label("ESCAPE THE 90s  /  STREET NETWORK");
        if (_currentStreet != null)
        {
            BiomeRules rules = settings.GetRules()[_currentStreet.Layout.Biome];
            GUILayout.Label("Street biome: " + rules.Name + "    chunks: " + _currentStreet.Layout.ChunkCount);
            GUILayout.Label("Buildings: " + BuildingSummary(_currentStreet.Layout));
        }
        GUILayout.Label("Walk " + (_destinationX < _entryX ? "←" : "→") + " with A/D     Space jump");
        if (_intersectionController.IsAtIntersection)
            GUILayout.Label("At " + _intersectionController.ActiveIntersection + " — pick an exit in the route graph.");
        else if (!string.IsNullOrEmpty(_message)) GUILayout.Label(_message);
        GUILayout.EndArea();
        if (_intersectionController.IsAtIntersection) _intersectionController.Select(_routeChooser.Draw(_intersectionController.ActiveIntersection, _intersectionController.Exits, _intersectionController.SelectedExit));
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
        if (_generatedContent != null) _generatedContent.Dispose();
        if (_ownsPlayer && _player != null)
        {
            if (Application.isPlaying) Destroy(_player.gameObject);
            else DestroyImmediate(_player.gameObject);
        }
    }
}
}
