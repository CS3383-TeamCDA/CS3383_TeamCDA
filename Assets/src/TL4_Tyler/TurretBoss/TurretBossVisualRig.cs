using UnityEngine;

/// <summary>Modular art assembly only. Does not drive combat, hitboxes or damage.</summary>
public sealed class TurretBossVisualRig : MonoBehaviour
{
    // Atlas order: dome, chassis, barrel, launcher, mount, core, shutters L/R,
    // planted base, upper leg, lower leg, foot.
    [SerializeField] private Sprite[] parts;
    [SerializeField] private bool mobile;
    [SerializeField] private bool exposed;
    [SerializeField, Min(0.2f)] private float walkCycleSeconds = 1.5f;
    [SerializeField, Min(0f)] private float stepHeight = 0.28f;
    private sealed class LegPose
    {
        public Transform Hip, Knee, Ankle, Upper, Lower;
        public Vector2 RestHip, RestAnkle;
        public float Side;
        public Vector3 PlantedWorld, LiftWorld, LandingWorld;
        public bool Swinging;
        public float SwingElapsed;
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
    public bool IsExposed => exposed;

    private void Awake() { Build(); }

    // Script reloads preserve generated objects but can reset newly added runtime fields.
    private void OnEnable() { Build(); }

    public void SetForm(bool isMobile)
    {
        mobile = isMobile;
        if (body == null) { return; }
        body.localPosition = new Vector3(0f, mobile ? 2.4f : 1.5f, 0f);
        legs.SetActive(mobile);
        plantedBase.SetActive(!mobile);
        ResetFooting();
    }

    public void SetExposed(bool value)
    {
        exposed = value;
        if (leftShutter == null) { return; }
        // Reactor is unobstructed when these two solid plates slide aside.
        leftShutter.localPosition = new Vector3(exposed ? -0.96f : -0.34f, 0f, 0f);
        rightShutter.localPosition = new Vector3(exposed ? 0.96f : 0.34f, 0f, 0f);
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
        Part(4, "Launcher bracket", turret, new Vector2(-0.85f, 0.55f), 0.55f, 23);
        Launcher = Part(3, "Independent launcher pivot", turret, new Vector2(-0.95f, 0.72f), 0.65f, 24);
        LauncherMuzzle = Node("Payload muzzle", Launcher, new Vector2(0.35f, 1.45f));
        plantedBase = Part(8, "Planted base skirt", transform, Vector2.zero, 1.18f, 15).gameObject;
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
        Cannon = turret.Find("Cannon recoil pivot");
        Launcher = turret.Find("Independent launcher pivot");
        if (Cannon == null || Launcher == null) { return false; }
        Muzzle = Cannon.Find("Beam muzzle");
        LauncherMuzzle = Launcher.Find("Payload muzzle");
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

    private void LateUpdate() { AdvancePresentation(Time.deltaTime); }

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
        if (Vector3.Distance(previousPosition, transform.position) > 2f || previousScale != transform.lossyScale)
        {
            ResetFooting();
        }
        Vector3 worldVelocity = (transform.position - previousPosition) / delta;
        Vector2 localVelocity = transform.InverseTransformVector(worldVelocity);
        previousPosition = transform.position;
        motionTime += delta;
        bool walking = mobile && Mathf.Abs(localVelocity.x) > 0.03f;
        float cycle = Mathf.Max(0.2f, walkCycleSeconds);
        float swingSeconds = cycle * 0.21f;
        float sway = Mathf.Sin(motionTime * 1.7f) * 0.035f;
        float bob = walking ? Mathf.Sin(motionTime / cycle * Mathf.PI * 4f) * 0.045f : Mathf.Sin(motionTime * 2.1f) * 0.018f;
        body.localPosition = new Vector3(sway, (mobile ? 2.4f : 1.5f) + bob, 0f);
        body.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(motionTime * 1.7f) * (walking ? 0.7f : 0.35f));
        // Upper assembly lags the chassis slightly without moving either weapon off its mount.
        turret.localRotation = Quaternion.Euler(0f, 0f, -Mathf.Sin(motionTime * 1.7f - 0.25f) * 0.35f);
        if (!mobile) { return; }
        stepClock -= delta;
        bool footInFlight = false;
        foreach (LegPose leg in legPoses) { footInFlight |= leg.Swinging; }
        if (walking && stepClock <= 0f && !footInFlight)
        {
            LegPose leg = legPoses[StepOrder[nextFoot]];
            nextFoot = (nextFoot + 1) % 4;
            leg.Swinging = true;
            leg.SwingElapsed = 0f;
            leg.LiftWorld = leg.PlantedWorld;
            Vector2 lead = new Vector2(Mathf.Clamp(localVelocity.x * cycle * 0.38f, -0.65f, 0.65f), 0f);
            leg.LandingWorld = transform.TransformPoint(leg.RestAnkle + lead);
            stepClock = cycle * 0.25f;
        }
        foreach (LegPose leg in legPoses)
        {
            Vector3 footWorld = leg.PlantedWorld;
            if (leg.Swinging)
            {
                leg.SwingElapsed += delta;
                float progress = Mathf.Clamp01(leg.SwingElapsed / swingSeconds);
                float eased = progress * progress * (3f - 2f * progress);
                footWorld = Vector3.Lerp(leg.LiftWorld, leg.LandingWorld, eased);
                footWorld += transform.up * (stepHeight * Mathf.Sin(progress * Mathf.PI));
                if (progress >= 1f)
                {
                    leg.PlantedWorld = leg.LandingWorld;
                    leg.Swinging = false;
                }
            }
            Vector2 hip = leg.RestHip + new Vector2(sway, bob);
            PoseLeg(leg, hip, transform.InverseTransformPoint(footWorld));
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
