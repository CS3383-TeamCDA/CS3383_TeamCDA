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
            Assert.IsNotNull(rig.FloorMuzzle);
            field.FireFloorShot(10);
            SpriteRenderer bolt = field.transform.Find("Floor bolt").GetComponent<SpriteRenderer>();
            Assert.Less(Vector3.Distance(rig.FloorMuzzle.position, bolt.transform.position), 0.0001f,
                "Bolts must appear immediately at the actual low firing port.");
            float spawnX = bolt.transform.position.x;
            field.Advance(0.05f);
            Assert.AreEqual(spawnX - 0.4f, bolt.transform.position.x, 0.001f);
            field.Clear();
            Vector2 rightFoot = rig.LeadingFootPosition(1f);
            Vector2 leftFoot = rig.LeadingFootPosition(-1f);
            Assert.Greater(rightFoot.x, rig.transform.position.x);
            Assert.Less(leftFoot.x, rig.transform.position.x);

            int damageEvents = 0;
            boss.DamageTaken += amount => damageEvents++;
            boss.TakeDamage(10);
            Assert.AreEqual(0, damageEvents, "Closed armor must not produce hit effects.");
            Assert.AreEqual(0f, rig.HitFlash);
            SpriteRenderer chassis = rig.transform.Find("Body motion pivot/Chassis").GetComponent<SpriteRenderer>();
            MaterialPropertyBlock wearProperties = new MaterialPropertyBlock();
            float previousWear = 0f;
            foreach (float hp in new[] { 0.95f, 0.7f, 0.45f, 0.2f })
            {
                rig.SetHealthFraction(hp);
                rig.AdvancePresentation(0.05f);
                chassis.GetPropertyBlock(wearProperties);
                float wear = wearProperties.GetFloat("_Wear");
                Assert.Greater(wear, previousWear, "Visible material wear must build with each health reduction.");
                previousWear = wear;
            }
            rig.SetHealthFraction(1f);
            rig.AdvancePresentation(0.05f);
            chassis.GetPropertyBlock(wearProperties);
            Assert.AreEqual(0f, wearProperties.GetFloat("_Wear"));
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
            Assert.AreEqual(1, damageEvents);
            Assert.Greater(rig.HitFlash, 0f);
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
            for (int frame = 0; frame < 1600 && !boss.CoreExposed; frame++)
            {
                boss.Advance(0.1f);
                tick.Invoke(adapter, null);
                rig.AdvancePresentation(0.1f);
            }
            Assert.IsTrue(boss.CoreExposed);
            boss.TakeDamage(100);
            tick.Invoke(adapter, null);
            Assert.IsTrue(rig.IsDead);
            Assert.IsFalse(rig.PlayCannonAttack());
            Assert.IsFalse(rig.PlayLauncherShot());
            Assert.IsFalse(boss.Launcher.IsActive);
            Vector3 deathStart = rig.transform.Find("Body motion pivot").localPosition;
            rig.AdvancePresentation(0f);
            Assert.AreEqual(deathStart, rig.transform.Find("Body motion pivot").localPosition,
                "Pause must freeze the death animation.");
            for (int frame = 0; frame < 120; frame++) { rig.AdvancePresentation(1f / 60f); }
            Assert.IsTrue(rig.DeathComplete);
            Assert.Less(rig.transform.Find("Body motion pivot").localPosition.y, 1.2f);
            Assert.IsTrue(rig.gameObject.activeSelf, "Defeat must leave wreckage visible.");
            Vector3 wreckPosition = rig.transform.position;
            enemy.transform.position += Vector3.right;
            tick.Invoke(adapter, null);
            Assert.AreEqual(wreckPosition, rig.transform.position, "Wreckage must stop following the dead physics body.");

        }
        finally
        {
            Object.DestroyImmediate(arena);
            Object.DestroyImmediate(sprite);
        }
    }
}
