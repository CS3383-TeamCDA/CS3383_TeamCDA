using System;
using System.Collections.Generic;
using UnityEngine;

// Standalone visual test harness. Samples the real clips without depending on gameplay AI.
public class EnemyAnimationShowcase : MonoBehaviour
{
    [Serializable]
    public class Preview
    {
        public string label;
        public EnemyBase prefab;
        public AnimationClip[] clips;
        [NonSerialized] public EnemyBase actor;
        [NonSerialized] public int index;
        [NonSerialized] public float elapsed;
        [NonSerialized] public Vector3 anchor;
    }

    [SerializeField] private Preview[] previews = Array.Empty<Preview>();
    [SerializeField, Min(1)] private int level = 1;
    [SerializeField, Min(1)] private float secondsPerAnimation = 2.5f;
    [SerializeField] private bool autoCycle = true;
    private readonly List<EnemyProjectile> projectiles = new List<EnemyProjectile>();
    private GUIStyle title, heading, body;
    public int PreviewCount => previews.Length;

    private void Start()
    {
        for (int i = 0; i < previews.Length; i++)
        {
            previews[i].anchor = new Vector3((i - 1) * 5.6f, .4f, 0);
            ResetActor(previews[i]);
            EnterAnimation(previews[i]);
        }
    }

    private void ResetActor(Preview p)
    {
        if (p.actor != null) Destroy(p.actor.gameObject);
        p.actor = Instantiate(p.prefab, p.anchor, Quaternion.identity, transform);
        p.actor.name = p.label + " Preview";
        p.actor.transform.localScale = Vector3.one * 1.4f;
        p.actor.SetLevel(level);
        var driver = p.actor.GetComponent<EnemyAnimationDriver>();
        if (driver != null) driver.enabled = false;
        var animator = p.actor.GetComponent<Animator>();
        if (animator != null) animator.enabled = false;
        var shooter = p.actor.GetComponent<EnemyRangedAttack>();
        if (shooter != null) shooter.SetTarget(null);
    }

    private void Update()
    {
        foreach (var p in previews)
        {
            if (p.actor == null || p.clips == null || p.clips.Length == 0) continue;
            p.elapsed += Time.deltaTime;
            var clip = p.clips[p.index];
            if (autoCycle && p.elapsed >= Mathf.Max(secondsPerAnimation, clip.length + .4f))
            {
                ChangeAnimation(p, 1);
                clip = p.clips[p.index];
            }
            float time = clip.isLooping ? Mathf.Repeat(p.elapsed, Mathf.Max(.001f, clip.length)) : Mathf.Min(p.elapsed, clip.length);
            clip.SampleAnimation(p.actor.gameObject, time);
            bool moving = clip.name == "Walk" || clip.name == "Run" || clip.name == "Fly";
            float phase = p.elapsed * (clip.name == "Run" ? 3 : 1.8f);
            p.actor.transform.position = p.anchor + (moving ? Vector3.right * Mathf.Sin(phase) * .65f : Vector3.zero);
            if (moving)
            {
                bool right = Mathf.Cos(phase) >= 0;
                if (p.actor is GroundEnemy ground) ground.SetFacingRight(right);
                else p.actor.GetComponent<SpriteRenderer>().flipX = !right;
            }
        }
        projectiles.RemoveAll(p => p == null);
    }

    public void NextAnimation(int previewIndex) { ChangeAnimation(previews[previewIndex], 1); }

    private void ChangeAnimation(Preview p, int step)
    {
        p.index = (p.index + step + p.clips.Length) % p.clips.Length;
        if (p.actor.IsDefeated) ResetActor(p);
        EnterAnimation(p);
    }

    private void EnterAnimation(Preview p)
    {
        p.elapsed = 0;
        p.actor.transform.position = p.anchor;
        string name = p.clips[p.index].name;
        if (name == "Hit") p.actor.TakeDamage(Mathf.Max(1, p.actor.MaxHealth / 5));
        if (name == "Death") p.actor.TakeDamage(p.actor.CurrentHealth);
        if (name == "Shoot" || name.StartsWith("Aim")) Fire(p);
        p.clips[p.index].SampleAnimation(p.actor.gameObject, 0);
    }

    private void Fire(Preview p)
    {
        var shooter = p.actor.GetComponent<EnemyRangedAttack>();
        if (shooter == null) return;
        float angle = 0;
        string name = p.clips[p.index].name;
        if (name.StartsWith("Aim")) float.TryParse(name.Substring(3), out angle);
        else if (p.actor is GroundEnemy ground && !ground.FacingRight) angle = 180;
        else if (p.actor.GetComponent<SpriteRenderer>().flipX) angle = 180;
        Vector2 direction = new Vector2(Mathf.Cos(angle * Mathf.Deg2Rad), Mathf.Sin(angle * Mathf.Deg2Rad));
        if (angle == 180) direction = Vector2.left;
        var muzzle = p.actor.transform.Find("Muzzle");
        Vector2 origin = muzzle != null ? muzzle.position : p.actor.transform.position;
        if (shooter.TryShoot(origin + direction * 10, out var shot)) projectiles.Add(shot);
    }

    public void SetPreviewLevel(int value)
    {
        level = Mathf.Clamp(value, 1, 50);
        foreach (var p in previews) if (p.actor != null) p.actor.SetLevel(level);
    }

    private void OnGUI()
    {
        if (title == null)
        {
            title = new GUIStyle(GUI.skin.label) { fontSize = 25, fontStyle = FontStyle.Bold };
            heading = new GUIStyle(GUI.skin.label) { fontSize = 19, fontStyle = FontStyle.Bold };
            body = new GUIStyle(GUI.skin.label) { fontSize = 16, wordWrap = true };
        }
        Matrix4x4 previous = GUI.matrix;
        float scale = Mathf.Min(Screen.width / 1280f, Screen.height / 720f);
        GUI.matrix = Matrix4x4.TRS(new Vector3((Screen.width - 1280 * scale) / 2, (Screen.height - 720 * scale) / 2), Quaternion.identity, Vector3.one * scale);
        GUI.Label(new Rect(32, 16, 850, 38), "TL4 / Enemy movement & animation lab", title);
        GUI.Label(new Rect(32, 55, 1000, 28), "Real sprite clips • preview motion • live difficulty-scaled stats", body);
        autoCycle = GUI.Toggle(new Rect(32, 94, 220, 28), autoCycle, "Auto-cycle animations");
        if (GUI.Button(new Rect(280, 90, 40, 30), "−")) SetPreviewLevel(level - 1);
        GUI.Label(new Rect(335, 93, 140, 28), "Level " + level, heading);
        if (GUI.Button(new Rect(460, 90, 40, 30), "+")) SetPreviewLevel(level + 1);
        if (GUI.Button(new Rect(530, 90, 170, 30), "Reset all enemies"))
        {
            foreach (var shot in projectiles) if (shot != null) Destroy(shot.gameObject);
            projectiles.Clear();
            foreach (var p in previews) { p.index = 0; ResetActor(p); EnterAnimation(p); }
        }
        for (int i = 0; i < previews.Length; i++)
        {
            var p = previews[i]; if (p.actor == null) continue;
            float x = 32 + i * 420;
            GUI.Label(new Rect(x, 140, 380, 30), p.label, heading);
            GUI.Label(new Rect(x, 175, 380, 25), "Animation: " + p.clips[p.index].name + "  (" + (p.index + 1) + "/" + p.clips.Length + ")", body);
            GUI.Box(new Rect(x, 410, 380, 246), GUIContent.none);
            GUI.Label(new Rect(x + 14, 423, 350, 150),
                "Type: " + p.actor.Type + " / " + p.actor.MovementType +
                "\nAttacks: " + p.actor.AttackMode + "    Level: " + p.actor.Level +
                "\nHealth: " + p.actor.CurrentHealth + " / " + p.actor.MaxHealth +
                "\nCoins: " + p.actor.CoinReward +
                "\nMelee damage: " + p.actor.MeleeDamage + "    Ranged damage: " + p.actor.RangedDamage +
                "\nStatus: " + (p.actor.IsDefeated ? "Defeated — next animation respawns" : "Alive"), body);
            if (GUI.Button(new Rect(x + 12, 580, 105, 30), "Previous")) ChangeAnimation(p, -1);
            if (GUI.Button(new Rect(x + 136, 580, 105, 30), "Next")) ChangeAnimation(p, 1);
            GUI.enabled = !p.actor.IsDefeated;
            if (GUI.Button(new Rect(x + 260, 580, 105, 30), "Fire")) Fire(p);
            if (GUI.Button(new Rect(x + 12, 618, 165, 26), "Take 10 damage") && p.actor.TakeDamage(10))
            {
                int death = Array.FindIndex(p.clips, clip => clip.name == "Death");
                if (death >= 0) { p.index = death; EnterAnimation(p); }
            }
            GUI.enabled = true;
            if (GUI.Button(new Rect(x + 195, 618, 170, 26), "Respawn")) { ResetActor(p); EnterAnimation(p); }
        }
        GUI.Label(new Rect(32, 678, 1210, 28), "Preview harness only: motion is scripted; damage buttons affect real enemy stats. Projectile collisions are not simulated.", body);
        GUI.matrix = previous;
    }

    private void OnDestroy()
    {
        foreach (var shot in projectiles) if (shot != null) Destroy(shot.gameObject);
    }
}
