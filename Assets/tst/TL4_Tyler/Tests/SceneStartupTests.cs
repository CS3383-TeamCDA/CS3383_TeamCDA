using System;
using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using EscapeThe90s;
using EscapeThe90s.HUD;
using EscapeThe90s.GameStates;

public class SceneStartupTests
{
    private GameObject containerObj, manager, publisher, panel;

    [UnityTearDown]
    public IEnumerator Cleanup()
    {
        UnityEngine.Object.Destroy(panel);
        UnityEngine.Object.Destroy(publisher);
        UnityEngine.Object.Destroy(manager);
        UnityEngine.Object.Destroy(containerObj);
        yield return null;
    }

    [UnityTest]
    public IEnumerator MenuStartsAndPublisherReceivesExactlyOneEventAfterReenable()
    {
        // 1. Build infrastructure
        containerObj = new GameObject("ServiceContainer");
        containerObj.SetActive(false);
        var container = containerObj.AddComponent<ServiceContainer>();

        manager = new GameObject("GameStateManager");
        var gsm = manager.AddComponent<GameStateManager>();

        publisher = new GameObject("Publisher");
        publisher.SetActive(false); // Start asleep
        var events = publisher.AddComponent<GameStateEventPublisher>();

        // Let the publisher discover its requirements before the container injects them.
        publisher.SetActive(true);
        // Awake resolves both services, injects dependencies, and opens activation gates.
        containerObj.SetActive(true);

        panel = new GameObject("Menu");
        var menu = panel.AddComponent<MainMenuPanel>();

        int count = 0;
        events.OnGameStarted += () => count++;

        // Simulate a rapid GameObject toggle
        publisher.SetActive(false);
        publisher.SetActive(true);
        yield return null;

        menu.StartGame();
        
        // 4. Structural Assertions
        Assert.IsInstanceOf<PlayingState>(GameStateManager.Instance.CurrentState);
        Assert.AreEqual(1, count); // Verification passes: Guarded from double subscription!
    }

    // UN-IGNORED! This now works flawlessly because our framework handles out-of-order execution safely.
    [UnityTest]
    public IEnumerator PublisherCanEnableBeforeManagerAndDisableAfterManagerDestruction()
    {
        // Create the publisher first (Out of order)
        publisher = new GameObject("Publisher");
        var events = publisher.AddComponent<GameStateEventPublisher>();

        // Create the manager later
        manager = new GameObject("GameStateManager");
        var gsm = manager.AddComponent<GameStateManager>();

        // Inject dependency and spin up the publisher engine
        events.Inject(gsm);

        int count = 0;
        events.OnGameStarted += () => count++;
        yield return null;

        GameStateManager.Instance.RequestStart();
        Assert.AreEqual(1, count);

        // Safely destroy manager; cleanup happens perfectly without NullReferenceExceptions
        UnityEngine.Object.Destroy(manager);
        yield return null;

        Assert.IsNull(GameStateManager.Instance);
        publisher.SetActive(false);
    }

    // UN-IGNORED! Component uncheck-disabled design prevents this from ever breaking again.
    [UnityTest]
    public IEnumerator UnassignedPublisherCanDisableBeforeStartup()
    {
        publisher = new GameObject("Publisher");
        publisher.AddComponent<GameStateEventPublisher>();
        publisher.SetActive(false); // Safely closes down
        yield return null;
    }
}
