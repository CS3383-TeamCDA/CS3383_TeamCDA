using System.Collections.Generic;
using UnityEngine;

/// <summary>Sandbox hazards with visible hit regions and one hit per projectile/beam/blast.</summary>
[DefaultExecutionOrder(100)]
public sealed class TurretBossAttackField : MonoBehaviour
{
    private sealed class FloorShot
    {
        public float X, Y, Direction;
        public int Damage;
        public SpriteRenderer Visual;
    }
    private readonly List<FloorShot> shots = new List<FloorShot>();
    private TurretBossVfxSet vfx;
    private readonly List<TurretBossVfxBurst> aftermath = new List<TurretBossVfxBurst>();
    public void ConfigureVfx(TurretBossVfxSet effects) { vfx = effects; }
    private PlayerHealth player;
    private readonly List<Collider2D> overlapResults = new List<Collider2D>(8);
    private Sprite square;
    private TurretBossAttackState state;
    private float elapsed;
    private float landingX;
    private int damage;
    private bool hit;
    public bool ChargeHitPlayer => hit && (state == TurretBossAttackState.ShortCharge || state == TurretBossAttackState.LongCharge);
    private SpriteRenderer beam;
    private Material warningBeamMaterial;
    private MaterialPropertyBlock beamProperties;
    private SpriteRenderer grenade;
    private SpriteRenderer blast;
    private SpriteRenderer marker;
    private static Sprite landingShadow;
    private SpriteRenderer missile;
    private bool missileLocked;
    private bool launcherModule;
    private bool showLauncherModule = true;
    private Transform floorMuzzle;
    private TurretBossVisualRig visualRig;
    private float lastResolvedDashX;
    public void BindBodyAnchors(TurretBossVisualRig rig)
    {
        visualRig = rig;
        floorMuzzle = rig != null ? rig.FloorMuzzle : null;
    }
    private bool mobileFloorOrigin;
    public void SetMobileFloorOrigin(bool mobile) { mobileFloorOrigin = mobile; }
    private Vector2 FloorOrigin => mobileFloorOrigin
        ? new Vector2(bossOrigin.x + aimDirection * 0.95f, 0.25f)
        : floorMuzzle != null ? (Vector2)floorMuzzle.position
        : new Vector2(bossOrigin.x + aimDirection * 1.5f, 0.25f);
    private Transform beamMuzzle;
    public void BindBeamMuzzle(Transform muzzle) { beamMuzzle = muzzle; }
    public void ShowLauncherModule(bool value)
    {
        showLauncherModule = value;
        if (!value && launcherTube != null) { launcherTube.enabled = false; }
    }
    private SpriteRenderer launcherTube;
    private Vector2 bossOrigin = new Vector2(7f, 0f);
    private Vector2 payloadLaunchOrigin;
    private float aimDirection = -1f;
    private float lungeDestination;
    [SerializeField] private Vector2 chargeBodyOffset = new Vector2(1.15f, -0.64f);
    [SerializeField] private Vector2 chargeBoxSize = new Vector2(2.25f, 0.6875f);
    [SerializeField, Min(0f)] private float chargeGroundInset = 0.04f;
    private BoxCollider2D chargeBox;
    private int chargeDamage;
    private SpriteRenderer dashWake;
    private float dashDirection = -1f;
    private Vector2 lastDashCenter;
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
        separate.ConfigureVfx(vfx);
        return separate;
    }

    public void ResolveLongCharge(float fromX, float toX, int attackDamage)
    {
        if (state == TurretBossAttackState.LongCharge) { ResolveCharge(fromX, toX, attackDamage); }
    }

    public void ResolveShortCharge(float fromX, float toX, int attackDamage)
    {
        if (state == TurretBossAttackState.ShortCharge) { ResolveCharge(fromX, toX, attackDamage); }
    }

    private void ResolveCharge(float fromX, float toX, int attackDamage)
    {
        chargeDamage = attackDamage;
        DrawDashWake(fromX, toX);
        // Sweep the SAME box between frames so a fast dash cannot tunnel through the player.
        float travel = toX - fromX;
        Vector2 sweptCenter = lastDashCenter - new Vector2(travel * 0.5f, 0f);
        if (Overlaps(sweptCenter, chargeBoxSize + new Vector2(Mathf.Abs(travel), 0f))) { TryChargeContact(); }
    }

    private void TryChargeContact()
    {
        if (hit || chargeBox == null || !chargeBox.enabled ||
            (state != TurretBossAttackState.ShortCharge && state != TurretBossAttackState.LongCharge) ||
            player == null || player.IsDead) { return; }
        hit = true;
        player.TakeDamage(chargeDamage);
    }

    private void DrawDashWake(float fromX, float toX)
    {
        if (Mathf.Abs(toX - fromX) > 0.001f) { dashDirection = Mathf.Sign(toX - fromX); }
        float progress = state == TurretBossAttackState.ShortCharge
            ? elapsed / TurretBossController.ShortChargeSeconds : elapsed * 3f;
        Skin(dashWake, vfx != null ? vfx.shockwaves : null, progress, state != TurretBossAttackState.ShortCharge);
        dashWake.flipX = dashDirection < 0f;
        lastResolvedDashX = toX;
        Vector2 centerline = visualRig != null ? visualRig.BodyCenter : new Vector2(toX, 1f);
        // Use the current logical X plus visual body sway, never an animated foot anchor.
        if (visualRig != null) { centerline.x += toX - visualRig.transform.position.x; }
        lastDashCenter = centerline + new Vector2(dashDirection * chargeBodyOffset.x, chargeBodyOffset.y);
        int groundLayer = LayerMask.NameToLayer("Ground");
        if (groundLayer >= 0)
        {
            RaycastHit2D ground = Physics2D.Raycast(lastDashCenter + Vector2.up * 2f, Vector2.down, 6f, 1 << groundLayer);
            if (ground.collider != null) { lastDashCenter.y = ground.point.y + chargeBoxSize.y * 0.5f - chargeGroundInset; }
        }
        bool hasArt = vfx != null && vfx.shockwaves != null && vfx.shockwaves.Length > 0;
        Display(dashWake, lastDashCenter, chargeBoxSize, hasArt);
        // Renderer and contact box share this transform and the same world footprint.
        Vector3 scale = dashWake.transform.lossyScale;
        chargeBox.size = new Vector2(chargeBoxSize.x / Mathf.Max(0.001f, Mathf.Abs(scale.x)),
            chargeBoxSize.y / Mathf.Max(0.001f, Mathf.Abs(scale.y)));
        chargeBox.enabled = true;
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
        square = sprite;
        beam = Visual("Cannon beam", Color.cyan);
        warningBeamMaterial = beam.sharedMaterial;
        beamProperties = new MaterialPropertyBlock();
        grenade = Visual("Lobbed grenade", Color.yellow);
        blast = Visual("Grenade blast", new Color(1f, 0.35f, 0.1f));
        marker = Visual("Landing warning", Color.magenta);
        missile = Visual("Vertical missile", new Color(1f, 0.65f, 0.1f));
        dashWake = Visual("Charge dash wake", Color.white);
        chargeBox = dashWake.gameObject.AddComponent<BoxCollider2D>();
        chargeBox.isTrigger = true;
        chargeBox.enabled = false;
        Rigidbody2D contactBody = dashWake.gameObject.AddComponent<Rigidbody2D>();
        contactBody.bodyType = RigidbodyType2D.Kinematic;
        contactBody.gravityScale = 0f;
        contactBody.constraints = RigidbodyConstraints2D.FreezeRotation;
        dashWake.gameObject.AddComponent<TurretBossChargeContact>().Bind(player, TryChargeContact);
        dashWake.sortingOrder = 44; // Above near legs (31–33), below impact flashes (45).
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
        Vector2 bounds = renderer.sprite != null ? (Vector2)renderer.sprite.bounds.size : Vector2.one;
        if (renderer.drawMode == SpriteDrawMode.Tiled)
        {
            float scale = size.y / Mathf.Max(0.001f, bounds.y);
            renderer.transform.localScale = new Vector3(scale, scale, 1f);
            renderer.size = new Vector2(size.x / Mathf.Max(0.001f, scale), bounds.y);
        }
        else
        {
            renderer.transform.localScale = new Vector3(size.x / Mathf.Max(0.001f, bounds.x), size.y / Mathf.Max(0.001f, bounds.y), 1f);
        }
    }

    public void SetState(TurretBossAttackState next, float targetX, int attackDamage)
    {
        bool leavingDash = (state == TurretBossAttackState.ShortCharge || state == TurretBossAttackState.LongCharge)
            && dashWake != null && dashWake.enabled;
        Clear(true);
        if (leavingDash && next != TurretBossAttackState.Defeated && next != TurretBossAttackState.Transforming &&
            vfx != null && vfx.shockwaves != null && vfx.shockwaves.Length > 0)
        {
            aftermath.RemoveAll(effect => effect == null);
            GameObject skid = new GameObject("Charge skid aftermath (visual only)");
            skid.transform.SetParent(transform, false);
            skid.transform.position = lastDashCenter;
            TurretBossVfxBurst effect = skid.AddComponent<TurretBossVfxBurst>();
            effect.Initialize(vfx.shockwaves, chargeBoxSize, 0.3f, dashDirection < 0f);
            aftermath.Add(effect);
        }
        state = next;
        if (next == TurretBossAttackState.ShortCharge || next == TurretBossAttackState.LongCharge) { dashDirection = aimDirection; }
        elapsed = 0f;
        damage = attackDamage;
        hit = false;
        if (next == TurretBossAttackState.GrenadeWarning || next == TurretBossAttackState.MissileWarning)
        {
            landingX = Mathf.Clamp(targetX, -9f, 9f);
        }
        if (next == TurretBossAttackState.GrenadeFlight || next == TurretBossAttackState.MissileLaunch)
        {
            payloadLaunchOrigin = bossOrigin + new Vector2(0.5f, 2.2f);
        }
        missileLocked = next == TurretBossAttackState.MissileDescent || next == TurretBossAttackState.MissileBlast;
        if ((next == TurretBossAttackState.GrenadeBlast || next == TurretBossAttackState.MissileBlast) &&
            vfx != null && vfx.explosions != null && vfx.explosions.Length > 0)
        {
            bool rocket = next == TurretBossAttackState.MissileBlast;
            aftermath.RemoveAll(effect => effect == null);
            GameObject impact = new GameObject("Animated payload explosion (visual only)");
            impact.transform.SetParent(transform, false);
            Vector2 groundAnchor;
            if (!EnemyBlast.TryGroundVisualAnchor(new Vector2(landingX, .5f), out groundAnchor))
                groundAnchor = new Vector2(landingX, 0);
            impact.transform.position = groundAnchor;
            TurretBossVfxBurst effect = impact.AddComponent<TurretBossVfxBurst>();
            effect.Initialize(vfx.explosions, rocket ? new Vector2(1.5f, 1.4f) : new Vector2(2f, 1f), 0.65f,
                anchorBottom: true, transparentBottom: vfx.groundExplosionBottomInset);
            aftermath.Add(effect);
        }
    }

    public void FireFloorShot(int attackDamage)
    {
        SpriteRenderer visual = Visual("Floor bolt", Color.magenta);
        Vector2 origin = FloorOrigin;
        FloorShot shot = new FloorShot { X = origin.x, Y = origin.y, Direction = aimDirection, Damage = attackDamage, Visual = visual };
        shots.Add(shot);
        Skin(visual, vfx != null ? vfx.beam : null, 0f, true);
        visual.flipX = shot.Direction < 0f;
        Display(visual, origin, new Vector2(0.6f, 0.22f), true);
    }

    private static Sprite LandingShadow()
    {
        if (landingShadow != null) { return landingShadow; }
        const int size = 64;
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        texture.name = "Payload ground shadow";
        texture.wrapMode = TextureWrapMode.Clamp;
        texture.filterMode = FilterMode.Bilinear;
        Color[] pixels = new Color[size * size];
        for (int y = 0; y < size; y++)
        for (int x = 0; x < size; x++)
        {
            float radius = new Vector2((x + 0.5f) / size * 2f - 1f, (y + 0.5f) / size * 2f - 1f).magnitude;
            float edge = 1f - Mathf.SmoothStep(0.85f, 1f, radius);
            float rim = Mathf.SmoothStep(0.65f, 0.85f, radius);
            Color color = Color.Lerp(new Color(0.015f, 0.01f, 0.035f), new Color(0.42f, 0.3f, 0.55f), rim);
            color.a = edge * 0.85f;
            pixels[y * size + x] = color;
        }
        texture.SetPixels(pixels);
        texture.Apply(false, true);
        landingShadow = Sprite.Create(texture, new Rect(0f, 0f, size, size), Vector2.one * 0.5f, size);
        return landingShadow;
    }

    private bool Overlaps(Vector2 center, Vector2 size)
    {
        if (player == null || player.IsDead || !player.gameObject.activeInHierarchy) { return false; }
        // Crouch changes and child hurtboxes must be visible to the query immediately.
        Physics2D.SyncTransforms();
        ContactFilter2D filter = ContactFilter2D.noFilter;
        filter.useTriggers = false;
        overlapResults.Clear();
        Physics2D.OverlapBox(center, size, 0f, filter, overlapResults);
        foreach (Collider2D candidate in overlapResults)
        {
            if (candidate != null && candidate.enabled && !candidate.isTrigger &&
                candidate.GetComponentInParent<PlayerHealth>() == player) { return true; }
        }
        return false;
    }

    private void Update()
    {
        Advance(Time.deltaTime);
    }

    private void LateUpdate()
    {
        if (Time.deltaTime > 0f && dashWake != null && dashWake.enabled &&
            (state == TurretBossAttackState.ShortCharge || state == TurretBossAttackState.LongCharge))
        {
            DrawDashWake(lastResolvedDashX, lastResolvedDashX);
        }
    }

    public void Advance(float delta)
    {
        if (square == null || delta <= 0f || float.IsNaN(delta) || float.IsInfinity(delta)) { return; }
        elapsed += delta;
        if (launcherModule && showLauncherModule && state != TurretBossAttackState.Defeated)
        {
            bool launching = state == TurretBossAttackState.MissileLaunch || state == TurretBossAttackState.GrenadeFlight;
            float recoil = launching ? 0.12f * Mathf.Abs(Mathf.Sin(elapsed * 18f)) : 0f;
            Display(launcherTube, bossOrigin + new Vector2(0.5f, 2.2f - recoil), new Vector2(0.65f, 0.55f), true);
            launcherTube.color = launching ? Color.yellow : new Color(0.2f, 0.65f, 0.7f);
        }
        for (int index = shots.Count - 1; index >= 0; index--)
        {
            FloorShot shot = shots[index];
            float previousX = shot.X;
            shot.X += shot.Direction * 8f * delta;
            Skin(shot.Visual, vfx != null ? vfx.beam : null, elapsed * 2f, true);
            Display(shot.Visual, new Vector2(shot.X, shot.Y), new Vector2(0.6f, 0.22f), true);
            bool struck = Overlaps(new Vector2((previousX + shot.X) * 0.5f, shot.Y),
                new Vector2(Mathf.Abs(previousX - shot.X) + 0.6f, 0.22f));
            if (struck) { player.TakeDamage(shot.Damage); }
            if (struck || shot.X < -11f || shot.X > 11f)
            {
                Destroy(shot.Visual.gameObject);
                shots.RemoveAt(index);
            }
        }
        bool missileWarning = state == TurretBossAttackState.MissileWarning;
        bool descending = state == TurretBossAttackState.MissileDescent;
        if (missileWarning && !missileLocked)
        {
            if (player != null) { landingX = Mathf.Clamp(player.transform.position.x, -9f, 9f); }
            missileLocked = elapsed >= MissileLockSeconds;
        }
        if (state == TurretBossAttackState.MissileLaunch)
        {
            Skin(missile, vfx != null ? vfx.rockets : null, elapsed * 4f, true);
            missile.flipY = false;
            Display(missile, new Vector2(payloadLaunchOrigin.x, Mathf.Lerp(payloadLaunchOrigin.y, 10f, Mathf.Clamp01(elapsed / MissileLaunchSeconds))),
                new Vector2(0.25f, 0.75f), true);
        }
        else if (descending)
        {
            Skin(missile, vfx != null ? vfx.rockets : null, elapsed * 4f, true);
            missile.flipY = true;
            Display(missile, new Vector2(landingX, Mathf.Lerp(10f, 0.3f, Mathf.Clamp01(elapsed / MissileDescentSeconds))),
                new Vector2(0.25f, 0.75f), true);
        }
        bool floorWarning = state == TurretBossAttackState.FloorWarning;
        bool grenadeWarning = state == TurretBossAttackState.GrenadeWarning || state == TurretBossAttackState.GrenadeFlight;
        bool payloadShadow = grenadeWarning || missileWarning || descending;
        marker.sprite = payloadShadow ? LandingShadow() : square;
        float descent = descending ? Mathf.Clamp01(elapsed / MissileDescentSeconds)
            : state == TurretBossAttackState.GrenadeFlight ? Mathf.Clamp01(elapsed / 1.2f) : 0f;
        float footprint = missileWarning || descending ? 1.5f : 2f;
        float shadowWidth = footprint * Mathf.Lerp(1.35f, 1f, descent);
        Display(marker, new Vector2(floorWarning ? FloorOrigin.x : landingX, 0.035f),
            new Vector2(floorWarning ? 0.7f : shadowWidth, payloadShadow ? 0.24f : 0.12f),
            floorWarning || payloadShadow);
        marker.color = payloadShadow ? new Color(1f, 1f, 1f, Mathf.Lerp(0.55f, 1f, descent)) : Color.magenta;
        if (state == TurretBossAttackState.GrenadeFlight)
        {
            float progress = Mathf.Clamp01(elapsed / 1.2f);
            Vector2 position = Vector2.Lerp(payloadLaunchOrigin, new Vector2(landingX, 0.25f), progress);
            position.y += 4f * Mathf.Sin(progress * Mathf.PI);
            Skin(grenade, vfx != null ? vfx.grenades : null, elapsed * 2.5f, true);
            Display(grenade, position, new Vector2(0.3f, 0.3f), true);
        }
        bool missileExploding = state == TurretBossAttackState.MissileBlast;
        bool exploding = state == TurretBossAttackState.GrenadeBlast || missileExploding;
        Vector2 blastSize = missileExploding ? new Vector2(1.5f, 1.4f) : new Vector2(2f, 1f);
        Vector2 blastCenter = new Vector2(landingX, missileExploding ? 0.7f : 0.5f);
        bool animatedExplosion = vfx != null && vfx.explosions != null && vfx.explosions.Length > 0;
        Display(blast, blastCenter, blastSize, exploding && !animatedExplosion);
        bool spinWarning = state == TurretBossAttackState.LongChargeWarning;
        bool lungeWarning = state == TurretBossAttackState.ShortChargeWarning || spinWarning;
        bool stompWarning = state == TurretBossAttackState.StompWarning;
        if (lungeWarning || stompWarning)
        {
            Display(marker, new Vector2(lungeWarning ? (bossOrigin.x + lungeDestination) * 0.5f : bossOrigin.x, 0.12f),
                new Vector2(lungeWarning ? Mathf.Abs(lungeDestination - bossOrigin.x) + (spinWarning ? 3.6f : 2f) : 3f, 0.15f), true);
            marker.color = Color.red;
        }
        if (state == TurretBossAttackState.Stomping)
        {
            Skin(leftWave, vfx != null ? vfx.shockwaves : null, elapsed * 3f, true);
            Skin(rightWave, vfx != null ? vfx.shockwaves : null, elapsed * 3f, true);
            leftWave.flipX = true;
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
        float muzzleX = beamMuzzle != null ? beamMuzzle.position.x : bossOrigin.x + aimDirection;
        float beamEnd = aimDirection < 0f ? -10f : 10f;
        Vector2 beamCenter = new Vector2((muzzleX + beamEnd) * 0.5f, (BeamBottom + BeamTop) * 0.5f);
        Vector2 beamSize = new Vector2(Mathf.Abs(beamEnd - muzzleX), BeamTop - BeamBottom);
        // The beam is one continuous strip. Moving energy is shaded along its length,
        // rather than repeating complete projectile silhouettes with gaps between them.
        beam.sprite = square;
        beam.flipX = false;
        beam.drawMode = SpriteDrawMode.Simple;
        Sprite beamFrame = firing && vfx != null ? TurretBossVfxSet.Frame(vfx.beam, elapsed * 3f, true) : null;
        bool continuous = firing && vfx != null && vfx.continuousBeam != null && beamFrame != null;
        beam.sharedMaterial = continuous ? vfx.continuousBeam : warningBeamMaterial;
        if (continuous)
        {
            beam.GetPropertyBlock(beamProperties);
            // Keep the original animated artwork; exclude each bolt's separate tail and tip.
            Rect frame = beamFrame.textureRect;
            Texture2D atlas = beamFrame.texture;
            beamProperties.SetTexture("_BeamAtlas", atlas);
            beamProperties.SetVector("_FrameRect", new Vector4(
                (frame.x + frame.width * 0.3f) / atlas.width, frame.y / atlas.height,
                frame.width * 0.4f / atlas.width, frame.height / atlas.height));
            beamProperties.SetFloat("_FlowTime", elapsed);
            beamProperties.SetFloat("_BeamLength", beamSize.x);
            beamProperties.SetFloat("_Direction", aimDirection);
            beam.SetPropertyBlock(beamProperties);
        }
        Display(beam, beamCenter, new Vector2(beamSize.x, charging ? 0.04f : beamSize.y), charging || firing);
        beam.color = charging ? new Color(1f, 0.2f, 0.2f, 0.5f) : continuous ? Color.white : Color.cyan;
        if (!hit && ((exploding && Overlaps(blastCenter, blastSize)) ||
            (firing && Overlaps(beamCenter, beamSize))))
        {
            hit = true;
            player.TakeDamage(damage);
        }
    }

    private void OnDisable() { Clear(); }

    private void Skin(SpriteRenderer renderer, Sprite[] frames, float progress, bool loop)
    {
        Sprite frame = TurretBossVfxSet.Frame(frames, progress, loop);
        if (frame == null) { return; }
        renderer.sprite = frame;
        renderer.color = Color.white;
    }

    public void Clear() { Clear(false); }

    private void Clear(bool preserveAftermath)
    {
        if (chargeBox != null) { chargeBox.enabled = false; }
        if (!preserveAftermath)
        {
            foreach (TurretBossVfxBurst effect in aftermath)
            {
                if (effect != null) { effect.gameObject.SetActive(false); Destroy(effect.gameObject); }
            }
            aftermath.Clear();
        }
        state = TurretBossAttackState.Defeated;
        missileLocked = false;
        foreach (FloorShot shot in shots)
        {
            if (shot.Visual != null) { Destroy(shot.Visual.gameObject); }
        }
        shots.Clear();
        foreach (SpriteRenderer renderer in new[] { beam, grenade, blast, marker, missile, leftWave, rightWave, launcherTube, dashWake })
        {
            if (renderer != null) { renderer.enabled = false; }
        }
    }
}
