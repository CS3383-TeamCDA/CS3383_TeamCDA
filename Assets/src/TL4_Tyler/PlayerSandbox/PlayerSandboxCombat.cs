using UnityEngine;
using UnityEngine.InputSystem;
using TwoHandedHero;

/// <summary>Eight-way aiming, swept test shots and sandbox-only damage routing.</summary>
[DefaultExecutionOrder(600)]
public sealed class PlayerSandboxCombat : MonoBehaviour
{
    sealed class Shot
    {
        public SpriteRenderer visual;
        public Vector2 direction;
        public float life;
    }
    readonly Shot[] shots = new Shot[32];
    readonly RaycastHit2D[] hits = new RaycastHit2D[24];
    TwoHandedRig rig;
    PlayerHealth health;
    PlayerSandboxFeedback feedback;
    Camera view;
    Transform shotRoot;
    SpriteRenderer flash;
    float nextShot, flashUntil;
    int cursor;
    bool fireHeld, mouseAim;
    Vector2 previousMouse;
    ContactFilter2D filter;
    public Vector2 AimDirection { get; private set; } = Vector2.right;
    public int ShotsFired { get; private set; }
    public int HitsLanded { get; private set; }

    public void Initialize(TwoHandedRig visual, Camera camera, Sprite square, PlayerSandboxFeedback effects)
    {
        rig = visual; view = camera; feedback = effects;
        health = GetComponent<PlayerHealth>();
        shotRoot = new GameObject("Sandbox shots").transform;
        shotRoot.SetParent(transform.parent, false);
        for (int i = 0; i < shots.Length; i++)
        {
            var renderer = new GameObject("Test round", typeof(SpriteRenderer)).GetComponent<SpriteRenderer>();
            renderer.transform.SetParent(shotRoot, false);
            renderer.sprite = square; renderer.enabled = false; renderer.sortingOrder = 45;
            renderer.transform.localScale = new Vector3(.22f, .045f, 1);
            shots[i] = new Shot { visual = renderer };
        }
        flash = new GameObject("Muzzle flash", typeof(SpriteRenderer)).GetComponent<SpriteRenderer>();
        flash.transform.SetParent(rig.muzzle, false);
        flash.sprite = square; flash.enabled = false; flash.sortingOrder = 46;
        flash.transform.localScale = new Vector3(.25f, .12f, 1);
        filter = ContactFilter2D.noFilter; filter.useTriggers = true;
        if (Mouse.current != null) previousMouse = Mouse.current.position.ReadValue();
    }

    void Update()
    {
        var keyboard = Keyboard.current;
        if (health.IsDead) { fireHeld = false; rig.SetAimDirection(Vector2.zero); return; }
        Vector2 direction = Vector2.zero;
        if (keyboard != null)
            direction = new Vector2((keyboard.lKey.isPressed ? 1 : 0) - (keyboard.jKey.isPressed ? 1 : 0),
                (keyboard.iKey.isPressed ? 1 : 0) - (keyboard.kKey.isPressed ? 1 : 0));
        if (direction.sqrMagnitude > 0) mouseAim = false;
        var mouse = Mouse.current;
        if (mouse != null)
        {
            Vector2 position = mouse.position.ReadValue();
            if ((position - previousMouse).sqrMagnitude > 4 || mouse.leftButton.wasPressedThisFrame) mouseAim = true;
            previousMouse = position;
            // Retain keyboard aim while an aim key is held.
            if (mouseAim && direction.sqrMagnitude == 0)
            {
                Vector3 world = view.ScreenToWorldPoint(new Vector3(position.x, position.y, -view.transform.position.z));
                direction = (Vector2)world - (Vector2)transform.position;
            }
        }
        if (direction.sqrMagnitude > .001f)
        {
            float angle = Mathf.Round(Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg / 45f) * 45f;
            AimDirection = new Vector2(Mathf.Cos(angle * Mathf.Deg2Rad), Mathf.Sin(angle * Mathf.Deg2Rad));
        }
        rig.SetAimDirection(AimDirection);
        fireHeld = (keyboard != null && keyboard.fKey.isPressed) || (mouse != null && mouse.leftButton.isPressed);
    }

    void LateUpdate()
    {
        // Rig and flip evaluation have finished; shoot from the actual final muzzle.
        if (!health.IsDead && fireHeld && Time.time >= nextShot)
        {
            if (rig.Fire())
            {
                nextShot = Time.time + .18f;
                var shot = shots[cursor++ % shots.Length];
                shot.visual.transform.position = rig.muzzle.position;
                shot.direction = AimDirection;
                shot.visual.transform.rotation = Quaternion.Euler(0, 0, Mathf.Atan2(AimDirection.y, AimDirection.x) * Mathf.Rad2Deg);
                shot.visual.color = new Color(.25f, .85f, 1, 1);
                shot.visual.enabled = true; shot.life = 1.5f;
                flashUntil = Time.time + .045f; ShotsFired++;
                // Check the shoulder-to-muzzle segment too: a protruding muzzle
                // must not let rounds appear on the far side of a wall.
                Vector2 start = rig.AimOrigin;
                Sweep(shot, start, (Vector2)rig.muzzle.position - start);
            }
        }
        flash.enabled = !health.IsDead && Time.time < flashUntil;
        flash.color = new Color(1, .85f, .3f, 1);
        foreach (var shot in shots)
        {
            if (shot.life <= 0) continue;
            Vector2 travel = shot.direction * (32f * Time.deltaTime);
            Sweep(shot, shot.visual.transform.position, travel);
            if (shot.life <= 0) continue;
            shot.visual.transform.position += (Vector3)travel;
            shot.life -= Time.deltaTime;
            shot.visual.enabled = shot.life > 0;
        }
    }

    void Sweep(Shot shot, Vector2 start, Vector2 travel)
    {
        if (travel.sqrMagnitude < .000001f) return;
        int count = Physics2D.CircleCast(start, .025f, travel.normalized, filter, hits, travel.magnitude);
        RaycastHit2D nearest = default;
        float distance = float.PositiveInfinity;
        for (int i = 0; i < count; i++)
        {
            if (hits[i].collider.transform.IsChildOf(transform)) continue;
            if (hits[i].collider.isTrigger && hits[i].collider.GetComponent<SandboxTargetDummy>() == null) continue;
            if (hits[i].distance < distance) { nearest = hits[i]; distance = hits[i].distance; }
        }
        if (nearest.collider == null) return;
        var target = nearest.collider.GetComponent<SandboxTargetDummy>();
        if (target != null && target.Health > 0) { target.Hit(10); HitsLanded++; }
        shot.life = 0; shot.visual.enabled = false;
    }

    public void TakeDamage(int damage)
    {
        if (damage <= 0 || health.IsDead || feedback.Invulnerable) return;
        health.TakeDamage(damage);
    }

    public void ResetCombat()
    {
        nextShot = flashUntil = 0; ShotsFired = HitsLanded = 0; fireHeld = false;
        foreach (var shot in shots) { shot.life = 0; shot.visual.enabled = false; }
        flash.enabled = false;
    }

    void OnDestroy() { if (shotRoot != null) Destroy(shotRoot.gameObject); }
}
