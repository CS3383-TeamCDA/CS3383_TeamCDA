using UnityEngine;

[CreateAssetMenu(menuName = "Escape the 90s/TL4/TV Claw Art")]
public sealed class TVClawArtSet : ScriptableObject
{
    public Sprite[] idle, run, swipe, bite, pounce, hit, death;
    public static Sprite Frame(Sprite[] frames, float progress, bool loop = false)
    {
        if (frames == null || frames.Length == 0) return null;
        float t = loop ? Mathf.Repeat(progress, 1) : Mathf.Clamp01(progress);
        return frames[Mathf.Min(frames.Length - 1, Mathf.FloorToInt(t * frames.Length))];
    }
}
