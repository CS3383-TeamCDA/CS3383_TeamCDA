using UnityEngine;

/// <summary>Modular art assembly only. Does not drive combat, hitboxes or damage.</summary>
public sealed class TurretBossVisualRig : MonoBehaviour
{
    // Atlas order: dome, chassis, barrel, launcher, mount, core, shutters L/R,
    // planted base, upper leg, lower leg, foot.
    [SerializeField] private Sprite[] parts;
    [SerializeField] private Material damageMaterial;
    [SerializeField] private bool mobile;
    [SerializeField] private bool exposed;
    [SerializeField, Min(0.2f)] private float walkCycleSeconds = 1.5f;
    [SerializeField, Min(0f)] private float stepHeight = 0.28f;
    [SerializeField, Min(0.1f)] private float transformationSeconds = 2f;
    private const float PlantedBodyHeight = 1.32f;
    private const float MobileBodyHeight = 2.4f;
    [SerializeField, Min(0.05f)] private float shutterSlideSeconds = 0.3f;
    private bool cannonExternallyDriven;
    public float BodyHeightOffset { get; set; }
    public bool GroundedWalkingEnabled { get; set; } = true;
    private float shutterOpening;
    private float cannonClock = -1f;
    private float launcherClock = -1f;
    private SpriteRenderer muzzleEnergy;
    public bool IsCannonAnimating => cannonClock >= 0f;
    public bool IsLauncherAnimating => launcherClock >= 0f;
    private float deployment;
    private bool formInitialized;
    private bool baseParked;
    private Vector3 baseWorldPosition;
    private bool returningToBase;
    public bool IsMobile => mobile;
    public bool IsTransforming => returningToBase || (formInitialized && !Mathf.Approximately(deployment, mobile ? 1f : 0f));
    public float Deployment => deployment;
    private sealed class LegPose
    {
        public Transform Hip, Knee, Ankle, Upper, Lower;
        public Vector2 RestHip, RestAnkle;
        public float Side;
        public Vector3 PlantedWorld, LiftWorld, LandingWorld;
        public bool Swinging;
        public float SwingElapsed, SwingDuration;
    }
    private readonly LegPose[] legPoses = new LegPose[4];
    // One foot at a time: alternate across the body instead of hopping in pairs.
    private static readonly int[] StepOrder = { 0, 3, 1, 2 };
    private static readonly Vector2 UpperSource = new Vector2(-1.55f, -1.28f);
    private static readonly Vector2 LowerSource = new Vector2(-0.91f, -1.35f);
    private Vector3 previousPosition;
    private Vector3 previousScale;
    private float motionTime;
    private float stepClock;
    private int nextFoot;
    private bool combatMotionBound;
    private TurretBossAttackState combatMotion;
    private float combatElapsed;
    public void SyncCombatMotion(TurretBossAttackState state, float elapsed)
    {
        if (combatMotionBound && combatMotion != state &&
            (UsesAttackPose(combatMotion) || UsesAttackPose(state))) { ResetFooting(); }
        combatMotionBound = true;
        combatMotion = state;
        combatElapsed = elapsed;
    }
    private static bool UsesAttackPose(TurretBossAttackState state)
    {
        return state == TurretBossAttackState.LungeWarning || state == TurretBossAttackState.Lunging ||
            state == TurretBossAttackState.SpinWarning || state == TurretBossAttackState.Spinning ||
            state == TurretBossAttackState.StompWarning || state == TurretBossAttackState.Stomping;
    }
    private Transform turret;
    private Transform body;
    private Transform leftShutter;
    private Transform rightShutter;
    private GameObject plantedBase;
    private GameObject legs;
    public Transform Cannon { get; private set; }
    public Transform Launcher { get; private set; }
    public Transform Muzzle { get; private set; }
    public Transform LauncherMuzzle { get; private set; }
    public Transform FloorMuzzle { get; private set; }
    public Vector2 BodyCenter => body != null ? (Vector2)body.position : (Vector2)transform.position;

    public Vector2 LeadingFootPosition(float direction)
    {
        Vector2 position = transform.position;
        bool found = false;
        foreach (LegPose leg in legPoses)
        {
            if (leg == null) { continue; }
            Vector2 foot = leg.Ankle.position;
            if (!found || (foot.x - position.x) * direction > 0f) { position = foot; found = true; }
        }
        // Include the visible toe beyond the ankle joint, scaled with the assembly.
        position.x += direction * Mathf.Abs(transform.lossyScale.x) * 0.5f;
        position.y = transform.position.y;
        return position;
    }

    private void EnsureFloorPort()
    {
        if (plantedBase == null) { return; }
        Transform port = plantedBase.transform.Find("Low firing port");
        if (port == null) { port = Part(2, "Low firing port", plantedBase.transform, new Vector2(1.2f, 0.9f), 0.2f, 26); }
        FloorMuzzle = port.Find("Floor bolt muzzle");
        if (FloorMuzzle == null) { FloorMuzzle = Node("Floor bolt muzzle", port, new Vector2(2.72f, 0f)); }
    }
    public bool IsExposed => exposed;
    public bool IsDead { get; private set; }
    public bool DeathComplete => IsDead && deathElapsed >= 1.6f;
    public float HitFlash => Mathf.Clamp01(hitRemaining / 0.14f);
    private float healthFraction = 1f;
    private float hitRemaining;
    private float damageTime;
    private float sparkCountdown;
    private float deathElapsed;
    private Vector3 deathStartBody;
    private Quaternion deathStartRotation;
    private readonly Vector2[] deathHips = new Vector2[4];
    private readonly Vector2[] deathAnkles = new Vector2[4];
    private SpriteRenderer[] damageRenderers;
    private MaterialPropertyBlock damageProperties;
    private sealed class Spark
    {
        public SpriteRenderer Visual;
        public Vector3 Position, Velocity;
        public float Remaining, Lifetime;
    }
    private Spark[] sparks;


    private void Awake() { Build(); }

    // Script reloads preserve generated objects but can reset newly added runtime fields.
    private void OnEnable() { Build(); }

    public void SetForm(bool isMobile)
    {
        mobile = isMobile;
        returningToBase = false;
        deployment = mobile ? 1f : 0f;
        formInitialized = true;
        if (body == null) { return; }
        body.localPosition = new Vector3(0f, mobile ? MobileBodyHeight : PlantedBodyHeight, 0f);
        legs.SetActive(mobile);
        plantedBase.SetActive(!mobile);
        ResetFooting();
        SetAssemblyVisibility();
    }

    public void TransitionToForm(bool isMobile)
    {
        if (IsDead) { return; }
        if (body == null) { Build(); }
        if (!formInitialized) { SetForm(mobile); }
        mobile = isMobile;
        if (!baseParked)
        {
            baseWorldPosition = transform.position;
            baseParked = true;
        }
        returningToBase = !mobile && Vector3.Distance(transform.position, baseWorldPosition) > 0.03f;
        // A repeated/reversed request continues from the current deployment without snapping.
        if (legs != null) { legs.SetActive(true); }
        if (plantedBase != null) { plantedBase.SetActive(true); }
    }

    public void SetExposed(bool value)
    {
        exposed = value;
        shutterOpening = value ? 1f : 0f;
        ApplyShutters();
    }

    public void AnimateCore(bool value) { exposed = value; }

    public bool PlayCannonAttack()
    {
        if (IsDead || Cannon == null || IsCannonAnimating) { return false; }
        cannonExternallyDriven = false;
        cannonClock = 0f;
        return true;
    }

    public void SyncCannonTimeline(float seconds)
    {
        cannonExternallyDriven = true;
        cannonClock = seconds;
    }

    public void CancelWeaponAnimations()
    {
        cannonExternallyDriven = false;
        cannonClock = -1f;
        launcherClock = -1f;
        if (Cannon != null && Launcher != null) { AdvanceWeapons(0f); }
    }

    public bool PlayLauncherShot(bool immediate = false)
    {
        if (IsDead || Launcher == null || IsLauncherAnimating) { return false; }
        launcherClock = immediate ? 0.15f : 0f;
        return true;
    }

    private void ApplyShutters()
    {
        if (leftShutter == null || rightShutter == null) { return; }
        float opening = Mathf.SmoothStep(0f, 1f, shutterOpening);
        leftShutter.localPosition = new Vector3(Mathf.Lerp(-0.34f, -0.96f, opening), 0f, 0f);
        rightShutter.localPosition = new Vector3(Mathf.Lerp(0.34f, 0.96f, opening), 0f, 0f);
    }

    private void EnsureMuzzleEnergy()
    {
        if (Muzzle == null) { return; }
        Transform existing = Muzzle.Find("Cannon muzzle energy");
        Transform energy = existing != null ? existing : Part(5, "Cannon muzzle energy", Muzzle, Vector2.zero, 0.12f, 30);
        energy.localRotation = Quaternion.Euler(0f, 0f, 90f);
        muzzleEnergy = energy.GetComponent<SpriteRenderer>();
        muzzleEnergy.enabled = false;
    }

    private void AdvanceWeapons(float delta)
    {
        shutterOpening = Mathf.MoveTowards(shutterOpening, exposed ? 1f : 0f,
            delta / Mathf.Max(0.05f, shutterSlideSeconds));
        ApplyShutters();
        if (muzzleEnergy == null) { EnsureMuzzleEnergy(); }
        float recoil = 0f;
        float glow = 0f;
        if (IsCannonAnimating)
        {
            if (!cannonExternallyDriven) { cannonClock += delta; }
            if (cannonClock < 1.5f)
            {
                float charge = cannonClock / 1.5f;
                glow = charge * (0.55f + 0.15f * Mathf.Sin(cannonClock * 32f));
            }
            else if (cannonClock < 2.2f)
            {
                float fire = cannonClock - 1.5f;
                recoil = 0.22f * Mathf.Clamp01(fire / 0.04f) * Mathf.Exp(-fire * 6f);
                glow = 1f - 0.2f * Mathf.Sin(fire * 45f);
            }
            else { cannonClock = -1f; }
        }
        Cannon.localPosition = new Vector3(0.45f - recoil, 0.75f, 0f);
        if (muzzleEnergy != null)
        {
            muzzleEnergy.enabled = glow > 0.01f;
            muzzleEnergy.transform.localScale = Vector3.one * (0.08f + glow * 0.14f);
            muzzleEnergy.color = new Color(1f, 0.75f + glow * 0.25f, 0.75f + glow * 0.25f, Mathf.Clamp01(glow));
        }
        float launcherKick = 0f;
        float launcherBrace = 0f;
        if (IsLauncherAnimating)
        {
            launcherClock += delta;
            if (launcherClock < 0.15f) { launcherBrace = Mathf.SmoothStep(0f, 1f, launcherClock / 0.15f); }
            else if (launcherClock < 0.65f)
            {
                float fire = launcherClock - 0.15f;
                launcherBrace = Mathf.Exp(-fire * 7f);
                launcherKick = Mathf.Clamp01(fire / 0.025f) * Mathf.Exp(-fire * 10f);
            }
            else { launcherClock = -1f; }
        }
        Launcher.localPosition = new Vector3(-0.95f - launcherKick * 0.08f, 0.72f - launcherKick * 0.12f, 0f);
        Launcher.localRotation = Quaternion.Euler(0f, 0f, launcherBrace * -6f);
        Launcher.GetComponent<SpriteRenderer>().color = Color.Lerp(Color.white, new Color(1f, 0.85f, 0.45f), launcherKick * 0.55f);
    }

    private void Build()
    {
        if (body == null) { body = transform.Find("Body motion pivot"); }
        if (body != null)
        {
            RestoreRuntimeReferences();
            return;
        }
        if (parts == null || parts.Length != 12)
        {
            Debug.LogError("Siege Core visual rig requires all 12 atlas sprites.", this);
            return;
        }
        foreach (Sprite sprite in parts)
        {
            if (sprite == null) { Debug.LogError("Siege Core atlas sprite is missing.", this); return; }
        }
        body = Node("Body motion pivot", transform, Vector2.zero);
        Part(1, "Chassis", body, Vector2.zero, 1f, 20);
        Part(5, "Bare reactor", body, Vector2.zero, 0.58f, 21);
        leftShutter = Part(6, "Left core shutter", body, Vector2.zero, 0.62f, 22);
        rightShutter = Part(7, "Right core shutter", body, Vector2.zero, 0.62f, 22);
        turret = Node("Turret pivot", body, new Vector2(0f, 0.58f));
        Part(0, "Purple dome and optic", turret, Vector2.zero, 1f, 25);
        Cannon = Part(2, "Cannon recoil pivot", turret, new Vector2(0.45f, 0.75f), 0.78f, 26);
        Muzzle = Node("Beam muzzle", Cannon, new Vector2(2.72f, 0f));
        EnsureMuzzleEnergy();
        Part(4, "Launcher bracket", turret, new Vector2(-0.85f, 0.55f), 0.55f, 23);
        Launcher = Part(3, "Independent launcher pivot", turret, new Vector2(-0.95f, 0.72f), 0.65f, 24);
        LauncherMuzzle = Node("Payload muzzle", Launcher, new Vector2(0.35f, 1.45f));
        plantedBase = Part(8, "Planted base skirt", transform, Vector2.zero, 1.18f, 15).gameObject;
        EnsureFloorPort();
        legs = Node("Four articulated legs", transform, Vector2.zero).gameObject;
        for (int index = 0; index < 4; index++)
        {
            bool far = index < 2;
            float side = index % 2 == 0 ? -1f : 1f;
            Vector2 hipPosition = new Vector2(side * (far ? 0.75f : 1.1f), far ? 2.3f : 2.2f);
            // Far feet are inset and slightly raised for depth; near feet share the ground line.
            Vector2 anklePosition = new Vector2(side * (far ? 1.55f : 2.15f), far ? 0.58f : 0.46f);
            Vector2 upperSource = new Vector2(-1.55f, -1.28f);
            Vector2 lowerSource = new Vector2(-0.91f, -1.35f);
            const float legScale = 0.8f;
            Vector2 kneePosition = SolveKnee(hipPosition, anklePosition,
                upperSource.magnitude * legScale, lowerSource.magnitude * legScale, side);
            Transform hip = Node((far ? "Far" : "Near") + (side < 0 ? " left hip" : " right hip"),
                legs.transform, hipPosition);
            int order = far ? 10 : 31;
            Color tint = far ? new Color(0.6f, 0.65f, 0.78f) : Color.white;
            Transform upper = Part(9, "Upper leg", hip, Vector2.zero, legScale, order, tint);
            AlignLink(upper, upperSource, kneePosition - hipPosition, side, legScale);
            Transform knee = Node("Knee pivot", hip, kneePosition - hipPosition);
            Transform lower = Part(10, "Lower leg", knee, Vector2.zero, legScale, order + 1, tint);
            AlignLink(lower, lowerSource, anklePosition - kneePosition, side, legScale);
            Transform ankle = Node("Ankle pivot", knee, anklePosition - kneePosition);
            Transform foot = Part(11, "Foot", ankle, Vector2.zero, legScale * 0.65f, order + 2, tint);
            // Mirroring the artwork never tilts the sole: feet stay level independently of the shin.
            foot.localScale = new Vector3(-side * legScale * 0.65f, legScale * 0.65f, 1f);
            legPoses[index] = new LegPose
            {
                Hip = hip, Knee = knee, Ankle = ankle, Upper = upper, Lower = lower,
                RestHip = hipPosition, RestAnkle = anklePosition, Side = side
            };

        }
        SetForm(mobile);
        SetExposed(exposed);
    }

    private bool RestoreRuntimeReferences()
    {
        if (body == null) { return false; }
        turret = body.Find("Turret pivot");
        leftShutter = body.Find("Left core shutter");
        rightShutter = body.Find("Right core shutter");
        Transform legRoot = transform.Find("Four articulated legs");
        Transform baseRoot = transform.Find("Planted base skirt");
        if (turret == null || leftShutter == null || rightShutter == null ||
            legRoot == null || baseRoot == null || legRoot.childCount != 4) { return false; }
        legs = legRoot.gameObject;
        plantedBase = baseRoot.gameObject;
        EnsureFloorPort();
        Cannon = turret.Find("Cannon recoil pivot");
        Launcher = turret.Find("Independent launcher pivot");
        if (Cannon == null || Launcher == null) { return false; }
        Muzzle = Cannon.Find("Beam muzzle");
        LauncherMuzzle = Launcher.Find("Payload muzzle");
        EnsureMuzzleEnergy();
        for (int index = 0; index < 4; index++)
        {
            Transform hip = legRoot.GetChild(index);
            Transform knee = hip.Find("Knee pivot");
            Transform ankle = knee != null ? knee.Find("Ankle pivot") : null;
            Transform upper = hip.Find("Upper leg");
            Transform lower = knee != null ? knee.Find("Lower leg") : null;
            if (knee == null || ankle == null || upper == null || lower == null) { return false; }
            bool far = index < 2;
            float side = index % 2 == 0 ? -1f : 1f;
            legPoses[index] = new LegPose
            {
                Hip = hip, Knee = knee, Ankle = ankle, Upper = upper, Lower = lower, Side = side,
                RestHip = new Vector2(side * (far ? 0.75f : 1.1f), far ? 2.3f : 2.2f),
                RestAnkle = new Vector2(side * (far ? 1.55f : 2.15f), far ? 0.58f : 0.46f)
            };
        }
        ResetFooting();
        SetExposed(exposed);
        if (!formInitialized) { SetForm(mobile); }
        return true;
    }

    public void ResetFooting()
    {
        previousPosition = transform.position;
        previousScale = transform.lossyScale;
        stepClock = 0f;
        nextFoot = 0;
        foreach (LegPose leg in legPoses)
        {
            if (leg == null) { continue; }
            leg.PlantedWorld = transform.TransformPoint(leg.RestAnkle);
            leg.Swinging = false;
            leg.SwingElapsed = 0f;
            PoseLeg(leg, leg.RestHip, leg.RestAnkle);
        }
    }

    private void LateUpdate()
    {
        AdvancePresentation(Time.deltaTime);
        if (!IsDead && body != null && hitRemaining > 0f && Time.deltaTime > 0f)
        {
            body.localPosition += new Vector3(Mathf.Sin(damageTime * 95f) * 0.045f * HitFlash, 0f, 0f);
        }
    }

    public void AdvancePresentation(float delta)
    {
        if (delta <= 0f || float.IsNaN(delta) || float.IsInfinity(delta)) { return; }
        if (body == null) { Build(); }
        if (body == null) { return; }
        if (turret == null || legPoses[0] == null)
        {
            if (!RestoreRuntimeReferences())
            {
                Debug.LogError("Siege Core visual hierarchy is incomplete. Restart the art preview to rebuild it.", this);
                enabled = false;
                return;
            }
        }
        AdvanceDamageEffects(delta);
        if (IsDead)
        {
            AdvanceDeath(delta);
            return;
        }
        AdvanceWeapons(delta);
        if (returningToBase)
        {
            transform.position = Vector3.MoveTowards(transform.position, baseWorldPosition, 0.65f * delta);
            if (Vector3.Distance(transform.position, baseWorldPosition) < 0.001f)
            {
                returningToBase = false;
                ResetFooting();
            }
        }
        if (baseParked && plantedBase != null)
        {
            plantedBase.transform.position = baseWorldPosition;
        }
        if (Vector3.Distance(previousPosition, transform.position) > 2f || previousScale != transform.lossyScale)
        {
            ResetFooting();
        }
        Vector3 worldVelocity = (transform.position - previousPosition) / delta;
        Vector2 localVelocity = transform.InverseTransformVector(worldVelocity);
        previousPosition = transform.position;
        motionTime += delta;
        if (!formInitialized) { SetForm(mobile); }
        if (IsTransforming && !returningToBase)
        {
            AdvanceTransformation(delta);
            return;
        }
        if (combatMotionBound && mobile && UsesAttackPose(combatMotion))
        {
            PoseCombatAttack();
            return;
        }
        if (!GroundedWalkingEnabled) { ResetFooting(); }
        float speed = Mathf.Abs(localVelocity.x);
        bool walking = GroundedWalkingEnabled && (mobile || returningToBase) && speed > 0.03f;
        bool running = walking && speed > 1.5f;
        // Shorter cycles at speed; diagonal support pairs keep fast movement within leg reach.
        float cycle = walking ? Mathf.Min(walkCycleSeconds, Mathf.Max(0.12f, 0.8f / speed)) : walkCycleSeconds;
        cycle = Mathf.Max(0.12f, cycle);
        float swingSeconds = cycle * (running ? 0.42f : 0.21f);
        if (running)
        {
            stepClock = Mathf.Min(stepClock, cycle * 0.5f);
            foreach (LegPose leg in legPoses)
            {
                if (leg.Swinging) { leg.SwingDuration = Mathf.Min(leg.SwingDuration, swingSeconds); }
            }
        }
        float sway = Mathf.Sin(motionTime * 1.7f) * 0.035f;
        float bob = walking ? Mathf.Sin(motionTime / cycle * Mathf.PI * 4f) * 0.045f : Mathf.Sin(motionTime * 2.1f) * 0.018f;
        body.localPosition = new Vector3(sway, Mathf.Lerp(PlantedBodyHeight, MobileBodyHeight, deployment) + bob + BodyHeightOffset, 0f);
        body.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(motionTime * 1.7f) * (walking ? 0.7f : 0.35f));
        // Upper assembly lags the chassis slightly without moving either weapon off its mount.
        turret.localRotation = Quaternion.Euler(0f, 0f, -Mathf.Sin(motionTime * 1.7f - 0.25f) * 0.35f);
        if (!mobile && !returningToBase) { return; }
        stepClock -= delta;
        bool footInFlight = false;
        foreach (LegPose leg in legPoses) { footInFlight |= leg.Swinging; }
        if (walking && stepClock <= 0f && !footInFlight)
        {
            int stepIndex = running ? (nextFoot % 2) * 2 : nextFoot;
            StartStep(legPoses[StepOrder[stepIndex]], localVelocity.x, cycle, swingSeconds);
            if (running) { StartStep(legPoses[StepOrder[stepIndex + 1]], localVelocity.x, cycle, swingSeconds); }
            nextFoot = (nextFoot + 1) % (running ? 2 : 4);
            stepClock = cycle * (running ? 0.5f : 0.25f);
        }

        foreach (LegPose leg in legPoses)
        {
            Vector3 footWorld = leg.PlantedWorld;
            if (leg.Swinging)
            {
                leg.SwingElapsed += delta;
                float progress = Mathf.Clamp01(leg.SwingElapsed / Mathf.Max(0.01f, leg.SwingDuration));
                float eased = progress * progress * (3f - 2f * progress);
                footWorld = Vector3.Lerp(leg.LiftWorld, leg.LandingWorld, eased);
                footWorld += transform.up * (stepHeight * Mathf.Sin(progress * Mathf.PI));
                if (progress >= 1f)
                {
                    leg.PlantedWorld = leg.LandingWorld;
                    leg.Swinging = false;
                }
            }
            Vector2 hip = leg.RestHip + new Vector2(sway, bob + BodyHeightOffset);
            PoseLeg(leg, hip, transform.InverseTransformPoint(footWorld));
        }
    }

    private void StartStep(LegPose leg, float velocity, float cycle, float duration)
    {
        leg.Swinging = true;
        leg.SwingElapsed = 0f;
        leg.SwingDuration = duration;
        leg.LiftWorld = leg.PlantedWorld;
        Vector2 lead = new Vector2(Mathf.Clamp(velocity * cycle * 0.38f, -0.65f, 0.65f), 0f);
        leg.LandingWorld = transform.TransformPoint(leg.RestAnkle + lead);
    }

    private void PoseCombatAttack()
    {
        float height = 0f;
        float lean = 0f;
        float phase = 0f;
        bool spin = combatMotion == TurretBossAttackState.Spinning;
        bool dash = combatMotion == TurretBossAttackState.Lunging;
        if (combatMotion == TurretBossAttackState.LungeWarning)
        {
            float brace = Mathf.Clamp01(combatElapsed / TurretBossController.LungeWarningSeconds);
            height = -0.16f * brace;
            lean = -6f * brace;
        }
        else if (dash)
        {
            phase = Mathf.Clamp01(combatElapsed / TurretBossController.LungeSeconds);
            height = -0.12f + 0.12f * Mathf.Sin(phase * Mathf.PI);
            lean = 10f * Mathf.Sin(phase * Mathf.PI);
        }
        else if (combatMotion == TurretBossAttackState.SpinWarning)
        {
            height = -0.35f * Mathf.Clamp01(combatElapsed / TurretBossController.SpinWarningSeconds);
            lean = Mathf.Sin(combatElapsed * 18f) * 2f;
        }
        else if (spin) { height = -0.65f; }
        else if (combatMotion == TurretBossAttackState.StompWarning)
        {
            height = 0.22f * Mathf.SmoothStep(0f, 1f, combatElapsed / TurretBossController.StompWarningSeconds);
        }
        else if (combatMotion == TurretBossAttackState.Stomping)
        {
            height = -0.25f * Mathf.Exp(-combatElapsed * 9f);
        }
        body.localPosition = new Vector3(0f, MobileBodyHeight + BodyHeightOffset + height, 0f);
        body.localRotation = Quaternion.Euler(0f, 0f, lean);
        turret.localRotation = Quaternion.Euler(0f, 0f, -lean * 0.3f);
        for (int index = 0; index < legPoses.Length; index++)
        {
            LegPose leg = legPoses[index];
            Vector2 hip = leg.RestHip + Vector2.up * (height + BodyHeightOffset);
            Vector2 ankle = transform.InverseTransformPoint(leg.PlantedWorld);
            if (spin)
            {
                // A side-view rig cannot orbit four fixed-facing sprite limbs through the
                // chassis without crossing their joints. Keep each chain on its own side.
                bool far = index < 2;
                float angle = combatElapsed * Mathf.PI * 8f + (far ? Mathf.PI : 0f);
                float sweep = Mathf.Sin(angle);
                float reach = (far ? 2.5f : 3f) + sweep * (far ? 0.14f : 0.18f);
                Vector2 sweepTarget = new Vector2(leg.Side * reach,
                    (far ? 0.48f : 0.32f) + Mathf.Cos(angle) * 0.04f);
                float extend = Mathf.SmoothStep(0f, 1f, combatElapsed / 0.18f);
                ankle = Vector2.Lerp(leg.RestAnkle, sweepTarget, extend);
                // Hip stays attached to its original chassis socket; only knee/ankle sweep.
                // Far limbs retain their existing rear render order and muted palette.
            }
            else if (dash)
            {
                // A committed burst has tucked, trailing legs rather than a sliding idle stance.
                hip.x -= 0.15f;
                ankle = leg.RestAnkle + new Vector2(-0.35f * Mathf.Sin(phase * Mathf.PI),
                    0.22f * Mathf.Sin(phase * Mathf.PI));
            }
            PoseLeg(leg, hip, ankle);
        }
    }

    private void AdvanceTransformation(float delta)
    {
        deployment = Mathf.MoveTowards(deployment, mobile ? 1f : 0f,
            delta / Mathf.Max(0.1f, transformationSeconds));
        // Unfold the feet first, then push the chassis clear of its physical docking base.
        float lift = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.3f, 1f, deployment));
        // Brace before lifting and settle as the feet take the load.
        float brace = Mathf.Sin(deployment * Mathf.PI * 2f) * 0.06f;
        body.localPosition = new Vector3(0f, Mathf.Lerp(PlantedBodyHeight, MobileBodyHeight, lift) - brace + BodyHeightOffset, 0f);
        body.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(deployment * Mathf.PI * 2f) * 1.5f);
        turret.localRotation = Quaternion.Euler(0f, 0f, -Mathf.Sin(deployment * Mathf.PI * 2f));
        for (int index = 0; index < legPoses.Length; index++)
        {
            LegPose leg = legPoses[index];
            float progress = Mathf.InverseLerp(0.02f + index * 0.04f, 0.48f + index * 0.04f, deployment);
            progress = Mathf.SmoothStep(0f, 1f, progress);
            Vector2 foldedHip = new Vector2(leg.Side * 0.6f, 1.15f);
            Vector2 foldedAnkle = new Vector2(leg.Side * 1.05f, 0.65f);
            Vector2 plantedHip = leg.RestHip + Vector2.up * BodyHeightOffset - Vector2.up * ((MobileBodyHeight - PlantedBodyHeight) * (1f - lift));
            PoseLeg(leg, Vector2.Lerp(foldedHip, plantedHip, progress),
                Vector2.Lerp(foldedAnkle, leg.RestAnkle, progress));
        }
        SetAssemblyVisibility();
        if (!IsTransforming) { SetForm(mobile); }
    }

    private void SetAssemblyVisibility()
    {
        if (plantedBase == null || legs == null) { return; }
        legs.SetActive(deployment > 0f);
        plantedBase.SetActive(baseParked || deployment < 1f);
        // Keep the solid docking skirt on the floor. Nothing shrinks or dissolves.
        plantedBase.transform.position = baseParked ? baseWorldPosition : transform.position;
        // Keep the docking socket low enough to clear the bare reactor in the planted pose.
        // Preserve its footprint; only the vertical profile is reduced.
        plantedBase.transform.localScale = new Vector3(1.18f, 0.54f, 1f);
        SpriteRenderer skirt = plantedBase.GetComponent<SpriteRenderer>();
        Color baseColor = skirt.color;
        baseColor.a = 1f;
        skirt.color = baseColor;
        skirt.sortingOrder = 19; // Platform sits behind chassis/core (20–22), in front of the far legs (10–12).
        foreach (SpriteRenderer renderer in legs.GetComponentsInChildren<SpriteRenderer>(true))
        {
            Color color = renderer.color;
            color.a = 1f;
            renderer.color = color;
        }
    }

    public void SetHealthFraction(float fraction) { healthFraction = Mathf.Clamp01(fraction); }

    public void PlayDamageFeedback(float remainingFraction)
    {
        if (IsDead || body == null) { return; }
        SetHealthFraction(remainingFraction);
        hitRemaining = 0.14f;
        EnsureDamageEffects();
        EmitSparks(7);
    }

    public void BeginDeath()
    {
        if (IsDead || body == null) { return; }
        IsDead = true;
        deathElapsed = 0f;
        CancelWeaponAnimations();
        deathStartBody = body.localPosition;
        deathStartRotation = body.localRotation;
        for (int i = 0; i < legPoses.Length; i++)
        {
            deathHips[i] = legPoses[i].Hip.localPosition;
            deathAnkles[i] = transform.InverseTransformPoint(legPoses[i].Ankle.position);
        }
        EnsureDamageEffects();
        EmitSparks(12);
    }

    private void AdvanceDeath(float delta)
    {
        deathElapsed += delta;
        float collapse = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.22f, 1.35f, deathElapsed));
        float settle = deathElapsed > 1.35f ? Mathf.Sin((deathElapsed - 1.35f) * 20f) * Mathf.Exp(-(deathElapsed - 1.35f) * 10f) * 0.025f : 0f;
        body.localPosition = Vector3.Lerp(deathStartBody, new Vector3(0.12f, mobile ? 1.08f : 1.02f, 0f), collapse) + Vector3.up * settle;
        body.localRotation = Quaternion.Lerp(deathStartRotation, Quaternion.Euler(0f, 0f, -7f), collapse);
        turret.localRotation = Quaternion.Euler(0f, 0f, -collapse * 5f);
        Cannon.localRotation = Quaternion.Euler(0f, 0f, -collapse * 18f);
        Launcher.localRotation = Quaternion.Euler(0f, 0f, collapse * 15f);
        shutterOpening = Mathf.MoveTowards(shutterOpening, 1f, delta * 4f);
        ApplyShutters();
        if (!mobile) { return; }
        for (int i = 0; i < legPoses.Length; i++)
        {
            LegPose leg = legPoses[i];
            Vector2 hip = Vector2.Lerp(deathHips[i], new Vector2(leg.Side * 1.05f, 0.96f), collapse);
            Vector2 ankle = Vector2.Lerp(deathAnkles[i], new Vector2(leg.Side * 1.25f, leg.RestAnkle.y), collapse);
            PoseLeg(leg, hip, ankle);
        }
    }

    private void EnsureDamageEffects()
    {
        if (damageRenderers != null && damageProperties != null && sparks != null) { return; }
        var renderers = new System.Collections.Generic.List<SpriteRenderer>();
        foreach (SpriteRenderer renderer in GetComponentsInChildren<SpriteRenderer>(true))
        {
            if (!renderer.name.StartsWith("Damage spark ")) { renderers.Add(renderer); }
        }
        damageRenderers = renderers.ToArray();
        damageProperties = new MaterialPropertyBlock();
        foreach (SpriteRenderer renderer in damageRenderers)
        {
            if (damageMaterial != null && renderer.gameObject != plantedBase) { renderer.sharedMaterial = damageMaterial; }
        }
        sparks = new Spark[12];
        for (int i = 0; i < sparks.Length; i++)
        {
            Transform chip = transform.Find("Damage spark " + i);
            if (chip == null) { chip = Part(5, "Damage spark " + i, transform, Vector2.zero, 0.025f, 70); }
            SpriteRenderer visual = chip.GetComponent<SpriteRenderer>();
            visual.enabled = false;
            sparks[i] = new Spark { Visual = visual };
        }
    }

    private void EmitSparks(int count)
    {
        if (sparks == null || body == null) { return; }
        for (int i = 0; i < count; i++)
        {
            Spark spark = sparks[i % sparks.Length];
            float angle = (i * 137.5f + damageTime * 91f) * Mathf.Deg2Rad;
            spark.Position = body.position + transform.TransformVector(new Vector3(Mathf.Cos(angle) * 0.35f, 0.15f, 0f));
            spark.Velocity = new Vector3(Mathf.Cos(angle) * 1.5f, 0.9f + Mathf.Abs(Mathf.Sin(angle)) * 1.2f, 0f);
            spark.Remaining = spark.Lifetime = 0.25f + (i % 4) * 0.07f;
            spark.Visual.enabled = true;
            spark.Visual.transform.position = spark.Position;
        }
    }

    private void AdvanceDamageEffects(float delta)
    {
        EnsureDamageEffects();
        damageTime += delta;
        hitRemaining = Mathf.Max(0f, hitRemaining - delta);
        sparkCountdown -= delta;
        float damage = 1f - healthFraction;
        if (!IsDead && healthFraction < 0.75f && sparkCountdown <= 0f)
        {
            int count = healthFraction <= 0.25f ? 5 : healthFraction <= 0.5f ? 3 : 1;
            EmitSparks(count);
            sparkCountdown = healthFraction > 0.5f
                ? Mathf.Lerp(1.2f, 2.5f, (healthFraction - 0.5f) / 0.25f)
                : Mathf.Lerp(0.18f, 1.2f, healthFraction / 0.5f);
        }
        // Persistent surface damage begins with the first hit, not just at critical HP.
        float wear = Mathf.Pow(damage, 0.8f);
        float instability = Mathf.Clamp01((damage - 0.2f) / 0.8f);
        float pulse = Mathf.Sin(damageTime * 31f) * Mathf.Sin(damageTime * 7f);
        float flicker = pulse > Mathf.Lerp(0.98f, 0.3f, instability)
            ? Mathf.Lerp(0f, 0.95f, instability) : 0f;
        float shutdown = IsDead ? Mathf.SmoothStep(0f, 0.7f, Mathf.InverseLerp(0.25f, 1.4f, deathElapsed)) : 0f;
        foreach (SpriteRenderer renderer in damageRenderers)
        {
            if (renderer == null || renderer.gameObject == plantedBase || renderer == muzzleEnergy) { continue; }
            float flare = IsDead && deathElapsed < 0.22f ? (renderer.name == "Bare reactor" ? 1f : 0.35f) : 0f;
            renderer.GetPropertyBlock(damageProperties);
            damageProperties.SetFloat("_FlashAmount", Mathf.Max(HitFlash, flare));
            damageProperties.SetFloat("_Wear", wear);
            damageProperties.SetFloat("_Shutdown", shutdown);
            damageProperties.SetFloat("_PowerLoss", IsDead ? Mathf.Clamp01((deathElapsed - 0.2f) / 0.6f) : flicker);
            renderer.SetPropertyBlock(damageProperties);
        }
        foreach (Spark spark in sparks)
        {
            if (spark.Remaining <= 0f) { continue; }
            spark.Remaining = Mathf.Max(0f, spark.Remaining - delta);
            spark.Velocity += Vector3.down * (5f * delta);
            spark.Position += spark.Velocity * delta;
            spark.Visual.transform.position = spark.Position;
            spark.Visual.transform.Rotate(0f, 0f, 240f * delta);
            spark.Visual.color = new Color(1f, 0.75f, 0.25f, spark.Remaining / spark.Lifetime);
            spark.Visual.enabled = spark.Remaining > 0f;
        }
    }

    private static void PoseLeg(LegPose leg, Vector2 hip, Vector2 ankle)
    {
        const float scale = 0.8f;
        Vector2 knee = SolveKnee(hip, ankle, UpperSource.magnitude * scale, LowerSource.magnitude * scale, leg.Side);
        leg.Hip.localPosition = hip;
        leg.Knee.localPosition = knee - hip;
        leg.Ankle.localPosition = ankle - knee;
        AlignLink(leg.Upper, UpperSource, knee - hip, leg.Side, scale);
        AlignLink(leg.Lower, LowerSource, ankle - knee, leg.Side, scale);
    }

    private static Vector2 SolveKnee(Vector2 hip, Vector2 ankle, float upperLength, float lowerLength, float side)
    {
        Vector2 delta = ankle - hip;
        float distance = Mathf.Clamp(delta.magnitude, 0.001f, upperLength + lowerLength - 0.001f);
        Vector2 direction = delta.normalized;
        float along = (upperLength * upperLength - lowerLength * lowerLength + distance * distance) / (2f * distance);
        float bend = Mathf.Sqrt(Mathf.Max(0f, upperLength * upperLength - along * along));
        Vector2 outward = new Vector2(-direction.y, direction.x) * side;
        return hip + direction * along + outward * bend;
    }

    private static void AlignLink(Transform visual, Vector2 source, Vector2 destination, float side, float scale)
    {
        Vector2 mirrored = new Vector2(source.x * -side, source.y);
        float angle = Mathf.Atan2(destination.y, destination.x) - Mathf.Atan2(mirrored.y, mirrored.x);
        visual.localScale = new Vector3(-side * scale, scale, 1f);
        visual.localRotation = Quaternion.Euler(0f, 0f, angle * Mathf.Rad2Deg);
    }

    private static Transform Node(string label, Transform parent, Vector2 position)
    {
        Transform node = new GameObject(label).transform;
        node.SetParent(parent, false);
        node.localPosition = position;
        return node;
    }

    private Transform Part(int index, string label, Transform parent, Vector2 position, float scale,
        int order, Color? tint = null)
    {
        Transform node = Node(label, parent, position);
        node.localScale = Vector3.one * scale;
        SpriteRenderer renderer = node.gameObject.AddComponent<SpriteRenderer>();
        renderer.sprite = parts[index];
        renderer.sortingOrder = order;
        renderer.color = tint ?? Color.white;
        return node;
    }
}
