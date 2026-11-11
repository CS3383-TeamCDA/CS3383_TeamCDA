using System.Reflection;
using NUnit.Framework;
using UnityEngine;

public sealed class TurretBossVisualRigTests
{
    [Test]
    public void IdlePlantsAllFeetAndWalkingLiftsOnlyOneAtATime()
    {
        GameObject root = new GameObject("Art gait test");
        root.SetActive(false);
        Sprite sprite = Sprite.Create(Texture2D.whiteTexture, new Rect(0f, 0f, 1f, 1f), Vector2.zero, 100f);
        try
        {
            TurretBossVisualRig rig = root.AddComponent<TurretBossVisualRig>();
            Sprite[] parts = new Sprite[12];
            for (int i = 0; i < parts.Length; i++) { parts[i] = sprite; }
            typeof(TurretBossVisualRig).GetField("parts", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(rig, parts);
            root.SetActive(true);
            rig.enabled = false;
            rig.SetForm(true);
            Transform legs = root.transform.Find("Four articulated legs");
            Transform[] ankles = new Transform[4];
            Vector3[] previous = new Vector3[4];
            for (int i = 0; i < 4; i++)
            {
                ankles[i] = legs.GetChild(i).Find("Knee pivot/Ankle pivot");
                previous[i] = ankles[i].position;
            }
            // Simulate a script reload adding the turret field and clearing runtime leg state.
            int rootChildren = root.transform.childCount;
            var flags = BindingFlags.NonPublic | BindingFlags.Instance;
            typeof(TurretBossVisualRig).GetField("turret", flags).SetValue(rig, null);
            var runtimeLegs = (System.Array)typeof(TurretBossVisualRig).GetField("legPoses", flags).GetValue(rig);
            for (int i = 0; i < runtimeLegs.Length; i++) { runtimeLegs.SetValue(null, i); }
            Assert.DoesNotThrow(() => rig.AdvancePresentation(1f / 60f));
            Assert.IsNotNull(typeof(TurretBossVisualRig).GetField("turret", flags).GetValue(rig));
            Assert.IsNotNull(rig.Cannon);
            Assert.AreEqual(rootChildren, root.transform.childCount, "Reload recovery must reuse the existing hierarchy.");
            const float delta = 1f / 60f;
            for (int tick = 0; tick < 120; tick++)
            {
                rig.AdvancePresentation(delta);
                for (int i = 0; i < 4; i++)
                {
                    Assert.Less(Vector3.Distance(previous[i], ankles[i].position), 0.0001f, "Idle must not slide grounded feet.");
                }
            }
            int movingFrames = 0;
            for (int tick = 0; tick < 480; tick++)
            {
                for (int i = 0; i < 4; i++) { previous[i] = ankles[i].position; }
                root.transform.position += Vector3.right * (0.65f * delta);
                rig.AdvancePresentation(delta);
                int moved = 0;
                for (int i = 0; i < 4; i++)
                {
                    if (Vector3.Distance(previous[i], ankles[i].position) > 0.0001f) { moved++; }
                    Transform knee = ankles[i].parent;
                    Transform hip = knee.parent;
                    Assert.AreEqual(new Vector2(1.55f, 1.28f).magnitude * 0.8f,
                        Vector3.Distance(hip.position, knee.position), 0.001f);
                    Assert.AreEqual(new Vector2(0.91f, 1.35f).magnitude * 0.8f,
                        Vector3.Distance(knee.position, ankles[i].position), 0.001f,
                        "Walking foot targets must remain within leg reach.");
                }
                Assert.LessOrEqual(moved, 1, "Crawl keeps three support feet planted.");
                if (moved > 0) { movingFrames++; }
            }
            Assert.Greater(movingFrames, 100, "Movement should actually animate stepping.");
            for (int tick = 0; tick < 60; tick++) { rig.AdvancePresentation(delta); }
            for (int i = 0; i < 4; i++) { previous[i] = ankles[i].position; }
            rig.AdvancePresentation(0.1f);
            for (int i = 0; i < 4; i++) { Assert.Less(Vector3.Distance(previous[i], ankles[i].position), 0.0001f); }

            // Combat-speed travel must keep stepping, with at most one diagonal pair airborne.
            int fastMovingFrames = 0;
            foreach (float speed in new[] { 6f, -8f })
            {
                for (int tick = 0; tick < 240; tick++)
                {
                    for (int i = 0; i < 4; i++) { previous[i] = ankles[i].position; }
                    root.transform.position += Vector3.right * (speed * delta);
                    rig.AdvancePresentation(delta);
                    int movingFeet = 0;
                    for (int i = 0; i < 4; i++)
                    {
                        if (Vector3.Distance(previous[i], ankles[i].position) > 0.0001f) { movingFeet++; }
                        Assert.AreEqual(new Vector2(0.91f, 1.35f).magnitude * 0.8f,
                            Vector3.Distance(ankles[i].parent.position, ankles[i].position), 0.001f);
                    }
                    Assert.LessOrEqual(movingFeet, 2);
                    if (movingFeet > 0) { fastMovingFrames++; }
                }
            }
            Assert.Greater(fastMovingFrames, 200, "Combat speed must animate steps instead of resetting to a sliding stance.");
            for (int tick = 0; tick < 60; tick++) { rig.AdvancePresentation(delta); }
            Transform body = root.transform.Find("Body motion pivot");
            rig.SyncCombatMotion(TurretBossAttackState.LungeWarning, TurretBossController.LungeWarningSeconds);
            rig.AdvancePresentation(delta);
            Assert.Less(body.localPosition.y, 2.4f, "Charge must brace low.");
            rig.SyncCombatMotion(TurretBossAttackState.Lunging, TurretBossController.LungeSeconds * 0.5f);
            rig.AdvancePresentation(delta);
            Assert.Greater(Mathf.DeltaAngle(0f, body.localEulerAngles.z), 5f);
            rig.SyncCombatMotion(TurretBossAttackState.Spinning, 0.1f);
            rig.AdvancePresentation(delta);
            Vector3 firstSpinFoot = ankles[0].localPosition;
            Assert.Less(body.localPosition.y, 2f);
            rig.SyncCombatMotion(TurretBossAttackState.Spinning, 0.3f);
            rig.AdvancePresentation(delta);
            Assert.Greater(Vector3.Distance(firstSpinFoot, ankles[0].localPosition), 0.1f,
                "Spin legs must sweep rather than hold a standing pose.");
            for (int frame = 0; frame <= 240; frame++)
            {
                rig.SyncCombatMotion(TurretBossAttackState.Spinning, frame / 60f);
                rig.AdvancePresentation(delta);
                for (int i = 0; i < 4; i++)
                {
                    float side = i % 2 == 0 ? -1f : 1f;
                    Transform knee = ankles[i].parent;
                    Transform hip = knee.parent;
                    Assert.AreEqual(side * (i < 2 ? 0.75f : 1.1f), hip.localPosition.x, 0.0001f,
                        "Spinning must not detach or orbit the hip socket.");
                    Assert.Greater(legs.InverseTransformPoint(ankles[i].position).x * side, 0f,
                        "Feet must never cross through the chassis to the opposite side.");
                    Assert.Greater(legs.InverseTransformPoint(knee.position).x * side, 0f,
                        "Knees must remain on their own side.");
                    Assert.AreEqual(new Vector2(0.91f, 1.35f).magnitude * 0.8f,
                        Vector3.Distance(knee.position, ankles[i].position), 0.001f);
                }
            }
            rig.SyncCombatMotion(TurretBossAttackState.StompWarning, TurretBossController.StompWarningSeconds);
            rig.AdvancePresentation(delta);
            Assert.Greater(body.localPosition.y, 2.4f);
            rig.SyncCombatMotion(TurretBossAttackState.Stomping, 0f);
            rig.AdvancePresentation(delta);
            Assert.Less(body.localPosition.y, 2.4f);
            rig.SyncCombatMotion(TurretBossAttackState.MobileAssess, 0f);
            Assert.IsTrue(rig.PlayCannonAttack());
            Assert.IsTrue(rig.PlayLauncherShot(), "Launcher should animate independently during cannon charge.");
            Assert.IsFalse(rig.PlayCannonAttack(), "Repeated triggers must not restart an active charge.");
            rig.AdvancePresentation(0f);
            Assert.IsTrue(rig.IsLauncherAnimating);
            rig.AdvancePresentation(0.75f);
            Assert.IsFalse(rig.IsLauncherAnimating);
            Assert.IsTrue(rig.IsCannonAnimating, "Launcher completion must not cancel the cannon.");
            rig.AdvancePresentation(0.85f);
            Assert.Less(rig.Cannon.localPosition.x, 0.45f, "Firing should recoil the cannon.");
            rig.AdvancePresentation(0.7f);
            Assert.IsFalse(rig.IsCannonAnimating);
            Assert.AreEqual(0.45f, rig.Cannon.localPosition.x, 0.0001f);
            rig.SetExposed(false);
            Transform shutter = root.transform.Find("Body motion pivot/Left core shutter");
            rig.AnimateCore(true);
            rig.AdvancePresentation(0.15f);
            Assert.That(shutter.localPosition.x, Is.InRange(-0.95f, -0.35f));
            float halfOpen = shutter.localPosition.x;
            rig.AnimateCore(false);
            Assert.AreEqual(halfOpen, shutter.localPosition.x, "Reversing shutters should not snap.");
            rig.AdvancePresentation(0.3f);
            Assert.AreEqual(-0.34f, shutter.localPosition.x, 0.0001f);
            rig.SetForm(false);
            Assert.AreEqual(0f, rig.Deployment);
            Assert.IsFalse(legs.gameObject.activeSelf);
            rig.TransitionToForm(true);
            Assert.IsTrue(rig.IsTransforming);
            rig.AdvancePresentation(0f);
            Assert.AreEqual(0f, rig.Deployment, "Paused time must freeze the transition.");
            rig.AdvancePresentation(0.5f);
            Assert.AreEqual(0.25f, rig.Deployment, 0.001f);
            Vector3 beforeReverse = root.transform.Find("Body motion pivot").localPosition;
            rig.TransitionToForm(false);
            Assert.AreEqual(beforeReverse, root.transform.Find("Body motion pivot").localPosition,
                "Reversing must not snap the body to a different pose.");
            rig.AdvancePresentation(0.5f);
            Assert.IsFalse(rig.IsTransforming);
            Assert.AreEqual(0f, rig.Deployment);
            Assert.IsFalse(legs.gameObject.activeSelf);
            rig.TransitionToForm(true);
            rig.AdvancePresentation(2f);
            Assert.IsFalse(rig.IsTransforming);
            Assert.AreEqual(1f, rig.Deployment);
            Assert.IsTrue(legs.gameObject.activeSelf);
            Assert.IsTrue(root.transform.Find("Planted base skirt").gameObject.activeSelf,
                "The docking base must remain on the floor after deployment.");
            Transform dockingBase = root.transform.Find("Planted base skirt");
            Vector3 basePosition = dockingBase.position;
            root.transform.position += Vector3.right * 0.1f;
            rig.AdvancePresentation(0.1f);
            Assert.Less(Vector3.Distance(basePosition, dockingBase.position), 0.0001f,
                "Walking away must leave the base behind.");
            Assert.AreEqual(1f, dockingBase.GetComponent<SpriteRenderer>().color.a);
            rig.TransitionToForm(false);
            for (int tick = 0; tick < 180; tick++) { rig.AdvancePresentation(1f / 60f); }
            Assert.Less(Vector3.Distance(basePosition, root.transform.position), 0.0001f,
                "Retraction must return to the docking base before folding in.");
            Assert.AreEqual(0f, rig.Deployment);
            rig.TransitionToForm(true);
            rig.AdvancePresentation(2f);
            for (int i = 0; i < 4; i++) { previous[i] = ankles[i].position; }
            rig.AdvancePresentation(0.1f);
            for (int i = 0; i < 4; i++)
            {
                Assert.Less(Vector3.Distance(previous[i], ankles[i].position), 0.0001f,
                    "Transformation completion should hand off to grounded idle without a foot jump.");
            }
        }
        finally
        {
            Object.DestroyImmediate(root);
            Object.DestroyImmediate(sprite);
        }
    }
}
