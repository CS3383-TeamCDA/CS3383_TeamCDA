using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>Isolated TL4 test arena with a local crouch collider; does not modify TL6 movement.</summary>
public sealed class TurretBossSandbox : MonoBehaviour
{
    [SerializeField] private TurretBossController bossPrefab;
    [SerializeField] private TurretBossVisualRig visualPrefab;
    [SerializeField] private TurretBossVfxSet attackVfx;
    [SerializeField, Min(1)] private int bossLevel = 1;
    private GameObject arena;
    private TurretBossController boss;
    private Rigidbody2D playerBody;
    private BoxCollider2D playerCollider;
    private PlayerHealth health;
    private SpriteRenderer playerVisual;
    private Sprite square;
    private bool paused;
    private bool crouching;

    private void Start()
    {
        square = Sprite.Create(Texture2D.whiteTexture, new Rect(0f, 0f, Texture2D.whiteTexture.width,
            Texture2D.whiteTexture.height), new Vector2(0.5f, 0.5f), Texture2D.whiteTexture.width);
        Restart();
    }

    private GameObject Block(string label, Vector2 center, Vector2 size, Color color)
    {
        GameObject block = new GameObject(label);
        block.transform.SetParent(arena.transform);
        block.transform.position = center;
        block.transform.localScale = size;
        SpriteRenderer renderer = block.AddComponent<SpriteRenderer>();
        renderer.sprite = square;
        renderer.color = color;
        return block;
    }

    private void Restart()
    {
        if (arena != null)
        {
            arena.SetActive(false);
            Destroy(arena);
        }
        Time.timeScale = 1f;
        paused = false;
        crouching = false;
        arena = new GameObject("Turret Boss Arena");
        arena.transform.SetParent(transform);
        GameObject floor = Block("Arena floor", new Vector2(-1f, -0.5f), new Vector2(24f, 1f), new Color(0.2f, 0.16f, 0.35f));
        floor.layer = LayerMask.NameToLayer("Ground");
        floor.AddComponent<BoxCollider2D>();
        foreach (float wallX in new[] { -10.5f, 10.5f })
        {
            GameObject wall = Block("Arena boundary", new Vector2(wallX, 2f), new Vector2(0.3f, 4f), new Color(0.1f, 0.1f, 0.2f));
            wall.layer = LayerMask.NameToLayer("Ground");
            wall.AddComponent<BoxCollider2D>();
        }
        GameObject target = new GameObject("Sandbox Player");
        target.transform.SetParent(arena.transform);
        target.transform.position = new Vector2(-5f, 0.9f);
        target.tag = "Player";
        playerCollider = target.AddComponent<BoxCollider2D>();
        playerCollider.size = new Vector2(0.7f, 1.6f);
        playerBody = target.AddComponent<Rigidbody2D>();
        playerBody.constraints = RigidbodyConstraints2D.FreezeRotation;
        playerBody.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        health = target.AddComponent<PlayerHealth>();
        GameObject visual = Block("Player silhouette", target.transform.position, playerCollider.size, Color.cyan);
        visual.transform.SetParent(target.transform, true);
        playerVisual = visual.GetComponent<SpriteRenderer>();
        playerVisual.sortingOrder = 30;
        TurretBossAttackField field = arena.AddComponent<TurretBossAttackField>();
        field.Initialize(health, square);
        field.ConfigureVfx(attackVfx);
        if (bossPrefab != null)
        {
            boss = Instantiate(bossPrefab, new Vector3(7.5f, 0.1f, 0f), Quaternion.identity, arena.transform);
            boss.Begin(target.transform, field, bossLevel);
            boss.gameObject.AddComponent<TurretBossBodyCollision>().Initialize(boss, health, 0f);
            Sprite bossPortrait = null;
            Sprite bossPortraitBarrel = null;
            if (visualPrefab != null)
            {
                TurretBossVisualRig visualRig = Instantiate(visualPrefab, new Vector3(7.5f, 0f, 0f),
                    Quaternion.identity, arena.transform);
                bossPortrait = visualRig.BossPortrait;
                bossPortraitBarrel = visualRig.BossPortraitBarrel;
                arena.AddComponent<TurretBossCombatVisuals>().Initialize(boss, visualRig, field, 0f);
            }
            TurretBossHUD hud = arena.AddComponent<TurretBossHUD>();
            hud.Bind(boss, bossPortrait);
            hud.SetPortraitOverlay(bossPortraitBarrel);
        }
        Camera camera = Camera.main;
        if (camera == null)
        {
            GameObject cameraObject = new GameObject("Turret Boss Camera");
            cameraObject.transform.SetParent(transform);
            cameraObject.tag = "MainCamera";
            camera = cameraObject.AddComponent<Camera>();
        }
        camera.transform.position = new Vector3(0f, 3.5f, -10f);
        camera.orthographic = true;
        camera.orthographicSize = 6.5f;
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(0.025f, 0.02f, 0.06f);
    }

    private void Update()
    {
        Keyboard keyboard = Keyboard.current;
        if (keyboard == null || health == null) { return; }
        if (keyboard.rKey.wasPressedThisFrame) { Restart(); }
        if (keyboard.escapeKey.wasPressedThisFrame && !health.IsDead)
        {
            paused = !paused;
            Time.timeScale = paused ? 0f : 1f;
        }
        if (health.IsDead)
        {
            Time.timeScale = 0f;
            return;
        }
        if (paused) { return; }
        bool wantsCrouch = keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed;
        if (!wantsCrouch && crouching && StandingSpaceBlocked())
        {
            wantsCrouch = true;
        }
        crouching = wantsCrouch;
        playerCollider.size = new Vector2(0.7f, crouching ? 0.7f : 1.6f);
        playerCollider.offset = new Vector2(0f, crouching ? -0.45f : 0f);
        playerVisual.transform.localPosition = playerCollider.offset;
        playerVisual.transform.localScale = playerCollider.size;
        float horizontal = (keyboard.dKey.isPressed ? 1f : 0f) - (keyboard.aKey.isPressed ? 1f : 0f);
        playerBody.linearVelocity = new Vector2(horizontal * (crouching ? 2.5f : 6f), playerBody.linearVelocity.y);
        if (keyboard.spaceKey.wasPressedThisFrame && !crouching && HasFootSupport())
        {
            playerBody.linearVelocity = new Vector2(playerBody.linearVelocity.x, 8f);
        }
        if (boss != null)
        {
            boss.GetComponent<SpriteRenderer>().color = boss.CoreExposed ? new Color(1f, 0.6f, 1f) : Color.white;
            if (keyboard.jKey.wasPressedThisFrame) { boss.TakeDamage(50); }
        }
    }

    private bool IsSolidSupport(Collider2D shape)
    {
        return shape != null && shape.enabled && !shape.isTrigger && shape != playerCollider &&
            (shape.gameObject.layer == LayerMask.NameToLayer("Ground") ||
             shape.GetComponent<TurretBossBodyCollision>() != null);
    }

    private bool HasFootSupport()
    {
        foreach (RaycastHit2D hit in Physics2D.RaycastAll(playerBody.position, Vector2.down, 0.9f))
        {
            if (IsSolidSupport(hit.collider)) { return true; }
        }
        return false;
    }

    private bool StandingSpaceBlocked()
    {
        foreach (Collider2D shape in Physics2D.OverlapBoxAll((Vector2)playerBody.position + new Vector2(0f, 0.4f),
            new Vector2(0.65f, 0.75f), 0f))
        {
            if (IsSolidSupport(shape)) { return true; }
        }
        return false;
    }

    private void OnGUI()
    {
        GUI.Label(new Rect(15f, 15f, 1100f, 30f), "TURRET BOSS | A/D move | Space jump | S/Down crouch | J debug core hit (50) | Esc pause | R restart");
        if (health != null)
        {
            GUI.Label(new Rect(15f, 45f, 1000f, 30f), "Player HP: " + health.CurrentHealth +
                (health.IsDead ? " | DEFEATED - R to retry" : (paused ? " | PAUSED" : "")));
        }
        if (boss != null)
        {
            GUI.Label(new Rect(15f, 75f, 1000f, 30f), "Boss HP: " + boss.CurrentHealth + "/" + boss.MaxHealth + (boss.IsMobilePhase ? " | MOBILE | " : " | PLANTED | ") + boss.State + (boss.Launcher != null && boss.Launcher.IsActive ? " | Launcher: " + boss.Launcher.Stage : ""));
            GUI.Label(new Rect(15f, 105f, 1000f, 30f), boss.IsDefeated ? "VICTORY" :
                (boss.CoreExposed ? "CORE OPEN - hit J" : "Jump long charge and stomp waves / dodge launcher markers / duck the cannon"));
        }
    }

    private void OnDestroy()
    {
        Time.timeScale = 1f;
        if (square != null) { Destroy(square); }
    }
}
