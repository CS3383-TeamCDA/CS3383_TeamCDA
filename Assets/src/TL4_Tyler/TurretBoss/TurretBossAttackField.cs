using System.Collections.Generic;
using UnityEngine;

/// <summary>Sandbox hazards with visible hit regions and one hit per projectile/beam/blast.</summary>
public sealed class TurretBossAttackField : MonoBehaviour
{
    private sealed class FloorShot
    {
        public float X;
        public int Damage;
        public SpriteRenderer Visual;
    }
    private readonly List<FloorShot> shots = new List<FloorShot>();
    private PlayerHealth player;
    private Collider2D playerCollider;
    private Sprite square;
    private TurretBossAttackState state;
    private float elapsed;
    private float landingX;
    private int damage;
    private bool hit;
    private SpriteRenderer beam;
    private SpriteRenderer grenade;
    private SpriteRenderer blast;
    private SpriteRenderer marker;
    private SpriteRenderer missile;
    private bool missileLocked;
    private bool launcherModule;
    private SpriteRenderer launcherTube;
    private Vector2 bossOrigin = new Vector2(7f, 0f);
    private float aimDirection = -1f;
    private float lungeDestination;
    private SpriteRenderer leftWave;
    private SpriteRenderer rightWave;
    public void SetBossOrigin(Vector2 origin, float direction, float destination)
    {
        bossOrigin = origin;
        aimDirection = direction;
        lungeDestination = destination;
    }

    public TurretBossAttackField CreateLauncherField()
    {
        GameObject child = new GameObject("Launcher projectiles and warnings");
        child.transform.SetParent(transform);
        TurretBossAttackField separate = child.AddComponent<TurretBossAttackField>();
        separate.Initialize(player, square);
        separate.launcherModule = true;
        return separate;
    }

    public void ResolveSpin(float fromX, float toX, int attackDamage)
    {
        if (state != TurretBossAttackState.Spinning || hit) { return; }
        if (Overlaps(new Vector2((fromX + toX) * 0.5f, 0.35f), new Vector2(Mathf.Abs(toX - fromX) + 3.6f, 0.5f)))
        {
            hit = true;
            player.TakeDamage(attackDamage);
        }
    }

    public void ResolveLunge(float fromX, float toX, int attackDamage)
    {
        if (state != TurretBossAttackState.Lunging || hit) { return; }
        if (Overlaps(new Vector2((fromX + toX) * 0.5f, 1.1f), new Vector2(Mathf.Abs(toX - fromX) + 2f, 2f)))
        {
            hit = true;
            player.TakeDamage(attackDamage);
        }
    }
    public const float MissileLaunchSeconds = 0.65f;
    public const float MissileWarningSeconds = 1f;
    public const float MissileLockSeconds = 0.6f;
    public const float MissileDescentSeconds = 0.45f;
    public const float MissileBlastSeconds = 0.35f;
    public float MissileLandingX => landingX;
    public bool MissileTargetLocked => missileLocked;
    public const float BeamBottom = 0.95f;
    public const float BeamTop = 1.55f;

    public void Initialize(PlayerHealth target, Sprite sprite)
    {
        player = target;
        playerCollider = target.GetComponent<Collider2D>();
        square = sprite;
        beam = Visual("Cannon beam", Color.cyan);
        grenade = Visual("Lobbed grenade", Color.yellow);
        blast = Visual("Grenade blast", new Color(1f, 0.35f, 0.1f));
        marker = Visual("Landing warning", Color.magenta);
        missile = Visual("Vertical missile", new Color(1f, 0.65f, 0.1f));
        leftWave = Visual("Left stomp wave", Color.magenta);
        rightWave = Visual("Right stomp wave", Color.magenta);
        launcherTube = Visual("Rear launcher module", new Color(0.2f, 0.65f, 0.7f));
    }

    private SpriteRenderer Visual(string label, Color color)
    {
        GameObject visual = new GameObject(label);
        visual.transform.SetParent(transform);
        SpriteRenderer renderer = visual.AddComponent<SpriteRenderer>();
        renderer.sprite = square;
        renderer.color = color;
        renderer.sortingOrder = 20;
        renderer.enabled = false;
        return renderer;
    }

    private void Display(SpriteRenderer renderer, Vector2 center, Vector2 size, bool visible)
    {
        renderer.enabled = visible;
        renderer.transform.position = center;
        renderer.transform.localScale = size;
    }

    public void SetState(TurretBossAttackState next, float targetX, int attackDamage)
    {
        Clear();
        state = next;
        elapsed = 0f;
        damage = attackDamage;
        hit = false;
        if (next == TurretBossAttackState.GrenadeWarning || next == TurretBossAttackState.MissileWarning)
        {
            landingX = Mathf.Clamp(targetX, -9f, 4f);
        }
        missileLocked = next == TurretBossAttackState.MissileDescent || next == TurretBossAttackState.MissileBlast;
    }

    public void FireFloorShot(int attackDamage)
    {
        SpriteRenderer visual = Visual("Floor bolt", Color.magenta);
        shots.Add(new FloorShot { X = 5.5f, Damage = attackDamage, Visual = visual });
    }

    private bool Overlaps(Vector2 center, Vector2 size)
    {
        return playerCollider != null && playerCollider.enabled && player != null && !player.IsDead &&
            playerCollider.bounds.Intersects(new Bounds(center, new Vector3(size.x, size.y, 2f)));
    }

    private void Update()
    {
        Advance(Time.deltaTime);
    }

    public void Advance(float delta)
    {
        if (square == null || delta <= 0f || float.IsNaN(delta) || float.IsInfinity(delta)) { return; }
        elapsed += delta;
        if (launcherModule && state != TurretBossAttackState.Defeated)
        {
            bool launching = state == TurretBossAttackState.MissileLaunch || state == TurretBossAttackState.GrenadeFlight;
            float recoil = launching ? 0.12f * Mathf.Abs(Mathf.Sin(elapsed * 18f)) : 0f;
            Display(launcherTube, new Vector2(bossOrigin.x + 0.5f, 2.2f - recoil), new Vector2(0.65f, 0.55f), true);
            launcherTube.color = launching ? Color.yellow : new Color(0.2f, 0.65f, 0.7f);
        }
        for (int index = shots.Count - 1; index >= 0; index--)
        {
            FloorShot shot = shots[index];
            float previousX = shot.X;
            shot.X -= 8f * delta;
            Display(shot.Visual, new Vector2(shot.X, 0.25f), new Vector2(0.6f, 0.22f), true);
            bool struck = Overlaps(new Vector2((previousX + shot.X) * 0.5f, 0.25f),
                new Vector2(previousX - shot.X + 0.6f, 0.22f));
            if (struck) { player.TakeDamage(shot.Damage); }
            if (struck || shot.X < -11f)
            {
                Destroy(shot.Visual.gameObject);
                shots.RemoveAt(index);
            }
        }
        bool missileWarning = state == TurretBossAttackState.MissileWarning;
        bool descending = state == TurretBossAttackState.MissileDescent;
        if (missileWarning && !missileLocked)
        {
            if (player != null) { landingX = Mathf.Clamp(player.transform.position.x, -9f, 4f); }
            missileLocked = elapsed >= MissileLockSeconds;
        }
        if (state == TurretBossAttackState.MissileLaunch)
        {
            Display(missile, new Vector2(bossOrigin.x, Mathf.Lerp(2.5f, 10f, Mathf.Clamp01(elapsed / MissileLaunchSeconds))),
                new Vector2(0.25f, 0.75f), true);
        }
        else if (descending)
        {
            Display(missile, new Vector2(landingX, Mathf.Lerp(10f, 0.3f, Mathf.Clamp01(elapsed / MissileDescentSeconds))),
                new Vector2(0.25f, 0.75f), true);
        }
        bool floorWarning = state == TurretBossAttackState.FloorWarning;
        bool grenadeWarning = state == TurretBossAttackState.GrenadeWarning || state == TurretBossAttackState.GrenadeFlight;
        Display(marker, new Vector2(floorWarning ? 5.5f : landingX, 0.08f),
            new Vector2(floorWarning ? 0.7f : (missileWarning || descending ? 1.5f : 2f), 0.12f), floorWarning || grenadeWarning || missileWarning || descending);
        marker.color = missileWarning || descending ? (missileLocked ? Color.red : Color.yellow) : Color.magenta;
        if (state == TurretBossAttackState.GrenadeFlight)
        {
            float progress = Mathf.Clamp01(elapsed / 1.2f);
            Vector2 position = Vector2.Lerp(new Vector2(7f, 2.5f), new Vector2(landingX, 0.25f), progress);
            position.y += 4f * Mathf.Sin(progress * Mathf.PI);
            Display(grenade, position, new Vector2(0.3f, 0.3f), true);
        }
        bool missileExploding = state == TurretBossAttackState.MissileBlast;
        bool exploding = state == TurretBossAttackState.GrenadeBlast || missileExploding;
        Vector2 blastSize = missileExploding ? new Vector2(1.5f, 1.4f) : new Vector2(2f, 1f);
        Vector2 blastCenter = new Vector2(landingX, missileExploding ? 0.7f : 0.5f);
        Display(blast, blastCenter, blastSize, exploding);
        bool spinWarning = state == TurretBossAttackState.SpinWarning;
        bool lungeWarning = state == TurretBossAttackState.LungeWarning || spinWarning;
        bool stompWarning = state == TurretBossAttackState.StompWarning;
        if (lungeWarning || stompWarning)
        {
            Display(marker, new Vector2(lungeWarning ? (bossOrigin.x + lungeDestination) * 0.5f : bossOrigin.x, 0.12f),
                new Vector2(lungeWarning ? Mathf.Abs(lungeDestination - bossOrigin.x) + (spinWarning ? 3.6f : 2f) : 3f, 0.15f), true);
            marker.color = Color.red;
        }
        if (state == TurretBossAttackState.Stomping)
        {
            float travel = elapsed * 8f;
            Display(leftWave, new Vector2(bossOrigin.x - travel, 0.22f), new Vector2(0.65f, 0.3f), true);
            Display(rightWave, new Vector2(bossOrigin.x + travel, 0.22f), new Vector2(0.65f, 0.3f), true);
            float previousTravel = Mathf.Max(0f, elapsed - delta) * 8f;
            float centerTravel = (previousTravel + travel) * 0.5f;
            Vector2 waveSweep = new Vector2(travel - previousTravel + 0.65f, 0.3f);
            if (!hit && (Overlaps(new Vector2(bossOrigin.x - centerTravel, 0.22f), waveSweep) ||
                Overlaps(new Vector2(bossOrigin.x + centerTravel, 0.22f), waveSweep)))
            {
                hit = true;
                player.TakeDamage(damage);
            }
        }
        bool charging = state == TurretBossAttackState.BeamCharge;
        bool firing = state == TurretBossAttackState.BeamFire;
        float muzzleX = bossOrigin.x + aimDirection;
        float beamEnd = aimDirection < 0f ? -10f : 5f;
        Vector2 beamCenter = new Vector2((muzzleX + beamEnd) * 0.5f, (BeamBottom + BeamTop) * 0.5f);
        Vector2 beamSize = new Vector2(Mathf.Abs(beamEnd - muzzleX), BeamTop - BeamBottom);
        Display(beam, beamCenter, new Vector2(beamSize.x, charging ? 0.04f : beamSize.y), charging || firing);
        beam.color = charging ? new Color(1f, 0.2f, 0.2f, 0.5f) : Color.cyan;
        if (!hit && ((exploding && Overlaps(blastCenter, blastSize)) ||
            (firing && Overlaps(beamCenter, beamSize))))
        {
            hit = true;
            player.TakeDamage(damage);
        }
    }

    public void Clear()
    {
        state = TurretBossAttackState.Defeated;
        missileLocked = false;
        foreach (FloorShot shot in shots)
        {
            if (shot.Visual != null) { Destroy(shot.Visual.gameObject); }
        }
        shots.Clear();
        foreach (SpriteRenderer renderer in new[] { beam, grenade, blast, marker, missile, leftWave, rightWave, launcherTube })
        {
            if (renderer != null) { renderer.enabled = false; }
        }
    }
}
