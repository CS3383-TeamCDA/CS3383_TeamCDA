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

    [UnityTest]
    public IEnumerator DefeatedFlyingEnemyFallsAndSettlesOnTerrain()
    {
        GameObject corpse = new GameObject("Flying corpse");
        GameObject terrain = new GameObject("Terrain");
        try
        {
            terrain.transform.position = new Vector3(0f, -0.5f, 0f);
            terrain.AddComponent<BoxCollider2D>().size = new Vector2(20f, 1f);
            corpse.transform.position = new Vector3(0f, 3f, 0f);
            Rigidbody2D body = corpse.AddComponent<Rigidbody2D>();
            body.gravityScale = 0f;
            body.constraints = RigidbodyConstraints2D.FreezeRotation;
            corpse.AddComponent<BoxCollider2D>();
            FlyingEnemy enemy = corpse.AddComponent<FlyingEnemy>();
            enemy.TakeDamage(enemy.MaxHealth);
            enemy.Move();
            Assert.Greater(body.gravityScale, 0f);
            yield return new WaitForSeconds(0.3f);
            Assert.Less(body.position.y, 2.9f, "A defeated flyer must leave its airborne perch.");
            Assert.AreEqual(0f, body.linearVelocity.x);
            yield return new WaitForSeconds(1.2f);
            Assert.That(body.position.y, Is.InRange(0.45f, 0.6f), "The solid body must land on terrain.");
            Assert.Less(Mathf.Abs(body.linearVelocity.y), 0.1f);
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(corpse);
            UnityEngine.Object.DestroyImmediate(terrain);
        }
    }

    [UnityTest]
    public IEnumerator CorpseIgnoresPlayerButKeepsSolidTerrainContact()
    {
        GameObject corpse = new GameObject("Corpse");
        GameObject player = new GameObject("Player");
        GameObject terrain = new GameObject("Terrain");
        try
        {
            player.tag = "Player";
            player.transform.position = new Vector3(10f, 0f, 0f);
            Collider2D playerCollider = player.AddComponent<BoxCollider2D>();
            terrain.transform.position = new Vector3(0f, -0.5f, 0f);
            Collider2D terrainCollider = terrain.AddComponent<BoxCollider2D>();
            terrain.GetComponent<BoxCollider2D>().size = new Vector2(20f, 1f);
            corpse.transform.position = new Vector3(0f, 2f, 0f);
            Rigidbody2D body = corpse.AddComponent<Rigidbody2D>();
            body.constraints = RigidbodyConstraints2D.FreezeRotation;
            Collider2D corpseCollider = corpse.AddComponent<BoxCollider2D>();
            GroundEnemy enemy = corpse.AddComponent<GroundEnemy>();
            enemy.TakeDamage(enemy.MaxHealth);
            Assert.AreEqual(LayerMask.NameToLayer("Corpse"), corpse.layer);
            Assert.AreNotEqual(0, corpse.layer, "Corpses must be excluded from Default ground checks.");
            Assert.IsTrue(Physics2D.GetIgnoreCollision(corpseCollider, playerCollider));
            Assert.IsFalse(Physics2D.GetIgnoreCollision(corpseCollider, terrainCollider));
            Assert.IsFalse(corpseCollider.isTrigger);
            yield return new WaitForSeconds(1f);
            Assert.That(body.position.y, Is.InRange(0.45f, 0.6f));
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(corpse);
            UnityEngine.Object.DestroyImmediate(player);
            UnityEngine.Object.DestroyImmediate(terrain);
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
