using NUnit.Framework;
using UnityEngine;
using EscapeThe90s.HUD;

/// <summary>
/// HUDPanel shows one heart per 20% of health (5 hearts in use), removing them from right to left.
/// The layout mirrors the HUD prefab: a "Hearts" object with 7 heart children, of which 5 are in use.
/// </summary>
public class HUDHeartsTests
{
    private const int TotalHeartSlots = 7;

    private GameObject root;
    private HUDPanel panel;
    private GameObject[] hearts;

    [SetUp]
    public void SetUp()
    {
        root = new GameObject("HUDPanel");
        panel = root.AddComponent<HUDPanel>();

        GameObject container = new GameObject("Hearts");
        container.transform.SetParent(root.transform);

        hearts = new GameObject[TotalHeartSlots];
        for (int i = 0; i < TotalHeartSlots; i++)
        {
            hearts[i] = new GameObject($"Heart{i + 1}");
            hearts[i].transform.SetParent(container.transform);
        }

        TL1TestUtil.SetField(panel, "hearts", hearts);
        TL1TestUtil.SetField(panel, "heartsInUse", 5);
        TL1TestUtil.SetField(panel, "maximumHealth", 100);
    }

    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(root);
    }

    private int CountVisibleHearts()
    {
        int count = 0;
        foreach (GameObject heart in hearts)
        {
            if (heart.activeSelf)
            {
                count++;
            }
        }
        return count;
    }

    [TestCase(100, 5)]
    [TestCase(99, 5)]   // a heart only disappears once its whole 20% is gone
    [TestCase(80, 4)]
    [TestCase(1, 1)]
    [TestCase(0, 0)]
    public void UpdateHealthDisplay_ShowsOneHeartPerTwentyPercent(int health, int expectedHearts)
    {
        panel.UpdateHealthDisplay(health);

        Assert.AreEqual(expectedHearts, CountVisibleHearts());
    }

    [Test]
    public void UpdateHealthDisplay_RemovesHeartsFromRightToLeft()
    {
        panel.UpdateHealthDisplay(60);   // 3 hearts

        for (int i = 0; i < TotalHeartSlots; i++)
        {
            Assert.AreEqual(i < 3, hearts[i].activeSelf, $"Heart {i + 1} has the wrong visibility.");
        }
    }

    [Test]
    public void UpdateHealthDisplay_FullHealth_ExtraSlotsStayHidden()
    {
        panel.UpdateHealthDisplay(100);

        Assert.IsFalse(hearts[5].activeSelf, "Heart 6 is not in use and should stay hidden.");
        Assert.IsFalse(hearts[6].activeSelf, "Heart 7 is not in use and should stay hidden.");
    }

    [Test]
    public void UpdateHealthDisplay_AboveMaximum_ShowsOnlyHeartsInUse()
    {
        panel.UpdateHealthDisplay(150);

        Assert.AreEqual(5, CountVisibleHearts());
    }

    [Test]
    public void UpdateHealthDisplay_HealAfterDamage_HeartsComeBack()
    {
        panel.UpdateHealthDisplay(20);
        panel.UpdateHealthDisplay(100);

        Assert.AreEqual(5, CountVisibleHearts());
    }

    [Test]
    public void HeartsLeftUnassigned_UsesChildrenOfHeartsObject()
    {
        TL1TestUtil.SetField(panel, "hearts", null);

        panel.UpdateHealthDisplay(40);   // 2 hearts

        Assert.AreEqual(2, CountVisibleHearts());
        Assert.IsTrue(hearts[0].activeSelf);
        Assert.IsTrue(hearts[1].activeSelf);
    }
}
