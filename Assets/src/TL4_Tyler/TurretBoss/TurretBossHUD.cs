using UnityEngine;

/// <summary>Siege Core binding for the shared boss HUD.</summary>
public sealed class TurretBossHUD : BossFightHUD
{
    public void Bind(TurretBossController boss, Sprite portrait = null)
    {
        if (portrait == null && boss != null)
        {
            SpriteRenderer source = boss.GetComponent<SpriteRenderer>();
            if (source != null) { portrait = source.sprite; }
        }
        base.Bind(boss, "Siege Core", portrait, () => boss == null ? "" :
            boss.CoreExposed ? "CORE EXPOSED" : boss.IsMobilePhase ? "PHASE II" : "PHASE I");
    }
}
