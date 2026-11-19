using System;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using EscapeThe90s.Contracts;
using EscapeThe90s.HUD;
using Object = UnityEngine.Object;

/// <summary>
/// Checks that PowerUpHUD shows active power-ups with their stacks and time left, and hides them when they run out.
/// </summary>
public class PowerUpHUDTests
{
    // Stands in for TL3's PowerUpManager so tests can report pickups directly.
    private sealed class FakeSpawner : IPowerUpSpawner
    {
        public event Action<PowerUpCollectedInfo> OnPowerUpCollected;

        public void SpawnPowerUp(WorldPosition2D position, PowerUpType type) { }

        public void Collect(PowerUpType type, int stacks, float seconds, bool isPermanent = false)
        {
            OnPowerUpCollected?.Invoke(new PowerUpCollectedInfo(type, new WorldPosition2D(0f, 0f), stacks, seconds, isPermanent));
        }
    }

    private GameObject hudObject;
    private PowerUpHUD hud;
    private FakeSpawner spawner;
    private PowerUpHUD.PowerUpSlot speedSlot;
    private PowerUpHUD.PowerUpSlot magnetSlot;

    [SetUp]
    public void SetUp()
    {
        speedSlot = CreateSlot(PowerUpType.SpeedBoost);
        magnetSlot = CreateSlot(PowerUpType.Magnet);

        // Starts inactive so the slots are assigned before Awake hides the icons, like a prefab wired in the Inspector.
        hudObject = new GameObject("PowerUpHUD");
        hudObject.SetActive(false);
        hud = hudObject.AddComponent<PowerUpHUD>();
        TL1TestUtil.SetField(hud, "slots", new[] { speedSlot, magnetSlot });
        hudObject.SetActive(true);

        spawner = new FakeSpawner();
        hud.Inject(spawner);
    }

    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(hudObject);
        Object.DestroyImmediate(speedSlot.icon);
        Object.DestroyImmediate(magnetSlot.icon);
    }

    private static PowerUpHUD.PowerUpSlot CreateSlot(PowerUpType type)
    {
        GameObject icon = new GameObject(type + "Icon");
        return new PowerUpHUD.PowerUpSlot
        {
            type = type,
            icon = icon,
            stackText = CreateText("Stacks", icon.transform),
            timerText = CreateText("Timer", icon.transform)
        };
    }

    private static TMP_Text CreateText(string name, Transform parent)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent);
        return go.AddComponent<TextMeshProUGUI>();
    }

    [Test]
    public void AtStart_AllIconsHidden()
    {
        Assert.IsFalse(speedSlot.icon.activeSelf);
        Assert.IsFalse(magnetSlot.icon.activeSelf);
    }

    [Test]
    public void Pickup_ShowsIconWithTimeLeft()
    {
        spawner.Collect(PowerUpType.SpeedBoost, 1, 5f);

        Assert.IsTrue(speedSlot.icon.activeSelf);
        Assert.AreEqual("5s", speedSlot.timerText.text);
        Assert.AreEqual("", speedSlot.stackText.text, "A single stack shouldn't show a count.");
    }

    [Test]
    public void Pickup_OnlyShowsThatType()
    {
        spawner.Collect(PowerUpType.SpeedBoost, 1, 5f);

        Assert.IsFalse(magnetSlot.icon.activeSelf);
    }

    [Test]
    public void SecondPickup_ShowsStackCount()
    {
        spawner.Collect(PowerUpType.SpeedBoost, 1, 5f);
        spawner.Collect(PowerUpType.SpeedBoost, 2, 5f);

        Assert.AreEqual("x2", speedSlot.stackText.text);
        Assert.AreEqual(2, hud.GetStacks(PowerUpType.SpeedBoost));
    }

    [Test]
    public void Tick_CountsDownTimeLeft()
    {
        spawner.Collect(PowerUpType.SpeedBoost, 1, 5f);

        hud.Tick(2.5f);

        Assert.AreEqual(2.5f, hud.GetRemainingSeconds(PowerUpType.SpeedBoost), 0.0001f);
        Assert.AreEqual("3s", speedSlot.timerText.text, "Time left is rounded up.");
    }

    [Test]
    public void Tick_PastDuration_HidesIcon()
    {
        spawner.Collect(PowerUpType.SpeedBoost, 1, 5f);

        hud.Tick(5f);

        Assert.IsFalse(speedSlot.icon.activeSelf);
        Assert.IsFalse(hud.IsActive(PowerUpType.SpeedBoost));
    }

    [Test]
    public void SecondPickup_RestartsTimer()
    {
        spawner.Collect(PowerUpType.SpeedBoost, 1, 5f);
        hud.Tick(4f);

        spawner.Collect(PowerUpType.SpeedBoost, 2, 5f);   // TL3 refreshes the timer on a repeat pickup

        Assert.AreEqual(5f, hud.GetRemainingSeconds(PowerUpType.SpeedBoost), 0.0001f);
    }

    [Test]
    public void Tick_WhilePaused_DoesNotCountDown()
    {
        spawner.Collect(PowerUpType.SpeedBoost, 1, 5f);

        hud.Tick(0f);   // Time.deltaTime is 0 while the game is paused

        Assert.AreEqual(5f, hud.GetRemainingSeconds(PowerUpType.SpeedBoost), 0.0001f);
    }

    [Test]
    public void PermanentPickup_NeverExpires_AndShowsNoTimer()
    {
        spawner.Collect(PowerUpType.Magnet, 1, 0f, isPermanent: true);

        hud.Tick(1000f);

        Assert.IsTrue(magnetSlot.icon.activeSelf);
        Assert.AreEqual("", magnetSlot.timerText.text);
    }

    [Test]
    public void InstantPickup_IsNotShown()
    {
        // A timed power-up with no duration (e.g. an instant heal) ends the same frame it is picked up.
        spawner.Collect(PowerUpType.SpeedBoost, 1, 0f);

        Assert.IsFalse(speedSlot.icon.activeSelf);
    }

    [Test]
    public void Inject_NewSpawner_StopsListeningToOld()
    {
        FakeSpawner newSpawner = new FakeSpawner();
        hud.Inject(newSpawner);

        spawner.Collect(PowerUpType.SpeedBoost, 1, 5f);

        Assert.IsFalse(hud.IsActive(PowerUpType.SpeedBoost), "Pickups from the old spawner should be ignored.");
    }
}
