using NUnit.Framework;
using UnityEngine;
using EscapeThe90s.HUD;

public class UIPanelTests
{
    private UIPanel panel;

    [SetUp]
    public void SetUp()
    {
        panel = new GameObject("Panel").AddComponent<UIPanel>();
    }

    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(panel.gameObject);
    }

    [Test]
    public void Show_ActivatesObject_AndSetsIsVisible()
    {
        panel.gameObject.SetActive(false);

        panel.Show();

        Assert.IsTrue(panel.gameObject.activeSelf);
        Assert.IsTrue(panel.isVisible);
    }

    [Test]
    public void Hide_DeactivatesObject_AndClearsIsVisible()
    {
        panel.Show();

        panel.Hide();

        Assert.IsFalse(panel.gameObject.activeSelf);
        Assert.IsFalse(panel.isVisible);
    }
}
