using UnityEngine;

/// <summary>Temporary articulated leg silhouettes; replace with production art after combat tuning.</summary>
public sealed class TurretBossLegRig : MonoBehaviour
{
    private readonly SpriteRenderer[] segments = new SpriteRenderer[8];
    private Sprite square;
    private bool presentationVisible = true;
    public bool PresentationVisible
    {
        get => presentationVisible;
        set
        {
            presentationVisible = value;
            if (!value) { foreach (SpriteRenderer segment in segments) { if (segment != null) { segment.enabled = false; } } }
        }
    }

    private void Awake()
    {
        square = Sprite.Create(Texture2D.whiteTexture, new Rect(0f, 0f, Texture2D.whiteTexture.width,
            Texture2D.whiteTexture.height), new Vector2(0.5f, 0.5f), Texture2D.whiteTexture.width);
        for (int index = 0; index < segments.Length; index++)
        {
            GameObject segment = new GameObject("Prototype leg segment " + index);
            segment.transform.SetParent(transform);
            segments[index] = segment.AddComponent<SpriteRenderer>();
            segments[index].sprite = square;
            segments[index].color = new Color(0.15f, 0.65f, 0.75f);
            segments[index].sortingOrder = 9;
            segments[index].enabled = false;
        }
    }

    public void SetPose(float deployment, float stride)
    {
        for (int leg = 0; leg < 4; leg++)
        {
            float side = leg < 2 ? -1f : 1f;
            float spacing = leg % 2 == 0 ? 0.45f : 1f;
            Vector2 hip = (Vector2)transform.position + new Vector2(side * spacing, 0.2f);
            Vector2 foot = new Vector2(transform.position.x + side * (spacing + 0.8f) +
                stride * (leg % 2 == 0 ? 0.15f : -0.15f), 0.08f);
            Vector2 knee = new Vector2(transform.position.x + side * (spacing + 0.5f),
                Mathf.Max(0.35f, transform.position.y * 0.5f));
            foot = Vector2.Lerp(hip, foot, deployment);
            knee = Vector2.Lerp(hip, knee, deployment);
            Segment(segments[leg * 2], hip, knee, deployment > 0f);
            Segment(segments[leg * 2 + 1], knee, foot, deployment > 0f);
        }
    }

    public void SetLongChargePose(float progress)
    {
        for (int leg = 0; leg < 4; leg++)
        {
            float angle = progress * Mathf.PI * 6f + leg * Mathf.PI * 0.5f;
            Vector2 hip = (Vector2)transform.position + new Vector2(Mathf.Cos(angle) * 0.5f, -0.25f);
            Vector2 knee = new Vector2(transform.position.x + Mathf.Cos(angle + 0.4f) * 1.2f, 0.4f);
            Vector2 foot = new Vector2(transform.position.x + Mathf.Cos(angle + 0.7f) * 1.8f, 0.2f + Mathf.Sin(angle) * 0.08f);
            Segment(segments[leg * 2], hip, knee, true);
            Segment(segments[leg * 2 + 1], knee, foot, true);
        }
    }

    private void Segment(SpriteRenderer renderer, Vector2 start, Vector2 end, bool visible)
    {
        if (renderer == null) { return; }
        Vector2 offset = end - start;
        renderer.enabled = visible && presentationVisible;
        renderer.transform.position = (start + end) * 0.5f;
        renderer.transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(offset.y, offset.x) * Mathf.Rad2Deg);
        renderer.transform.localScale = new Vector3(offset.magnitude / Mathf.Abs(transform.lossyScale.x),
            0.15f / Mathf.Abs(transform.lossyScale.y), 1f);
    }

    private void OnDestroy()
    {
        if (square != null) { Destroy(square); }
    }
}
