using System.Collections;
using EscapeThe90s.Contracts;
using UnityEngine;

public class TurretHazard : EnemyBase
{
    [SerializeField] private Transform target;
    [SerializeField] private Vector2 targetAimOffset = new Vector2(0, .9f);
    private EnemyRangedAttack shooter;
    private EnemyAnimationDriver animationDriver;
    private static Sprite smokeSprite;
    public void SetTarget(Transform player) { target = player; }
    public override Vector3 AttackTargetPosition => base.AttackTargetPosition + (Vector3)targetAimOffset;

    protected override void Awake()
    {
        base.Awake();
        shooter = GetComponent<EnemyRangedAttack>();
        animationDriver = GetComponent<EnemyAnimationDriver>();
    }

    protected override void Update()
    {
        base.Update();
        if (TryAcquireTarget(target) && shooter != null && animationDriver != null &&
            TryGetShotDirection(shooter.ShotOrigin, AttackTargetPosition, out var direction))
            animationDriver.SetAimDirection(direction);
    }

    public override bool TakeDamage(int amount)
    {
        bool wasDefeated = IsDefeated;
        bool defeated = base.TakeDamage(amount);
        if (defeated && !wasDefeated && isActiveAndEnabled)
            StartCoroutine(SmokeWreck());
        return defeated;
    }

    private IEnumerator SmokeWreck()
    {
        yield return new WaitForSeconds(.5f);
        var wreck = GetComponent<SpriteRenderer>();
        var puffs = new SpriteRenderer[5];
        for (int i = 0; i < puffs.Length; i++)
        {
            var child = new GameObject("Wreck smoke puff");
            child.transform.SetParent(transform, false);
            puffs[i] = child.AddComponent<SpriteRenderer>();
            puffs[i].sprite = GetSmokeSprite();
            puffs[i].flipX = i % 2 != 0;
            if (wreck != null)
            {
                puffs[i].sortingLayerID = wreck.sortingLayerID;
                puffs[i].sortingOrder = wreck.sortingOrder + 1;
            }
        }

        float elapsed = 0f;
        while (true)
        {
            for (int i = 0; i < puffs.Length; i++)
            {
                float age = elapsed - i * .26f;
                float t = Mathf.Floor(Mathf.Repeat(age, 1.3f) / 1.3f * 16f) / 16f;
                var puff = puffs[i];
                puff.enabled = age >= 0f;
                float drift = Mathf.Sin(t * 3f + i * 1.7f) * t * .13f;
                puff.transform.localPosition = new Vector3(drift, .34f + .72f * t);
                puff.transform.localScale = Vector3.one * Mathf.Lerp(.08f, .24f, t);
                puff.color = new Color(1f, 1f, 1f, .85f * (1f - t));
            }
            yield return null;
            elapsed += Time.deltaTime;
        }
    }

    private static Sprite GetSmokeSprite()
    {
        if (smokeSprite != null) return smokeSprite;

        string[] rows =
        {
            "....222..",
            "..223332.",
            ".2333332.",
            "233332221",
            "233222211",
            ".2222111.",
            "..1111..."
        };
        var palette = new Color32[]
        {
            new Color32(0, 0, 0, 0),
            new Color32(45, 40, 57, 255),
            new Color32(78, 69, 91, 255),
            new Color32(121, 113, 137, 255)
        };
        var texture = new Texture2D(9, 7, TextureFormat.RGBA32, false)
        {
            filterMode = FilterMode.Point
        };
        var pixels = new Color32[9 * 7];
        for (int y = 0; y < rows.Length; y++)
            for (int x = 0; x < rows[y].Length; x++)
                pixels[(rows.Length - 1 - y) * 9 + x] = palette[rows[y][x] == '.' ? 0 : rows[y][x] - '0'];
        texture.SetPixels32(pixels);
        texture.Apply(false, true);
        smokeSprite = Sprite.Create(texture, new Rect(0, 0, 9, 7), Vector2.one * .5f, 9, 0, SpriteMeshType.FullRect);
        return smokeSprite;
    }
    public override EnemyType Type => EnemyType.Turret;
    public override EnemyMovementType MovementType => EnemyMovementType.Static;

    protected override bool CalculateShotDirection(Vector2 origin, Vector2 target, out Vector2 direction)
    {
        Vector2 offset = target - origin;
        direction = Vector2.zero;
        // World-space upper semicircle: both horizontal endpoints are included.
        if (offset.y < 0 || offset.sqrMagnitude < 0.000001f) return false;
        float angle = Mathf.Atan2(offset.y, offset.x) * Mathf.Rad2Deg;
        // Midpoints round toward the larger angle (22.5 -> 45, for example).
        int slot = Mathf.Clamp(Mathf.FloorToInt(angle / 45f + 0.5f), 0, 4);
        switch (slot)
        {
            case 0: direction = Vector2.right; break;
            case 1: direction = new Vector2(1, 1).normalized; break;
            case 2: direction = Vector2.up; break;
            case 3: direction = new Vector2(-1, 1).normalized; break;
            default: direction = Vector2.left; break;
        }
        return true;
    }

    public override void Move()
    {
        // Turrets remain fixed at their generated terrain location.
    }

}
