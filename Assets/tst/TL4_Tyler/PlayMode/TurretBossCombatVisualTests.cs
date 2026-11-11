using System.Reflection;
using NUnit.Framework;
using UnityEngine;

public sealed class TurretBossCombatVisualTests
{
    [Test]
    public void CombatBindingHidesLegacyArtAndFollowsCoreBeamAndTransformation()
    {
        GameObject arena = new GameObject("Combat art binding test");
        Sprite sprite = Sprite.Create(Texture2D.whiteTexture, new Rect(0f, 0f, 1f, 1f), Vector2.zero, 100f);
        try
        {
            GameObject player = new GameObject("Target");
            player.transform.SetParent(arena.transform);
            player.transform.position = new Vector3(-5f, 0.8f, 0f);
            player.AddComponent<BoxCollider2D>();
            PlayerHealth health = player.AddComponent<PlayerHealth>();
            TurretBossAttackField field = arena.AddComponent<TurretBossAttackField>();
            field.Initialize(health, sprite);
            GameObject enemy = new GameObject("Controller");
            enemy.transform.SetParent(arena.transform);
            enemy.transform.position = new Vector3(7.5f, 0.1f, 0f);
            SpriteRenderer legacy = enemy.AddComponent<SpriteRenderer>();
            TurretBossController boss = enemy.AddComponent<TurretBossController>();
            boss.Begin(player.transform, field, 1, 22);
            GameObject visual = new GameObject("New rig");
            visual.transform.SetParent(arena.transform);
            visual.SetActive(false);
            TurretBossVisualRig rig = visual.AddComponent<TurretBossVisualRig>();
            Sprite[] sprites = new Sprite[12];
            for (int i = 0; i < sprites.Length; i++) { sprites[i] = sprite; }
            typeof(TurretBossVisualRig).GetField("parts", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(rig, sprites);
            visual.SetActive(true);
            TurretBossCombatVisuals adapter = arena.AddComponent<TurretBossCombatVisuals>();
            adapter.Initialize(boss, rig, field, 0f);
            var tick = typeof(TurretBossCombatVisuals).GetMethod("Update", BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsFalse(legacy.enabled);
            Assert.IsFalse(enemy.GetComponent<TurretBossLegRig>().PresentationVisible);
            Assert.AreEqual(boss.FacingDirection * 0.5f, rig.transform.localScale.x);
            Assert.AreEqual(0f, rig.transform.position.y, "Art remains grounded despite controller body lift.");
            boss.Advance(1f);
            boss.Advance(3.5f);
            boss.Advance(0.8f);
            Assert.IsTrue(rig.IsLauncherAnimating, "Real grenade flight should trigger independent recoil.");
            for (int tickIndex = 0; tickIndex < 200 && boss.State != TurretBossAttackState.BeamCharge; tickIndex++)
            {
                boss.Advance(0.1f);
                tick.Invoke(adapter, null);
                rig.AdvancePresentation(0.1f);
            }
            Assert.AreEqual(TurretBossAttackState.BeamCharge, boss.State);
            Assert.IsTrue(rig.IsCannonAnimating);
            boss.Advance(boss.BeamChargeSeconds);
            boss.Advance(0.1f);
            tick.Invoke(adapter, null);
            rig.AdvancePresentation(0.01f);
            Assert.Less(rig.Cannon.localPosition.x, 0.45f);
            boss.Advance(0.7f);
            tick.Invoke(adapter, null);
            Assert.IsTrue(rig.IsExposed);
            boss.TakeDamage(60);
            tick.Invoke(adapter, null);
            Assert.IsFalse(rig.IsExposed);
            Assert.IsTrue(rig.IsTransforming);
            rig.AdvancePresentation(2f);
            Assert.AreEqual(1f, rig.Deployment);
            Assert.IsTrue(rig.transform.Find("Planted base skirt").gameObject.activeSelf);
            boss.Advance(2f);
            tick.Invoke(adapter, null);
            Assert.AreEqual(0f, rig.transform.position.y);
            Assert.IsFalse(legacy.enabled);
        }
        finally
        {
            Object.DestroyImmediate(arena);
            Object.DestroyImmediate(sprite);
        }
    }
}
