using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public class TurretBossTests
{
    [Test]
    public void MobileOpeningsReactToDistanceCooldownAndRepeatHistory()
    {
        GameObject root = new GameObject("Decision test");
        Sprite square = Sprite.Create(Texture2D.whiteTexture, new Rect(0f, 0f, 1f, 1f), Vector2.zero, 1f);
        try
        {
            GameObject target = new GameObject("Target");
            target.transform.SetParent(root.transform);
            target.AddComponent<BoxCollider2D>();
            PlayerHealth health = target.AddComponent<PlayerHealth>();
            TurretBossAttackField field = root.AddComponent<TurretBossAttackField>();
            field.Initialize(health, square);
            TurretBossController boss = root.AddComponent<TurretBossController>();
            boss.Begin(target.transform, field, 1, 17);
            var flags = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
            var select = typeof(TurretBossController).GetMethod("SelectOpening", flags);
            target.transform.position = Vector3.left * 7f;
            Assert.AreEqual(TurretBossAttackState.LongChargeWarning, select.Invoke(boss, new object[] { true }));
            target.transform.position = Vector3.left * 2f;
            Assert.AreEqual(TurretBossAttackState.ShortChargeWarning, select.Invoke(boss, new object[] { true }));
            Assert.AreEqual(TurretBossAttackState.StompWarning, select.Invoke(boss, new object[] { true }),
                "A repeated close-range opener should change attacks.");
            typeof(TurretBossController).GetField("chargeCooldown", flags).SetValue(boss, 2f);
            target.transform.position = Vector3.left;
            Assert.AreEqual(TurretBossAttackState.Repositioning, select.Invoke(boss, new object[] { true }));
            typeof(TurretBossController).GetField("chargeCooldown", flags).SetValue(boss, 0f);
            typeof(TurretBossController).GetField("completedAttack", flags).SetValue(boss, TurretBossAttackState.LongCharge);
            typeof(TurretBossController).GetField("comboAttacks", flags).SetValue(boss, 1);
            var followUp = typeof(TurretBossController).GetMethod("ChooseFollowUp", flags);
            followUp.Invoke(boss, null);
            Assert.AreEqual(TurretBossAttackState.ShortChargeWarning, boss.State,
                "Closing to melee range after pursuit should trigger charge without waiting for a new combo.");
            boss.Advance(TurretBossController.ShortChargeWarningSeconds);
            Assert.AreEqual(TurretBossAttackState.ShortCharge, boss.State);
            float beforeCharge = boss.transform.position.x;
            boss.Advance(TurretBossController.ShortChargeSeconds);
            Assert.Less(boss.transform.position.x, beforeCharge, "Selected charge must actually dash toward the player.");
            Assert.AreEqual(TurretBossAttackState.FollowThrough, boss.State);
            typeof(TurretBossController).GetField("comboAttacks", flags).SetValue(boss, 3);
            typeof(TurretBossController).GetField("chargeCooldown", flags).SetValue(boss, 0f);
            typeof(TurretBossController).GetField("completedAttack", flags).SetValue(boss, TurretBossAttackState.LongCharge);
            followUp.Invoke(boss, null);
            Assert.AreNotEqual(TurretBossAttackState.ShortChargeWarning, boss.State,
                "Proximity charge must not bypass the combo cap.");
            typeof(TurretBossController).GetField("comboAttacks", flags).SetValue(boss, 1);
            typeof(TurretBossController).GetField("chargeCooldown", flags).SetValue(boss, 2f);
            followUp.Invoke(boss, null);
            Assert.AreNotEqual(TurretBossAttackState.ShortChargeWarning, boss.State,
                "Pursuit follow-up must respect charge cooldown.");
        }
        finally
        {
            Object.DestroyImmediate(root);
            Object.DestroyImmediate(square);
        }
    }

    [Test]
    public void VariableBarragesUseOneToFiveShotsAndWaitWithoutFinishingEarly()
    {
        GameObject root = new GameObject("Variable barrage test");
        Sprite square = Sprite.Create(Texture2D.whiteTexture, new Rect(0f, 0f, 1f, 1f), Vector2.zero, 1f);
        try
        {
            GameObject target = new GameObject("Target");
            target.transform.SetParent(root.transform);
            target.AddComponent<BoxCollider2D>();
            PlayerHealth health = target.AddComponent<PlayerHealth>();
            TurretBossAttackField field = root.AddComponent<TurretBossAttackField>();
            field.Initialize(health, square);
            TurretBossLauncher launcher = root.AddComponent<TurretBossLauncher>();
            launcher.Initialize(field, target.transform, 12345);
            var counts = new System.Collections.Generic.HashSet<int>();
            var gaps = new System.Collections.Generic.HashSet<float>();
            foreach (TurretBossPayload payload in new[] { TurretBossPayload.Grenades, TurretBossPayload.Missiles })
            {
                for (int barrage = 0; barrage < 30; barrage++)
                {
                    int fired = 0;
                    System.Action<TurretBossAttackState> onStage = stage =>
                    {
                        if (stage == TurretBossAttackState.GrenadeFlight || stage == TurretBossAttackState.MissileLaunch) { fired++; }
                    };
                    launcher.OnStageChanged += onStage;
                    Assert.IsTrue(launcher.TryStart(payload, 10, true));
                    int planned = launcher.PlannedShotCount;
                    Assert.That(planned, Is.InRange(1, 5));
                    counts.Add(planned);
                    for (int step = 0; step < 40 && launcher.IsActive; step++)
                    {
                        if (launcher.Stage == TurretBossAttackState.PayloadWait)
                        {
                            Assert.IsTrue(launcher.IsActive, "Body must wait through the random gap.");
                            Assert.That(launcher.NextShotDelay, Is.InRange(0.15f, 0.85f));
                            gaps.Add(launcher.NextShotDelay);
                            launcher.Advance(launcher.NextShotDelay * 0.5f);
                            Assert.AreEqual(TurretBossAttackState.PayloadWait, launcher.Stage);
                        }
                        launcher.Advance(10f);
                    }
                    Assert.IsFalse(launcher.IsActive);
                    Assert.AreEqual(planned, fired);
                    launcher.OnStageChanged -= onStage;
                }
            }
            Assert.AreEqual(5, counts.Count, "Seeded sample should exercise every barrage length.");
            Assert.Greater(gaps.Count, 1, "Gaps should vary between shots.");
            launcher.TryStart(TurretBossPayload.Grenades, 10, true);
            launcher.Cancel();
            Assert.IsFalse(launcher.IsActive);
        }
        finally
        {
            Object.DestroyImmediate(root);
            Object.DestroyImmediate(square);
        }
    }

    [Test]
    public void GrenadeLaunchesFromCurrentBossAndKeepsItsFlightOrigin()
    {
        GameObject root = new GameObject("Grenade origin test");
        Sprite square = Sprite.Create(Texture2D.whiteTexture, new Rect(0f, 0f, 1f, 1f), Vector2.zero, 1f);
        try
        {
            GameObject target = new GameObject("Target");
            target.transform.SetParent(root.transform);
            target.AddComponent<BoxCollider2D>();
            PlayerHealth health = target.AddComponent<PlayerHealth>();
            TurretBossAttackField field = root.AddComponent<TurretBossAttackField>();
            field.Initialize(health, square);
            field.SetBossOrigin(new Vector2(-4f, 1f), -1f, 0f);
            field.SetState(TurretBossAttackState.GrenadeWarning, 0f, 10);
            field.SetState(TurretBossAttackState.GrenadeFlight, 0f, 10);
            field.SetBossOrigin(new Vector2(3f, 1f), -1f, 0f);
            field.Advance(0.001f);
            Transform grenade = root.transform.Find("Lobbed grenade");
            Assert.AreEqual(-3.5f, grenade.position.x, 0.02f);
            Assert.AreEqual(3.2f, grenade.position.y, 0.03f);
        }
        finally
        {
            Object.DestroyImmediate(root);
            Object.DestroyImmediate(square);
        }
    }

    [UnityTest]
    public IEnumerator CycleSeparatesHazardsAndOpensCoreOnlyAfterBeam()
    {
        GameObject root = new GameObject("Turret test");
        Sprite square = null;
        try
        {
            GameObject target = new GameObject("Target");
            target.transform.SetParent(root.transform);
            target.AddComponent<BoxCollider2D>();
            PlayerHealth health = target.AddComponent<PlayerHealth>();
            square = Sprite.Create(Texture2D.whiteTexture, new Rect(0f, 0f, 1f, 1f), Vector2.zero, 1f);
            TurretBossAttackField field = root.AddComponent<TurretBossAttackField>();
            field.Initialize(health, square);
            TurretBossController boss = root.AddComponent<TurretBossController>();
            boss.Begin(target.transform, field, 3);
            Assert.AreEqual(140, boss.MaxHealth);
            boss.TakeDamage(50);
            Assert.AreEqual(140, boss.CurrentHealth, "Closed armor must reject damage.");
            boss.Advance(1f);
            Assert.AreEqual(TurretBossAttackState.FloorVolley, boss.State);
            boss.Advance(3.5f);
            for (int grenade = 0; grenade < 3; grenade++)
            {
                Assert.AreEqual(TurretBossAttackState.GrenadeWarning, boss.Launcher.Stage);
                boss.Advance(0.8f);
                Assert.AreEqual(TurretBossAttackState.GrenadeFlight, boss.Launcher.Stage);
                boss.Advance(1.2f);
                Assert.AreEqual(TurretBossAttackState.GrenadeBlast, boss.Launcher.Stage);
                boss.Advance(0.35f);
            }
            Assert.AreEqual(TurretBossAttackState.BeamCharge, boss.State);
            float beamDirection = (float)typeof(TurretBossController).GetField("facingDirection",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).GetValue(boss);
            target.transform.position = root.transform.position + new Vector3(beamDirection * -2f, 0f, 0f);
            boss.Advance(1.5f);
            Assert.AreEqual(beamDirection, (float)typeof(TurretBossController).GetField("facingDirection",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).GetValue(boss),
                "Crossing the boss during charge must not redirect the shot.");
            Assert.AreEqual(TurretBossAttackState.BeamFire, boss.State);
            Assert.IsFalse(boss.CoreExposed);
            boss.Advance(0.7f);
            Assert.IsTrue(boss.CoreExposed);
            boss.Advance(2.5f);
            boss.Advance(1f);
            boss.Advance(3.5f);
            for (int missile = 0; missile < 3; missile++)
            {
                Assert.AreEqual(TurretBossAttackState.MissileLaunch, boss.Launcher.Stage);
                boss.Advance(TurretBossAttackField.MissileLaunchSeconds);
                Assert.AreEqual(TurretBossAttackState.MissileWarning, boss.Launcher.Stage);
                boss.Advance(TurretBossAttackField.MissileWarningSeconds);
                Assert.AreEqual(TurretBossAttackState.MissileDescent, boss.Launcher.Stage);
                boss.Advance(TurretBossAttackField.MissileDescentSeconds);
                Assert.AreEqual(TurretBossAttackState.MissileBlast, boss.Launcher.Stage);
                boss.Advance(TurretBossAttackField.MissileBlastSeconds);
            }
            Assert.AreEqual(TurretBossAttackState.BeamCharge, boss.State, "All missiles finish before the beam.");
            boss.Advance(1.5f);
            boss.Advance(0.7f);
            Assert.IsTrue(boss.CoreExposed);
            boss.TakeDamage(140);
            Assert.AreEqual(TurretBossAttackState.Defeated, boss.State);
            Assert.IsTrue(boss.IsDefeated);
            yield return null;
        }
        finally
        {
            Object.DestroyImmediate(root);
            if (square != null) { Object.DestroyImmediate(square); }
        }
    }

    [UnityTest]
    public IEnumerator FortyPercentTransformsOnceAndMobileLoopReplacesFloorAttacks()
    {
        GameObject root = new GameObject("Phase two test");
        Sprite square = null;
        try
        {
            root.transform.position = new Vector3(7.5f, 0f, 0f);
            GameObject target = new GameObject("Target");
            target.transform.SetParent(root.transform);
            target.transform.position = new Vector3(-5f, 0.8f, 0f);
            target.AddComponent<BoxCollider2D>();
            PlayerHealth health = target.AddComponent<PlayerHealth>();
            square = Sprite.Create(Texture2D.whiteTexture, new Rect(0f, 0f, 1f, 1f), Vector2.zero, 1f);
            TurretBossAttackField field = root.AddComponent<TurretBossAttackField>();
            field.Initialize(health, square);
            field.enabled = false;
            TurretBossController boss = root.AddComponent<TurretBossController>();
            boss.Begin(target.transform, field, 1);
            boss.Advance(1f);
            boss.Advance(3.5f);
            for (int grenade = 0; grenade < 3; grenade++)
            {
                boss.Advance(0.8f);
                boss.Advance(1.2f);
                boss.Advance(0.35f);
            }
            boss.Advance(1.5f);
            boss.Advance(0.7f);
            boss.TakeDamage(60);
            Assert.AreEqual(40, boss.CurrentHealth);
            Assert.IsTrue(boss.IsMobilePhase);
            Assert.AreEqual(TurretBossAttackState.Transforming, boss.State);
            Assert.IsFalse(boss.CoreExposed);
            boss.TakeDamage(10);
            Assert.AreEqual(40, boss.CurrentHealth);
            field.Advance(10f);
            Assert.AreEqual(100, health.CurrentHealth, "Transformation clears hazards.");
            target.transform.position = root.transform.position + Vector3.left * 2f;
            boss.Advance(TurretBossController.TransformationSeconds);
            Assert.Greater(root.transform.position.y, 0.8f);
            Assert.AreEqual(TurretBossAttackState.MobileAssess, boss.State);
            // Force extensions so this regression covers heavier chains deterministically.
            typeof(TurretBossController).GetField("comboExtensionChance",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).SetValue(boss, 1f);
            int recoveries = 0;
            bool sawFollowThrough = false;
            bool sawPursuit = false;
            bool sawLauncher = false;
            bool sawExtendedCombo = false;
            for (int tick = 0; tick < 2000 && recoveries < 2; tick++)
            {
                target.transform.position = new Vector3(-5f, 0.8f, 0f);
                boss.Advance(0.1f);
                Assert.LessOrEqual(boss.ComboAttacks, 3, "Combo length must be bounded.");
                sawFollowThrough |= boss.State == TurretBossAttackState.FollowThrough;
                sawPursuit |= boss.State == TurretBossAttackState.LongCharge;
                sawLauncher |= boss.Launcher.IsActive;
                sawExtendedCombo |= boss.ComboAttacks > 1;
                Assert.AreNotEqual(TurretBossAttackState.FloorWarning, boss.State);
                Assert.AreNotEqual(TurretBossAttackState.FloorVolley, boss.State);
                if (boss.State == TurretBossAttackState.BeamCharge || boss.State == TurretBossAttackState.BeamFire)
                {
                    Assert.IsFalse(boss.Launcher.IsActive, "Beam must wait for launcher completion.");
                }
                if (boss.CoreExposed)
                {
                    Assert.IsFalse(boss.Launcher.IsActive, "Earned openings must be free of leftover payloads.");
                    Assert.Greater(boss.RecoveryDuration, 2.5f);
                    recoveries++;
                    boss.Advance(boss.RecoveryDuration);
                    Assert.AreEqual(TurretBossAttackState.MobileAssess, boss.State);
                }
            }
            Assert.AreEqual(2, recoveries, "Chains must eventually give the player an opening.");
            Assert.IsTrue(sawFollowThrough);
            Assert.IsTrue(sawPursuit);
            Assert.IsTrue(sawLauncher);
            Assert.IsTrue(sawExtendedCombo);
            Object.DestroyImmediate(root);
            yield return null;
        }
        finally
        {
            if (root != null) { Object.DestroyImmediate(root); }
            if (square != null) { Object.DestroyImmediate(square); }
        }
    }

    [UnityTest]
    public IEnumerator MobileHazardsHitOnceAndStompCanBeJumped()
    {
        GameObject root = new GameObject("Mobile damage test");
        Sprite square = null;
        try
        {
            GameObject target = new GameObject("Target");
            target.transform.SetParent(root.transform);
            target.transform.position = new Vector3(0f, 0.8f, 0f);
            target.AddComponent<BoxCollider2D>().size = new Vector2(0.7f, 1.6f);
            PlayerHealth health = target.AddComponent<PlayerHealth>();
            square = Sprite.Create(Texture2D.whiteTexture, new Rect(0f, 0f, 1f, 1f), Vector2.zero, 1f);
            TurretBossAttackField field = root.AddComponent<TurretBossAttackField>();
            field.Initialize(health, square);
            field.enabled = false;
            field.SetState(TurretBossAttackState.ShortCharge, 0f, 20);
            Physics2D.SyncTransforms();
            field.ResolveShortCharge(-3f, 3f, 20);
            field.ResolveShortCharge(-3f, 3f, 20);
            Assert.AreEqual(80, health.CurrentHealth);
            health.ResetHealth();
            field.SetBossOrigin(new Vector2(3f, 0f), -1f, 0f);
            field.SetState(TurretBossAttackState.Stomping, 0f, 20);
            target.transform.position = new Vector3(2.2f, 3f, 0f);
            Physics2D.SyncTransforms();
            field.Advance(0.1f);
            Assert.AreEqual(100, health.CurrentHealth, "Jumping clears the low stomp wave.");
            field.SetState(TurretBossAttackState.Stomping, 0f, 20);
            target.transform.position = new Vector3(2.2f, 0.8f, 0f);
            Physics2D.SyncTransforms();
            field.Advance(0.1f);
            field.Advance(0.01f);
            Assert.AreEqual(80, health.CurrentHealth);
            yield return null;
        }
        finally
        {
            Object.DestroyImmediate(root);
            if (square != null) { Object.DestroyImmediate(square); }
        }
    }

    [UnityTest]
    public IEnumerator LauncherSurvivesBodyChangesAndSpinIsJumpable()
    {
        GameObject root = new GameObject("Independent tracks test");
        Sprite square = null;
        try
        {
            GameObject target = new GameObject("Target");
            target.transform.SetParent(root.transform);
            target.transform.position = new Vector3(-5f, 0.8f, 0f);
            target.AddComponent<BoxCollider2D>().size = new Vector2(0.7f, 1.6f);
            PlayerHealth health = target.AddComponent<PlayerHealth>();
            square = Sprite.Create(Texture2D.whiteTexture, new Rect(0f, 0f, 1f, 1f), Vector2.zero, 1f);
            TurretBossAttackField body = root.AddComponent<TurretBossAttackField>();
            body.Initialize(health, square);
            body.enabled = false;
            TurretBossAttackField payload = body.CreateLauncherField();
            payload.enabled = false;
            TurretBossLauncher launcher = root.AddComponent<TurretBossLauncher>();
            launcher.Initialize(payload, target.transform);
            Assert.IsTrue(launcher.TryStart(TurretBossPayload.Missiles, 10));
            Assert.IsFalse(launcher.TryStart(TurretBossPayload.Grenades, 10));
            launcher.Advance(TurretBossAttackField.MissileLaunchSeconds);
            body.SetState(TurretBossAttackState.Stomping, -5f, 20);
            Assert.AreEqual(TurretBossAttackState.MissileWarning, launcher.Stage);
            launcher.Advance(TurretBossAttackField.MissileWarningSeconds);
            body.Clear();
            Assert.IsTrue(launcher.IsActive, "Body cleanup must not erase in-flight launcher attacks.");
            launcher.Advance(TurretBossAttackField.MissileDescentSeconds);
            Physics2D.SyncTransforms();
            payload.Advance(0.01f);
            Assert.AreEqual(90, health.CurrentHealth);
            launcher.Cancel();
            health.ResetHealth();
            payload.Advance(1f);
            Assert.AreEqual(100, health.CurrentHealth, "Explicit encounter cancellation clears payload damage.");
            body.SetState(TurretBossAttackState.LongCharge, -5f, 20);
            target.transform.position = new Vector3(-5f, 3f, 0f);
            Physics2D.SyncTransforms();
            body.ResolveLongCharge(-7f, -3f, 20);
            Assert.AreEqual(100, health.CurrentHealth, "Jumping clears the leg sweep.");
            target.transform.position = new Vector3(-5f, 0.8f, 0f);
            Physics2D.SyncTransforms();
            body.ResolveLongCharge(-7f, -3f, 20);
            body.ResolveLongCharge(-7f, -3f, 20);
            Assert.AreEqual(80, health.CurrentHealth, "One hit per spin, including swept travel.");
            yield return null;
        }
        finally
        {
            Object.DestroyImmediate(root);
            if (square != null) { Object.DestroyImmediate(square); }
        }
    }

    [UnityTest]
    public IEnumerator MissileLocksTargetHitsOnceAndCancelsCleanly()
    {
        GameObject root = new GameObject("Missile test");
        Sprite square = null;
        try
        {
            GameObject target = new GameObject("Target");
            target.transform.SetParent(root.transform);
            target.transform.position = new Vector3(-5f, 0.8f, 0f);
            target.AddComponent<BoxCollider2D>().size = new Vector2(0.7f, 1.6f);
            PlayerHealth health = target.AddComponent<PlayerHealth>();
            square = Sprite.Create(Texture2D.whiteTexture, new Rect(0f, 0f, 1f, 1f), Vector2.zero, 1f);
            TurretBossAttackField field = root.AddComponent<TurretBossAttackField>();
            field.Initialize(health, square);
            field.enabled = false;
            field.SetState(TurretBossAttackState.MissileWarning, -5f, 10);
            target.transform.position = new Vector3(-3f, 0.8f, 0f);
            field.Advance(0.3f);
            Assert.AreEqual(-3f, field.MissileLandingX);
            Assert.IsFalse(field.MissileTargetLocked);
            field.Advance(0.31f);
            Assert.IsTrue(field.MissileTargetLocked);
            target.transform.position = new Vector3(0f, 0.8f, 0f);
            field.Advance(0.3f);
            Assert.AreEqual(-3f, field.MissileLandingX, "Tracking stops before descent.");
            Assert.AreEqual(100, health.CurrentHealth, "Warnings do no damage.");
            field.SetState(TurretBossAttackState.MissileDescent, 0f, 10);
            Assert.AreEqual(-3f, field.MissileLandingX);
            field.SetState(TurretBossAttackState.MissileBlast, 0f, 10);
            Physics2D.SyncTransforms();
            field.Advance(0.01f);
            Assert.AreEqual(100, health.CurrentHealth, "Moving off the locked mark avoids impact.");
            target.transform.position = new Vector3(-3f, 0.8f, 0f);
            Physics2D.SyncTransforms();
            field.Advance(0.01f);
            field.Advance(0.01f);
            Assert.AreEqual(90, health.CurrentHealth, "One hit per explosion.");
            health.ResetHealth();
            field.SetState(TurretBossAttackState.MissileBlast, -3f, 10);
            field.Clear();
            field.Advance(1f);
            Assert.AreEqual(100, health.CurrentHealth);
            yield return null;
        }
        finally
        {
            Object.DestroyImmediate(root);
            if (square != null) { Object.DestroyImmediate(square); }
        }
    }

    [UnityTest]
    public IEnumerator BeamHitsStandingPlayerOnceAndClearsCrouchedPlayer()
    {
        GameObject root = new GameObject("Beam test");
        Sprite square = null;
        try
        {
            GameObject target = new GameObject("Target");
            target.transform.SetParent(root.transform);
            target.transform.position = new Vector3(-5f, 0.8f, 0f);
            BoxCollider2D collider = target.AddComponent<BoxCollider2D>();
            collider.size = new Vector2(0.7f, 1.6f);
            PlayerHealth health = target.AddComponent<PlayerHealth>();
            square = Sprite.Create(Texture2D.whiteTexture, new Rect(0f, 0f, 1f, 1f), Vector2.zero, 1f);
            TurretBossAttackField field = root.AddComponent<TurretBossAttackField>();
            field.Initialize(health, square);
            field.SetState(TurretBossAttackState.BeamFire, -5f, 10);
            Physics2D.SyncTransforms();
            yield return null;
            yield return null;
            Assert.AreEqual(90, health.CurrentHealth);
            yield return null;
            Assert.AreEqual(90, health.CurrentHealth, "Beam must not damage every frame.");
            health.ResetHealth();
            collider.size = new Vector2(0.7f, 0.7f);
            collider.offset = new Vector2(0f, -0.45f);
            Physics2D.SyncTransforms();
            field.SetState(TurretBossAttackState.BeamFire, -5f, 10);
            yield return null;
            yield return null;
            Assert.AreEqual(100, health.CurrentHealth);
            field.Clear();
            collider.size = new Vector2(0.7f, 1.6f);
            collider.offset = Vector2.zero;
            Physics2D.SyncTransforms();
            yield return null;
            Assert.AreEqual(100, health.CurrentHealth, "Cancelled hazards must stay cleared.");
        }
        finally
        {
            Object.DestroyImmediate(root);
            if (square != null) { Object.DestroyImmediate(square); }
        }
    }
}
