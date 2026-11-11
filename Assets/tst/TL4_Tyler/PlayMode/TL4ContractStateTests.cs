using System;
using System.Collections;
using EscapeThe90s.Contracts;
using EscapeThe90s.Infrastructure.Unity;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public class TL4ContractStateTests
{
    [UnityTest]
    public IEnumerator StateBindingHandlesLateInjectionPauseResumeAndDisable()
    {
        var root = new GameObject("Scope");
        var spawnerObject = new GameObject("Spawner");
        var cameraObject = new GameObject("Camera");
        var point = new GameObject("Offscreen spawn");
        EnemyBase spawned = null;
        try
        {
            var scope = root.AddComponent<DependencyScope>();
            var state = new FakeGameState();
            // The Playing event happened before the binding existed.
            state.Change(GameStateType.Playing);
            scope.Register<IGameState>(state);
            var spawner = spawnerObject.AddComponent<EnemySpawner>();
            var binding = spawnerObject.AddComponent<EnemyGameStateBinding>();
            var camera = cameraObject.AddComponent<Camera>();
            camera.orthographic = true;
            camera.transform.position = new Vector3(0, 0, -10);
            point.transform.position = new Vector3(100, 0, 0);
            spawner.ConfigureCamera(camera);
            spawner.ConfigureSpawnPoints(new[] { point.transform });
            spawner.SetSpawnRate(1);
            spawner.OnEnemySpawned += enemy => spawned = enemy;
            scope.Inject(spawnerObject);
            yield return null;
            Assert.IsTrue(binding.enabled);
            spawner.Tick(1);
            Assert.AreEqual(1, spawner.AliveCount, "Late injection reads Playing immediately.");
            UnityEngine.Object.DestroyImmediate(spawned.gameObject);
            state.Change(GameStateType.Paused);
            spawner.Tick(10);
            Assert.AreEqual(0, spawner.AliveCount);
            state.Change(GameStateType.Playing);
            spawner.Tick(.5f);
            Assert.AreEqual(0, spawner.AliveCount, "Paused time must not accumulate spawn debt.");
            spawner.Tick(.5f);
            Assert.AreEqual(1, spawner.AliveCount, "Resume preserves the configured rate.");
            UnityEngine.Object.DestroyImmediate(spawned.gameObject);
            binding.enabled = false;
            state.Change(GameStateType.Playing);
            spawner.Tick(1);
            Assert.AreEqual(0, spawner.AliveCount, "Disabled binding unsubscribes and blocks spawning.");
            binding.enabled = true;
            state.Change(GameStateType.Menu);
            spawner.Tick(1);
            Assert.AreEqual(0, spawner.AliveCount);
            state.Change(GameStateType.GameOver);
            spawner.Tick(1);
            Assert.AreEqual(0, spawner.AliveCount);
        }
        finally
        {
            if (spawned != null) UnityEngine.Object.DestroyImmediate(spawned.gameObject);
            UnityEngine.Object.DestroyImmediate(spawnerObject);
            UnityEngine.Object.DestroyImmediate(root);
            UnityEngine.Object.DestroyImmediate(cameraObject);
            UnityEngine.Object.DestroyImmediate(point);
        }
    }

    private sealed class FakeGameState : IGameState
    {
        public GameStateType CurrentStateType { get; private set; } = GameStateType.Menu;
        public event Action<GameStateChange> OnStateTypeChanged;
        public void Change(GameStateType next)
        {
            var previous = CurrentStateType;
            CurrentStateType = next;
            OnStateTypeChanged?.Invoke(new GameStateChange(previous, next, false));
        }
        public void RequestStart() { }
        public void RequestPause() { }
        public void RequestResume() { }
        public void RequestGameOver() { }
        public void RequestRestart() { }
        public void RequestReturnToMenu() { }
    }
}
