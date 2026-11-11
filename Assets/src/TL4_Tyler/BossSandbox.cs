using System;
using EscapeThe90s.Contracts;
using EscapeThe90s.Infrastructure.Unity;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>TL4-only prototype harness; never add this local state provider to FinalGame.</summary>
public sealed class BossSandbox : MonoBehaviour
{
    [SerializeField] private BossEnemy bossPrefab;
    private BossEncounterController encounter;
    private SandboxState state;
    private Rigidbody2D playerBody;
    private PlayerHealth health;
    private Sprite square;
    private string result = "";

    private void Start()
    {
        square = Sprite.Create(Texture2D.whiteTexture, new Rect(0f, 0f, Texture2D.whiteTexture.width,
            Texture2D.whiteTexture.height), new Vector2(0.5f, 0.5f), Texture2D.whiteTexture.width);
        Camera camera = Camera.main;
        if (camera == null)
        {
            GameObject cameraObject = new GameObject("Sandbox Camera");
            cameraObject.transform.SetParent(transform);
            cameraObject.tag = "MainCamera";
            camera = cameraObject.AddComponent<Camera>();
        }
        camera.transform.position = new Vector3(0f, 3f, -10f);
        camera.orthographic = true;
        camera.orthographicSize = 6f;
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(0.04f, 0.03f, 0.08f);
        GameObject floor = CreateBlock("Ground", new Vector2(0f, -0.5f), new Vector2(40f, 1f), Color.gray);
        floor.layer = LayerMask.NameToLayer("Ground");
        floor.AddComponent<BoxCollider2D>();
        GameObject target = CreateBlock("Sandbox Player", new Vector2(-5f, 1f), new Vector2(0.7f, 1.2f), Color.cyan);
        target.tag = "Player";
        target.AddComponent<BoxCollider2D>();
        playerBody = target.AddComponent<Rigidbody2D>();
        playerBody.constraints = RigidbodyConstraints2D.FreezeRotation;
        health = target.AddComponent<PlayerHealth>();
        state = new SandboxState();
        health.OnPlayerDied += state.RequestGameOver;
        DependencyScope scope = gameObject.AddComponent<DependencyScope>();
        scope.Register<IGameState>(state);
        GameObject service = new GameObject("Boss Encounter");
        service.transform.SetParent(transform);
        encounter = service.AddComponent<BossEncounterController>();
        encounter.Configure(bossPrefab, target.transform);
        encounter.OnEncounterEnded += HandleEnded;
        scope.Inject(service);
        Restart();
    }

    private GameObject CreateBlock(string label, Vector2 position, Vector2 size, Color color)
    {
        GameObject block = new GameObject(label);
        block.transform.SetParent(transform);
        block.transform.position = position;
        block.transform.localScale = size;
        SpriteRenderer renderer = block.AddComponent<SpriteRenderer>();
        renderer.sprite = square;
        renderer.color = color;
        return block;
    }

    private void Update()
    {
        Keyboard keyboard = Keyboard.current;
        if (keyboard == null || state == null)
        {
            return;
        }
        if (keyboard.rKey.wasPressedThisFrame)
        {
            Restart();
        }
        if (keyboard.escapeKey.wasPressedThisFrame)
        {
            if (state.CurrentStateType == GameStateType.Paused)
            {
                state.RequestResume();
            }
            else
            {
                state.RequestPause();
            }
        }
        if (state.CurrentStateType != GameStateType.Playing)
        {
            return;
        }
        float direction = (keyboard.dKey.isPressed ? 1f : 0f) - (keyboard.aKey.isPressed ? 1f : 0f);
        playerBody.linearVelocity = new Vector2(direction * 6f, playerBody.linearVelocity.y);
        if (keyboard.spaceKey.wasPressedThisFrame && Physics2D.Raycast(playerBody.position, Vector2.down,
            0.7f, 1 << LayerMask.NameToLayer("Ground")).collider != null)
        {
            playerBody.linearVelocity = new Vector2(playerBody.linearVelocity.x, 9f);
        }
        if (keyboard.jKey.wasPressedThisFrame)
        {
            BossEnemy boss = encounter.GetComponentInChildren<BossEnemy>();
            if (boss != null)
            {
                boss.TakeDamage(50);
            }
        }
    }

    private void Restart()
    {
        encounter.Cancel();
        health.ResetHealth();
        playerBody.position = new Vector2(-5f, 1f);
        playerBody.linearVelocity = Vector2.zero;
        state.NewRun();
        result = encounter.TryStart(new BossEncounterRequest(1UL, new WorldPosition2D(5f, 1f), 1, 7))
            ? "Boss active" : "Could not start: assign PrototypeBoss.";
    }

    private void HandleEnded(BossEncounterResult ended)
    {
        result = ended.Outcome + " | coins: " + ended.CoinReward + " | score: " + ended.ScoreValue;
    }

    private void OnGUI()
    {
        if (state == null)
        {
            return;
        }
        GUI.Label(new Rect(15f, 15f, 900f, 30f), "TL4 BOSS SANDBOX | A/D move | Space jump | J deal 50 debug damage | Esc pause | R restart");
        GUI.Label(new Rect(15f, 45f, 900f, 30f), state.CurrentStateType + " | " + result + " | Player HP: " + health.CurrentHealth);
        BossEnemy boss = encounter.GetComponentInChildren<BossEnemy>();
        if (boss != null)
        {
            GUI.Label(new Rect(15f, 75f, 900f, 30f), "Boss HP: " + boss.CurrentHealth + "/" + boss.MaxHealth +
                (boss.IsEnraged ? " | ENRAGED" : " | Phase 1"));
        }
    }

    private void OnDestroy()
    {
        Time.timeScale = 1f;
        if (square != null)
        {
            Destroy(square);
        }
    }

    private sealed class SandboxState : IGameState
    {
        public GameStateType CurrentStateType { get; private set; } = GameStateType.Menu;
        public event Action<GameStateChange> OnStateTypeChanged;
        private void Change(GameStateType next, bool newRun = false)
        {
            GameStateType previous = CurrentStateType;
            CurrentStateType = next;
            Time.timeScale = next == GameStateType.Playing ? 1f : 0f;
            OnStateTypeChanged?.Invoke(new GameStateChange(previous, next, newRun));
        }
        public void NewRun() { Change(GameStateType.Playing, true); }
        public void RequestStart() { NewRun(); }
        public void RequestPause() { if (CurrentStateType == GameStateType.Playing) { Change(GameStateType.Paused); } }
        public void RequestResume() { if (CurrentStateType == GameStateType.Paused) { Change(GameStateType.Playing); } }
        public void RequestGameOver() { Change(GameStateType.GameOver); }
        public void RequestRestart() { NewRun(); }
        public void RequestReturnToMenu() { Change(GameStateType.Menu); }
    }
}
