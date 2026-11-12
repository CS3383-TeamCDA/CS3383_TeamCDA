using UnityEngine;

// Radial fire/smoke puff for airborne or player-contact detonations; no ground-facing base.
public sealed class EnemyAirBurst : MonoBehaviour
{
    private static Sprite puff;
    private readonly SpriteRenderer[] flames = new SpriteRenderer[9];
    private float elapsed;
    private float radius;
    public void Initialize(float blastRadius)
    {
        radius = Mathf.Max(.1f, blastRadius);
        if (puff == null)
        {
            var texture = new Texture2D(16, 16) { filterMode = FilterMode.Point };
            var pixels = new Color[256];
            for (int y = 0; y < 16; y++)
                for (int x = 0; x < 16; x++)
                    pixels[y * 16 + x] = Vector2.Distance(new Vector2(x, y), new Vector2(7.5f, 7.5f)) <= 7.5f
                        ? Color.white : Color.clear;
            texture.SetPixels(pixels); texture.Apply();
            puff = Sprite.Create(texture, new Rect(0, 0, 16, 16), Vector2.one * .5f, 16);
        }
        for (int i = 0; i < flames.Length; i++)
        {
            var child = new GameObject("Radial fire puff");
            child.transform.SetParent(transform, false);
            flames[i] = child.AddComponent<SpriteRenderer>();
            flames[i].sprite = puff;
            flames[i].sortingOrder = 45 + (i == 8 ? 1 : 0);
        }
        Draw();
    }
    private void Update()
    {
        elapsed += Time.deltaTime;
        if (elapsed >= .55f) { Destroy(gameObject); return; }
        Draw();
    }
    private void Draw()
    {
        float t = Mathf.Clamp01(elapsed / .55f);
        float spread = radius * .65f * Mathf.Sin(t * Mathf.PI * .5f);
        for (int i = 0; i < flames.Length; i++)
        {
            float angle = i * Mathf.PI / 4;
            bool core = i == 8;
            flames[i].transform.localPosition = core ? Vector3.zero :
                new Vector3(Mathf.Cos(angle) * spread, Mathf.Sin(angle) * spread + t * .2f);
            float size = radius * (core ? .9f : .7f) * (1 - t * .8f);
            flames[i].transform.localScale = Vector3.one * size;
            Color fire = core ? new Color(1, 1, .55f) : new Color(1, .35f + .25f * (i % 2), .03f);
            flames[i].color = Color.Lerp(fire, new Color(.3f, .25f, .35f), t);
            var color = flames[i].color; color.a = 1 - t; flames[i].color = color;
        }
    }
}
