using System;
using System.Collections;
using EscapeThe90s.Contracts;
using EscapeThe90s.Infrastructure.Unity;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public class BossEncounterTests
{
    [UnityTest]
    public IEnumerator EncounterRejectsDuplicatesScalesAndRewardsExactlyOnce()
    {
        GameObject root = new GameObject("Boss scope");
        GameObject template = new GameObject("Boss template");
        GameObject player = new GameObject("Target");
        try
        {
            DependencyScope scope = root.AddComponent<DependencyScope>();
            FakeState state = new FakeState();
            scope.Register<IGameState>(state);
            BossEnemy prefab = template.AddComponent<BossEnemy>();
            template.SetActive(false);
            GameObject controllerObject = new GameObject("Encounter");
            controllerObject.transform.SetParent(root.transform);
            BossEncounterController controller = controllerObject.AddComponent<BossEncounterController>();
            controller.Configure(prefab, player.transform);
            scope.Inject(controllerObject);
            IBossEncounter encounter = controller;
            int endedCount = 0;
            BossEncounterResult ended = default;
            encounter.OnEncounterEnded += result => { endedCount++; ended = result; };
            BossEncounterRequest request = new BossEncounterRequest(42UL, new WorldPosition2D(5f, 2f), 3, 7);
            state.Set(GameStateType.Menu);
            Assert.IsFalse(encounter.TryStart(request));
            state.Set(GameStateType.Playing);
            Assert.IsFalse(encounter.TryStart(new BossEncounterRequest(1, new WorldPosition2D(float.NaN, 0), 1, 0)));
            Assert.IsTrue(encounter.TryStart(request));
            Assert.IsFalse(encounter.TryStart(request));
            BossEnemy boss = controller.GetComponentInChildren<BossEnemy>(true);
            Assert.AreEqual(3, boss.Level);
            Assert.AreEqual(140, boss.MaxHealth);
            boss.TakeDamage(70);
            Assert.IsTrue(boss.IsEnraged);
            state.Set(GameStateType.Paused);
            Assert.IsFalse(boss.CanStartAttack);
            Assert.IsTrue(encounter.IsActive);
            state.Set(GameStateType.Playing);
            boss.TakeDamage(70);
            boss.TakeDamage(1000);
            Assert.IsFalse(encounter.IsActive);
            Assert.AreEqual(1, endedCount);
            Assert.AreEqual(BossEncounterOutcome.Defeated, ended.Outcome);
            Assert.AreEqual(42UL, ended.EncounterId);
            Assert.AreEqual(9, ended.CoinReward);
            Assert.IsFalse(encounter.TryStart(request));
            encounter.Cancel();
            Assert.AreEqual(1, endedCount);
            state.Set(GameStateType.Playing, true);
            Assert.IsTrue(encounter.TryStart(request));
            state.Set(GameStateType.Menu);
            Assert.AreEqual(BossEncounterOutcome.Cancelled, ended.Outcome);
            Assert.AreEqual(0, ended.CoinReward);
            Assert.AreEqual(2, endedCount);
            yield return null;
            state.Set(GameStateType.Playing);
            Assert.IsFalse(encounter.TryStart(new BossEncounterRequest(43UL, new WorldPosition2D(0f, 0f), 0, 0)));
            Assert.IsTrue(encounter.TryStart(new BossEncounterRequest(43UL, new WorldPosition2D(5f, 2f), 1, 0)));
            BossEnemy lostBoss = controller.GetComponentInChildren<BossEnemy>(true);
            UnityEngine.Object.DestroyImmediate(lostBoss.gameObject);
            yield return null;
            Assert.AreEqual(BossEncounterOutcome.Despawned, ended.Outcome);
            Assert.AreEqual(0, ended.CoinReward);
            Assert.AreEqual(3, endedCount);
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(root);
            UnityEngine.Object.DestroyImmediate(template);
            UnityEngine.Object.DestroyImmediate(player);
        }
    }

    private sealed class FakeState : IGameState
    {
        public GameStateType CurrentStateType { get; private set; } = GameStateType.Playing;
        public event Action<GameStateChange> OnStateTypeChanged;
        public void Set(GameStateType state, bool newRun = false)
        {
            GameStateType previous = CurrentStateType;
            CurrentStateType = state;
            OnStateTypeChanged?.Invoke(new GameStateChange(previous, state, newRun));
        }
        public void RequestStart() { }
        public void RequestPause() { }
        public void RequestResume() { }
        public void RequestGameOver() { }
        public void RequestRestart() { }
        public void RequestReturnToMenu() { }
    }
}
