using UnityEngine;

// Visual-only playback. Combat owns hit timing, launch, recovery and facing.
[RequireComponent(typeof(TVClawEnemy), typeof(SpriteRenderer))]
public sealed class TVClawArtDriver : MonoBehaviour
{
    [SerializeField] private TVClawArtSet art;
    private TVClawEnemy enemy;
    private SpriteRenderer visual;
    private Rigidbody2D body;
    private float clock, deathTime, hitTime = -10;
    private bool dead;
    public TVClawArtSet Art => art;

    private void Awake()
    {
        enemy = GetComponent<TVClawEnemy>();
        visual = GetComponent<SpriteRenderer>();
        body = GetComponent<Rigidbody2D>();
        // Old placeholder Animator must never overwrite the new atlas frames.
        var animator = GetComponent<Animator>();
        if (animator != null) animator.enabled = false;
        Render(0);
    }
    public void ShowHit() { hitTime = clock; }
    private void LateUpdate() { Render(Time.deltaTime); }
    public void Render(float delta)
    {
        if (art == null || visual == null || enemy == null) return;
        clock += Mathf.Max(0, delta);
        Sprite frame;
        if (enemy.IsDefeated)
        {
            if (!dead) { dead = true; deathTime = clock; }
            frame = TVClawArtSet.Frame(art.death, (clock - deathTime) / .85f);
        }
        else
        {
            float t = enemy.CombatElapsed;
            switch (enemy.CombatState)
            {
                case TVClawCombatState.SwipeWindup:
                    frame = TVClawArtSet.Frame(art.swipe, Mathf.Lerp(0, .49f, Mathf.Clamp01(t / Mathf.Max(.01f, enemy.SwipeWindup))));
                    break;
                case TVClawCombatState.BiteWindup:
                    frame = TVClawArtSet.Frame(art.bite, Mathf.Lerp(0, .49f, Mathf.Clamp01(t / Mathf.Max(.01f, enemy.BiteWindup))));
                    break;
                case TVClawCombatState.PounceWindup:
                    frame = TVClawArtSet.Frame(art.pounce, Mathf.Lerp(0, .36f, Mathf.Clamp01(t / Mathf.Max(.01f, enemy.PounceWindup))));
                    break;
                case TVClawCombatState.Pouncing:
                    frame = TVClawArtSet.Frame(art.pounce, Mathf.Lerp(.38f, .7f, Mathf.Clamp01(t / Mathf.Max(.01f, enemy.PounceDuration))));
                    break;
                case TVClawCombatState.Recovery:
                    var sequence = enemy.LastAttack == TVClawCombatState.BiteWindup ? art.bite :
                        enemy.LastAttack == TVClawCombatState.PounceWindup ? art.pounce : art.swipe;
                    float start = enemy.LastAttack == TVClawCombatState.PounceWindup ? .75f : .5f;
                    frame = TVClawArtSet.Frame(sequence, Mathf.Lerp(start, 1, Mathf.Clamp01(t / .8f)));
                    break;
                default:
                    bool moving = body != null && Mathf.Abs(body.linearVelocity.x) > .1f;
                    frame = clock - hitTime < .3f ? TVClawArtSet.Frame(art.hit, (clock - hitTime) / .3f) :
                        TVClawArtSet.Frame(moving ? art.run : art.idle, clock * (moving ? 1.6f : .8f), true);
                    break;
            }
        }
        if (frame != null) visual.sprite = frame;
    }
}
