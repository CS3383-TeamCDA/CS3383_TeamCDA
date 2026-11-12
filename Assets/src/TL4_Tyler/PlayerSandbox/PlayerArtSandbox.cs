using UnityEngine;
using UnityEngine.InputSystem;
using TwoHandedHero;

/// <summary>TL4-only harness; locomotion and health remain owned by TL6.</summary>
public sealed class PlayerArtSandbox : MonoBehaviour
{
    [SerializeField] GameObject playerPrefab;
    [SerializeField] TwoHandedRig visualPrefab;
    [SerializeField] TwoHandedWeapon[] weapons;
    [SerializeField] RuntimeAnimatorController locomotionAnimator;
    PlayerControllerTL6 controller;
    PlayerHealth health;
    PlayerCrouchSlide stance;
    Rigidbody2D body;
    TwoHandedRig rig;
    Sprite square;
    PlayerSandboxMotor motor;
    PlayerSandboxFeedback feedback;
    PlayerSandboxCombat combat;
    SandboxTargetDummy[] targets;
    int weaponIndex;
    readonly Vector2 spawn = new Vector2(-11, 1.2f);

    void Start()
    {
        square = Sprite.Create(Texture2D.whiteTexture, new Rect(0, 0, Texture2D.whiteTexture.width,
            Texture2D.whiteTexture.height), Vector2.one * .5f, Texture2D.whiteTexture.width);
        var cameraObject = new GameObject("Sandbox Camera", typeof(Camera), typeof(AudioListener));
        cameraObject.transform.SetParent(transform);
        cameraObject.transform.position = new Vector3(0, 3, -10);
        var camera = cameraObject.GetComponent<Camera>();
        camera.orthographic = true;
        camera.orthographicSize = 7;
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(.035f, .045f, .08f);
        Block("Floor", new Vector2(0, -.5f), new Vector2(28, 1));
        Block("Left wall", new Vector2(-14, 3), new Vector2(.5f, 7));
        Block("Right wall", new Vector2(14, 3), new Vector2(.5f, 7));
        Block("Low platform", new Vector2(-3, 1.1f), new Vector2(3, .35f));
        Block("High platform", new Vector2(2, 2.3f), new Vector2(3, .35f));
        Block("Crouch tunnel roof", new Vector2(-8, .95f), new Vector2(2.5f, .4f));
        Block("Landing platform", new Vector2(7, 1.1f), new Vector2(3, .35f));
        var player = Instantiate(playerPrefab, spawn, Quaternion.identity, transform);
        controller = player.GetComponent<PlayerControllerTL6>();
        body = player.GetComponent<Rigidbody2D>();
        health = player.GetComponent<PlayerHealth>();
        rig = Instantiate(visualPrefab, player.transform);
        if (locomotionAnimator != null) rig.bodyAnimator.runtimeAnimatorController = locomotionAnimator;
        // Art pivots sit at the feet; TL6's capsule is centered on its root.
        rig.transform.localPosition = new Vector3(0, -.5f, 0);
        rig.transform.localScale = Vector3.one * (1f / 2.4f);
        rig.stableBodyWhenFiring = true;
        rig.recoilDistanceMultiplier = 3f;
        rig.recoilRiseDegrees = 4f;
        player.AddComponent<PlayerMotionEffects>().Initialize(body, rig);
        stance = player.AddComponent<PlayerCrouchSlide>();
        stance.Initialize(controller);
        feedback = player.AddComponent<PlayerSandboxFeedback>();
        feedback.Initialize(rig, health);
        motor = player.AddComponent<PlayerSandboxMotor>();
        motor.Initialize(controller, stance, feedback);
        combat = player.AddComponent<PlayerSandboxCombat>();
        combat.Initialize(rig, camera, square, feedback);
        targets = new[] {
            Target("Ground target", new Vector2(11.5f, .85f)),
            Target("High target", new Vector2(5f, 5.2f)),
            Target("Left target", new Vector2(-5.5f, 4.2f))
        };
        var danger = new GameObject("Damage test pad", typeof(SpriteRenderer));
        danger.transform.SetParent(transform, false);
        danger.transform.position = new Vector2(10, .06f);
        danger.transform.localScale = new Vector3(1.5f, .12f, 1);
        danger.GetComponent<SpriteRenderer>().sprite = square;
        danger.GetComponent<SpriteRenderer>().color = new Color(.85f, .15f, .22f);
        danger.GetComponent<SpriteRenderer>().sortingOrder = 2;
    }

    void Block(string label, Vector2 position, Vector2 size)
    {
        var block = new GameObject(label, typeof(SpriteRenderer), typeof(BoxCollider2D));
        block.transform.SetParent(transform);
        block.transform.position = position;
        block.transform.localScale = size;
        block.layer = LayerMask.NameToLayer("Ground");
        var renderer = block.GetComponent<SpriteRenderer>();
        renderer.sprite = square;
        renderer.color = new Color(.18f, .28f, .38f);
        // The collider is added before the renderer has a sprite. Do not use its
        // automatically inferred size: the transform supplies the block dimensions.
        var collider = block.GetComponent<BoxCollider2D>();
        collider.size = Vector2.one;
        collider.offset = Vector2.zero;
        collider.isTrigger = false;
    }

    void Update()
    {
        if (controller == null) return;
        var keyboard = Keyboard.current;
        if (keyboard == null) return;
        if (keyboard.rKey.wasPressedThisFrame) ResetPlayer();
        if (keyboard.hKey.wasPressedThisFrame) combat.TakeDamage(25);
        if (keyboard.tKey.wasPressedThisFrame) health.Heal(25);
        if (health.IsDead) { controller.enabled = false; body.linearVelocity = Vector2.zero; return; }
        if (keyboard.qKey.wasPressedThisFrame && weapons.Length > 0)
        {
            weaponIndex = (weaponIndex + 1) % weapons.Length;
            rig.Equip(weapons[weaponIndex]);
        }
        if (Mathf.Abs(body.position.x - 10f) < .9f && body.position.y < .9f && body.position.y > -.5f)
            combat.TakeDamage(20);
        if (body.position.y < -8) ResetPlayer();
    }

    void LateUpdate()
    {
        if (rig == null) return;
        if (Mathf.Abs(combat.AimDirection.x) > .1f)
            rig.SetFacingRight(combat.AimDirection.x > 0);
        rig.SetLocomotion(controller.IsGrounded(), health.IsDead ? 0f : body.linearVelocity.x,
            body.linearVelocity.y, stance.IsLow, stance.IsSliding);
    }

    void ResetPlayer()
    {
        stance.ResetStance();
        body.position = spawn;
        body.linearVelocity = Vector2.zero;
        health.ResetHealth();
        motor.ResetMotor();
        feedback.ResetFeedback();
        combat.ResetCombat();
        foreach (var target in targets) target.ResetTarget();
    }

    SandboxTargetDummy Target(string label, Vector2 position)
    {
        var target = new GameObject(label, typeof(SpriteRenderer), typeof(BoxCollider2D));
        target.transform.SetParent(transform, false);
        target.transform.position = position;
        var renderer = target.GetComponent<SpriteRenderer>();
        renderer.sprite = square;
        renderer.color = new Color(.95f, .55f, .18f);
        target.transform.localScale = new Vector3(.65f, 1.5f, 1);
        var collider = target.GetComponent<BoxCollider2D>();
        collider.size = Vector2.one;
        collider.isTrigger = true;
        var dummy = target.AddComponent<SandboxTargetDummy>();
        dummy.Initialize(renderer);
        return dummy;
    }

    void OnGUI()
    {
        if (health == null || combat == null) return;
        GUILayout.BeginArea(new Rect(16, 16, 760, 230), GUI.skin.box);
        GUILayout.Label("TL4 PLAYER ART SANDBOX | movement + combat test");
        GUILayout.Label("A/D move | Space jump / double-jump flip | Shift dash | Mouse or I/J/K/L aim");
        GUILayout.Label("F / Left mouse fire | Q weapon | S / Down / Ctrl crouch (running = slide)");
        GUILayout.Label("H damage | T heal | R reset | Red floor pad deals damage; orange targets respawn");
        GUILayout.Label($"Health {health.CurrentHealth}/{health.MaxHealth} | Extra jump {motor.AirJumpAvailable} | Air dash {controller.AirDashAvailable}");
        GUILayout.Label($"Art: {rig.Action} | Weapon: {rig.Weapon.name} | Shots {combat.ShotsFired} | Hits {combat.HitsLanded}");
        foreach (var target in targets) GUILayout.Label($"{target.name}: {target.Health}/100 ({target.Hits} hits)");
        if (health.IsDead) GUILayout.Label("PLAYER DOWN - R to restart");
        else if (feedback.Invulnerable) GUILayout.Label("Damage protection active");
        GUILayout.EndArea();
    }

    void OnDestroy() { if (square != null) Destroy(square); }
}
