using UnityEngine;

/// <summary>Reusable sandbox target; deliberately independent of the team's enemy systems.</summary>
public sealed class SandboxTargetDummy : MonoBehaviour
{
    public int Health { get; private set; } = 100;
    public int Hits { get; private set; }
    SpriteRenderer visual;
    float flash, respawn;
    Color baseColor;
    public void Initialize(SpriteRenderer renderer) { visual = renderer; baseColor = renderer.color; }
    public void Hit(int damage)
    {
        if (Health <= 0) return;
        Health = Mathf.Max(0, Health - damage); Hits++; flash = .12f;
        if (Health == 0) respawn = 1.5f;
    }
    void Update()
    {
        if (visual == null) return;
        flash = Mathf.Max(0, flash - Time.deltaTime);
        if (Health == 0)
        {
            respawn -= Time.deltaTime;
            if (respawn <= 0) Health = 100;
        }
        visual.color = Health == 0 ? new Color(baseColor.r, baseColor.g, baseColor.b, .2f) : flash > 0 ? Color.white : baseColor;
    }
    public void ResetTarget() { Health = 100; Hits = 0; flash = respawn = 0; }
}
