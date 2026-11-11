using UnityEngine;

/// <summary>Non-damaging impact aftermath. Scaled time freezes it with the fight.</summary>
public sealed class TurretBossVfxBurst : MonoBehaviour
{
    private Sprite[] frames;
    private SpriteRenderer visual;
    private Vector2 size;
    private float elapsed;
    private float lifetime;

    public void Initialize(Sprite[] sequence, Vector2 dimensions, float seconds, bool flipX = false)
    {
        frames = sequence;
        size = dimensions;
        lifetime = Mathf.Max(0.01f, seconds);
        visual = gameObject.AddComponent<SpriteRenderer>();
        visual.sortingOrder = 45;
        visual.flipX = flipX;
        Draw();
    }

    private void Update()
    {
        if (Time.deltaTime <= 0f) { return; }
        elapsed += Time.deltaTime;
        if (elapsed >= lifetime) { Destroy(gameObject); return; }
        Draw();
    }

    private void Draw()
    {
        Sprite sprite = TurretBossVfxSet.Frame(frames, elapsed / lifetime, false);
        visual.sprite = sprite;
        if (sprite == null) { visual.enabled = false; return; }
        Vector2 bounds = sprite.bounds.size;
        transform.localScale = new Vector3(size.x / Mathf.Max(0.001f, bounds.x), size.y / Mathf.Max(0.001f, bounds.y), 1f);
    }
}
