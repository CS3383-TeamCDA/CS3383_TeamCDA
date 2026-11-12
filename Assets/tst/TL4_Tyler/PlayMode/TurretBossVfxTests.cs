using NUnit.Framework;
using UnityEngine;

public sealed class TurretBossVfxTests
{
    [Test]
    public void AnimatedPayloadsFreezeOnPauseAndAftermathDoesNotExtendDamage()
    {
        GameObject root = new GameObject("Attack VFX test");
        Sprite first = Sprite.Create(Texture2D.whiteTexture, new Rect(0f, 0f, 1f, 1f), Vector2.one * 0.5f, 1f);
        Sprite second = Sprite.Create(Texture2D.whiteTexture, new Rect(0f, 0f, 1f, 1f), Vector2.one * 0.5f, 1f);
        TurretBossVfxSet effects = ScriptableObject.CreateInstance<TurretBossVfxSet>();
        effects.continuousBeam = new Material(Shader.Find("Sprites/Default"));
        effects.beam = new[] { first, second };
        effects.grenades = new[] { first, second };
        effects.explosions = new[] { first, second };
        effects.shockwaves = new[] { first, second };
        try
        {
            GameObject player = new GameObject("Player");
            player.transform.SetParent(root.transform);
            player.transform.position = new Vector3(-8f, 0.5f, 0f);
            player.AddComponent<BoxCollider2D>();
            PlayerHealth health = player.AddComponent<PlayerHealth>();
            TurretBossAttackField field = root.AddComponent<TurretBossAttackField>();
            field.Initialize(health, first);
            field.ConfigureVfx(effects);
            TurretBossAttackField launcher = field.CreateLauncherField();
            launcher.SetState(TurretBossAttackState.GrenadeWarning, 0f, 10);
            launcher.SetState(TurretBossAttackState.GrenadeFlight, 0f, 10);
            SpriteRenderer grenade = launcher.transform.Find("Lobbed grenade").GetComponent<SpriteRenderer>();
            launcher.Advance(0.05f);
            Assert.AreSame(first, grenade.sprite, "Launcher must inherit the VFX set.");
            launcher.Advance(0.25f);
            Assert.AreSame(second, grenade.sprite);
            Vector3 position = grenade.transform.position;
            launcher.Advance(0f);
            Assert.AreSame(second, grenade.sprite);
            Assert.AreEqual(position, grenade.transform.position);
            player.transform.position = new Vector3(0f, 0.5f, 0f);
            Physics2D.SyncTransforms();
            launcher.SetState(TurretBossAttackState.GrenadeBlast, 0f, 10);
            launcher.Advance(0.01f);
            Assert.AreEqual(90, health.CurrentHealth);
            Assert.AreEqual(1, launcher.GetComponentsInChildren<TurretBossVfxBurst>().Length);
            launcher.SetState(TurretBossAttackState.PayloadWait, 0f, 10);
            Assert.AreEqual(1, launcher.GetComponentsInChildren<TurretBossVfxBurst>().Length,
                "The non-damaging smoke animation should survive the blast state.");
            launcher.Advance(0.5f);
            Assert.AreEqual(90, health.CurrentHealth, "Aftermath must not add damage.");
            launcher.Clear();
            Assert.AreEqual(0, launcher.GetComponentsInChildren<TurretBossVfxBurst>().Length,
                "Explicit cancellation must stop all remaining visuals.");
            field.SetBossOrigin(new Vector2(7f, 0f), -1f, 0f);
            field.SetState(TurretBossAttackState.BeamFire, 0f, 10);
            field.Advance(0.1f);
            SpriteRenderer beam = field.transform.Find("Cannon beam").GetComponent<SpriteRenderer>();
            Assert.AreEqual(SpriteDrawMode.Simple, beam.drawMode, "Beam must not tile projectile silhouettes.");
            Assert.AreSame(effects.continuousBeam, beam.sharedMaterial);
            MaterialPropertyBlock flow = new MaterialPropertyBlock();
            beam.GetPropertyBlock(flow);
            Assert.AreEqual(0.1f, flow.GetFloat("_FlowTime"), 0.0001f);
            Assert.AreEqual(-1f, flow.GetFloat("_Direction"));
            Assert.AreSame(first.texture, flow.GetTexture("_BeamAtlas"),
                "Continuous beam must use the original animated atlas, not replacement procedural art.");
            Vector4 sampleRect = flow.GetVector("_FrameRect");
            Assert.Greater(sampleRect.x, first.textureRect.x / first.texture.width);
            Assert.Less(sampleRect.z, first.textureRect.width / first.texture.width,
                "Only the middle should repeat, excluding projectile caps.");
            field.Advance(0f);
            beam.GetPropertyBlock(flow);
            Assert.AreEqual(0.1f, flow.GetFloat("_FlowTime"), 0.0001f);
            field.Advance(0.1f);
            beam.GetPropertyBlock(flow);
            Assert.AreEqual(0.2f, flow.GetFloat("_FlowTime"), 0.0001f);
            field.SetState(TurretBossAttackState.BeamCharge, 0f, 10);
            field.Advance(0.1f);
            Assert.AreNotSame(effects.continuousBeam, beam.sharedMaterial,
                "Warning line must remain distinct from the fired beam.");
            field.SetState(TurretBossAttackState.ShortCharge, 0f, 10);
            field.Advance(0.05f);
            field.ResolveShortCharge(0f, 0.1f, 10);
            SpriteRenderer wake = field.transform.Find("Charge dash wake").GetComponent<SpriteRenderer>();
            Assert.IsTrue(wake.enabled, "The dash must actually show its effect, not just the stomp.");
            Assert.Greater(wake.sortingOrder, 33, "Dash effect must draw over foreground legs.");
            Assert.AreSame(first, wake.sprite);
            Assert.IsFalse(wake.flipX);
            field.Advance(0.15f);
            field.ResolveShortCharge(0.1f, 0.3f, 10);
            Assert.AreSame(second, wake.sprite, "Wake animation must continue even after the attack has hit.");
            Assert.AreEqual(1.45f, wake.transform.position.x, 0.001f);
            Assert.Greater(wake.transform.position.x, 0.3f, "Rightward charge burst must be ahead of the body.");
            field.SetState(TurretBossAttackState.FollowThrough, 0f, 10);
            Assert.IsFalse(wake.enabled);
            Assert.Greater(field.GetComponentsInChildren<TurretBossVfxBurst>().Length, 0);
            field.SetState(TurretBossAttackState.LongCharge, 0f, 10);
            field.Advance(0.05f);
            field.ResolveLongCharge(1f, 0.8f, 10);
            Assert.IsTrue(wake.enabled);
            Assert.IsTrue(wake.flipX);
            Assert.Less(wake.transform.position.x, 0.8f, "Leftward charge burst must move to the leading side too.");
            field.Clear();
            Assert.IsFalse(wake.enabled);
            Assert.AreEqual(0, field.GetComponentsInChildren<TurretBossVfxBurst>().Length);
            BoxCollider2D chargeBox = wake.GetComponent<BoxCollider2D>();
            Assert.IsNotNull(chargeBox);
            Assert.IsTrue(chargeBox.isTrigger);
            Assert.IsFalse(chargeBox.enabled);
            GameObject ground = new GameObject("Ground for charge placement");
            ground.transform.SetParent(root.transform);
            ground.layer = LayerMask.NameToLayer("Ground");
            ground.transform.position = new Vector3(0f, -0.5f, 0f);
            ground.AddComponent<BoxCollider2D>().size = new Vector2(20f, 1f);
            player.transform.position = new Vector3(1f, 0.8f, 0f);
            Physics2D.SyncTransforms();
            field.SetState(TurretBossAttackState.LongChargeWarning, 0f, 10);
            Assert.IsFalse(chargeBox.enabled, "Warnings must not enable contact damage.");
            int beforeCharge = health.CurrentHealth;
            field.SetState(TurretBossAttackState.LongCharge, 0f, 10);
            field.ResolveLongCharge(-0.1f, 0f, 10);
            Physics2D.SyncTransforms();
            Assert.IsTrue(chargeBox.enabled);
            Assert.AreEqual(-0.04f, chargeBox.bounds.min.y, 0.001f,
                "The shared box must retain its small ground inset.");
            Assert.AreEqual(beforeCharge - 10, health.CurrentHealth);
            field.ResolveLongCharge(0f, 0f, 10);
            Assert.AreEqual(beforeCharge - 10, health.CurrentHealth, "Charge contact must hit once per attack.");
            field.SetState(TurretBossAttackState.FollowThrough, 0f, 10);
            Assert.IsFalse(chargeBox.enabled, "The collider must turn off as soon as the charge ends.");
        }
        finally
        {
            Object.DestroyImmediate(root);
            Object.DestroyImmediate(effects.continuousBeam);
            Object.DestroyImmediate(effects);
            Object.DestroyImmediate(first);
            Object.DestroyImmediate(second);
        }
    }
}
