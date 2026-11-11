using UnityEngine;

/// <summary>Art frames only; never controls attack timing or damage.</summary>
[CreateAssetMenu(menuName = "Escape the 90s/TL4/Siege Core VFX")]
public sealed class TurretBossVfxSet : ScriptableObject
{
    public Material continuousBeam;
    public Sprite[] beam;
    public Sprite[] grenades;
    public Sprite[] rockets;
    public Sprite[] explosions;
    public Sprite[] shockwaves;

    public static Sprite Frame(Sprite[] frames, float progress, bool loop)
    {
        if (frames == null || frames.Length == 0) { return null; }
        float position = loop ? Mathf.Repeat(progress, 1f) : Mathf.Clamp01(progress);
        return frames[Mathf.Clamp(Mathf.FloorToInt(position * frames.Length), 0, frames.Length - 1)];
    }
}
