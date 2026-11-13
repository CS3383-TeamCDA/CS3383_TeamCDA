#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using EscapeThe90s.Contracts;
using EscapeThe90s.WorldGen.Demo;
using EscapeThe90s.WorldGen.Integration;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace EscapeThe90s.WorldGen.Tests
{
    public sealed class CoordinateEnemyLifecycleTests
    {
        private const long Seed = 431;
        private Scene _scene;
        private WorldGenSettings _settings;
        private Component _gameStateComponent;
        private GeneratedCoordinateContent _content;
        private Random.State _randomState;

        [SetUp]
        public void CaptureRandomState()
        {
            _randomState = Random.state;
            Random.InitState((int)Seed);
        }

        [UnityTearDown]
        public IEnumerator Cleanup()
        {
            if (_content != null) _content.Dispose();
            if (_gameStateComponent != null) Object.Destroy(_gameStateComponent.gameObject);
            if (_scene.IsValid() && _scene.isLoaded)
            {
                SceneManager.CreateScene("AfterCoordinateEnemyLifecycle");
                yield return SceneManager.UnloadSceneAsync(_scene);
            }
            if (_settings != null) Object.Destroy(_settings);
            yield return null;
            Random.state = _randomState;
        }

        [UnityTest]
        public IEnumerator DefaultCoordinateScene_ChunkRecycled_RetiresOwnedEnemiesAndPreservesLoadedNeighbors()
        {
            _scene = EditorSceneManager.LoadSceneInPlayMode(
                "Assets/Scenes/GeneratedWorldGameDemo.unity", new LoadSceneParameters(LoadSceneMode.Single));
            WorldNetworkDemo demo = null;
            float deadline = Time.realtimeSinceStartup + 10f;
            while (demo == null && Time.realtimeSinceStartup < deadline)
            {
                demo = Object.FindAnyObjectByType<WorldNetworkDemo>();
                if (demo == null) yield return null;
            }
            Assert.IsNotNull(demo, Context("setup", "scene", "missing composition"));
            Assert.IsTrue(demo.coordinateWorldMode, Context("setup", "scene", "default must remain coordinate mode"));
            // Clone configuration so no scene or referenced asset is edited by the test.
            _settings = Object.Instantiate(demo.settings);
            _settings.seed = Seed;
            demo.settings = _settings;
            EnemySpawner spawner = Object.FindAnyObjectByType<EnemySpawner>();
            Assert.IsNotNull(spawner, Context("setup", "scene", "missing enemy spawner"));
            CoordinateWorldStreamer streamer = demo.GetComponent<CoordinateWorldStreamer>();
            if (streamer == null) streamer = demo.gameObject.AddComponent<CoordinateWorldStreamer>();
            var observer = demo.gameObject.AddComponent<CoordinateEnemyLifecycleObserver>();
            observer.Initialize(streamer, spawner);
            IGameState gameState = null;
            deadline = Time.realtimeSinceStartup + 10f;
            while (gameState == null && Time.realtimeSinceStartup < deadline)
            {
                foreach (MonoBehaviour component in Object.FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include))
                {
                    if (!(component is IGameState candidate)) continue;
                    gameState = candidate;
                    _gameStateComponent = component;
                    break;
                }
                if (gameState == null) yield return null;
            }
            Assert.IsNotNull(gameState, Context("setup", "scene", "missing IGameState"));
            var retiredCoordinate = new WorldChunkCoord(-2, 0);
            var retainedCoordinate = new WorldChunkCoord(1, 0);
            ChunkId retiredId = ChunkId.ForWorldCoordinate(retiredCoordinate.X, retiredCoordinate.Y);
            ChunkId retainedId = ChunkId.ForWorldCoordinate(retainedCoordinate.X, retainedCoordinate.Y);
            gameState.RequestStart();
            deadline = Time.realtimeSinceStartup + 10f;
            while ((!observer.TryGetSpawn(retiredId, out _) || !observer.TryGetSpawn(retainedId, out _)) &&
                   Time.realtimeSinceStartup < deadline) yield return null;
            _content = demo.GetComponent<GeneratedCoordinateContent>();
            Assert.IsNotNull(_content, Context("startup", "scene", "production content consumer missing"));
            Assert.IsTrue(observer.TryGetSpawn(retiredId, out ChunkSpawnedInfo retiredInfo), Context("spawn event", "(-2,0)", "not observed"));
            Assert.IsTrue(observer.TryGetSpawn(retainedId, out ChunkSpawnedInfo retainedInfo), Context("spawn event", "(1,0)", "not observed"));
            AssertEnemyInstruction(retiredInfo, EnemyType.Ground);
            AssertEnemyInstruction(retainedInfo, EnemyType.Flying);
            IReadOnlyList<EnemyBase> retiredEnemies = observer.GetOwnedEnemies(retiredId);
            IReadOnlyList<EnemyBase> retainedEnemies = observer.GetOwnedEnemies(retainedId);
            Assert.AreEqual(1, retiredEnemies.Count, Context("owned content", "(-2,0)", "expected ground enemy at published anchor"));
            Assert.AreEqual(1, retainedEnemies.Count, Context("owned content", "(1,0)", "expected flying enemy at published anchor"));
            Assert.IsTrue(retiredEnemies[0] != null, Context("before recycle", "(-2,0)", "enemy missing"));
            Assert.IsTrue(retainedEnemies[0] != null, Context("before recycle", "(1,0)", "enemy missing"));
            Assert.IsTrue(streamer.TryGetContentHash(retiredCoordinate, out _), Context("before recycle", "(-2,0)", "terrain not loaded"));
            Assert.IsTrue(streamer.TryGetContentHash(retainedCoordinate, out _), Context("before recycle", "(1,0)", "terrain not loaded"));

            CameraFollow camera = Object.FindAnyObjectByType<CameraFollow>();
            Assert.IsNotNull(camera, Context("move focus", "scene", "camera binding missing"));
            Transform focus = camera.target;
            Assert.IsNotNull(focus, Context("move focus", "scene", "player target missing"));
            Rigidbody2D body = focus.GetComponent<Rigidbody2D>();
            Assert.IsNotNull(body, Context("move focus", "scene", "player body missing"));
            body.linearVelocity = Vector2.zero;
            body.simulated = false;
            focus.position = new Vector3(WorldChunkCoord.Size * 2, streamer.SurfaceHeightAt(WorldChunkCoord.Size * 2) + 3f, 0f);
            deadline = Time.realtimeSinceStartup + 10f;
            while (!observer.WasRecycled(retiredId) && Time.realtimeSinceStartup < deadline) yield return null;
            Assert.IsTrue(observer.WasRecycled(retiredId), Context("recycle event", "(-2,0)", "not observed after moving focus to chunk (2,0)"));
            yield return null; // Destroy is deferred to the end of the recycle frame.
            Assert.IsFalse(streamer.TryGetContentHash(retiredCoordinate, out _), Context("after recycle", "(-2,0)", "terrain still loaded"));
            Assert.IsTrue(retiredEnemies[0] == null, Context("after recycle", "(-2,0)", "owned enemy survived"));
            Assert.IsFalse(observer.WasRecycled(retainedId), Context("after recycle", "(1,0)", "neighbor unexpectedly recycled"));
            Assert.IsTrue(streamer.TryGetContentHash(retainedCoordinate, out _), Context("after recycle", "(1,0)", "neighbor terrain not loaded"));
            Assert.IsTrue(retainedEnemies[0] != null, Context("after recycle", "(1,0)", "neighbor's enemy was incorrectly retired"));
        }

        private static void AssertEnemyInstruction(ChunkSpawnedInfo info, EnemyType expected)
        {
            int count = 0;
            for (int i = 0; i < info.SpawnLocations.Count; i++)
                if (info.SpawnLocations[i].Kind == SpawnLocationKind.Enemy && info.SpawnLocations[i].ContentKindIndex == (int)expected) count++;
            Assert.AreEqual(1, count, Context("spawn instruction", info.Id.ToString(), "known populated chunk must declare " + expected));
        }

        private static string Context(string stage, string chunk, string detail) =>
            "Seed=" + Seed + "; chunk=" + chunk + "; lifecycle stage=" + stage + ": " + detail;
    }
}
#endif
