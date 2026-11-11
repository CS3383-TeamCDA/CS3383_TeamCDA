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
        }
        finally
        {
            Object.DestroyImmediate(root);
            Object.DestroyImmediate(sprite);
        }
    }
}
