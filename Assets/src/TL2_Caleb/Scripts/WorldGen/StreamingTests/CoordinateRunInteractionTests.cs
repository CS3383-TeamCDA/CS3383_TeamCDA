#if UNITY_EDITOR
using System;
using System.Collections;
using EscapeThe90s.Contracts;
using EscapeThe90s.WorldGen.Demo;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.InputSystem.Users;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;
using Random = UnityEngine.Random;

namespace EscapeThe90s.WorldGen.Tests
{
    public sealed class CoordinateRunInteractionTests
    {
        private const string ScenePath = "Assets/Scenes/GeneratedWorldGameDemo.unity";
        private Scene _scene;
        private IGameState _gameState;
        private MonoBehaviour _manager;
        private WorldNetworkDemo _demo;
        private Mouse _mouse;
        private Mouse _previousMouse;
        private WorldGenSettings _settings;
        private Random.State _randomState;
        private float _timeScale;

        [UnitySetUp]
        public IEnumerator StartRun()
        {
            _randomState = Random.state;
            _timeScale = Time.timeScale;
            _previousMouse = Mouse.current;
            _scene = EditorSceneManager.LoadSceneInPlayMode(ScenePath, new LoadSceneParameters(LoadSceneMode.Single));
            yield return WaitFor(() =>
            {
                _manager = FindService<IGameState>();
                _gameState = _manager as IGameState;
                return _gameState != null && _gameState.CurrentStateType == GameStateType.Menu;
            }, "Menu did not initialize.");
            _demo = Object.FindAnyObjectByType<WorldNetworkDemo>();
            Assert.IsNotNull(_demo);
            _settings = Object.Instantiate(_demo.settings);
            _settings.seed = 431;
            _demo.settings = _settings;
            _gameState.RequestStart();
            yield return WaitFor(() => _demo.player.gameObject.activeInHierarchy &&
                _demo.GetComponent<CoordinateWorldStreamer>() != null, "Coordinate run did not start.");
        }

        [UnityTearDown]
        public IEnumerator Cleanup()
        {
            if (_mouse != null && _mouse.added) InputSystem.RemoveDevice(_mouse);
            if (_previousMouse != null && _previousMouse.added) _previousMouse.MakeCurrent();
            if (_manager != null) Object.Destroy(_manager.gameObject);
            if (_settings != null) Object.Destroy(_settings);
            Scene active = SceneManager.GetActiveScene();
            if (active.path == ScenePath) _scene = active;
            if (_scene.IsValid() && _scene.isLoaded)
            {
                SceneManager.CreateScene("AfterCoordinateRunInteractions");
                yield return SceneManager.UnloadSceneAsync(_scene);
            }
            yield return null;
            Random.state = _randomState;
            Time.timeScale = _timeScale;
        }

        [UnityTest]
        public IEnumerator CoordinateRun_SpeedPickup_IncreasesPlayerMovementSpeed()
        {
            var player = _demo.player.GetComponent<PlayerControllerTL6>();
            var pickups = FindService<IPowerUpSpawner>() as IPowerUpSpawner;
            Assert.IsNotNull(pickups);
            float startingSpeed = player.MaxSpeedX;
            bool collected = false;
            void OnCollected(PowerUpCollectedInfo info)
            {
                if (info.Type == PowerUpType.SpeedBoost) collected = true;
            }
            pickups.OnPowerUpCollected += OnCollected;
            try
            {
                Vector3 position = player.GetComponent<Collider2D>().bounds.center;
                pickups.SpawnPowerUp(new WorldPosition2D(position.x, position.y), PowerUpType.SpeedBoost);
                yield return WaitFor(() => collected, "The pickup trigger did not publish a speed boost.");
                Assert.Greater(player.MaxSpeedX, startingSpeed);
            }
            finally
            {
                pickups.OnPowerUpCollected -= OnCollected;
            }
        }

        [UnityTest]
        public IEnumerator CoordinateRun_FireHeld_SpawnsPlayerProjectile()
        {
            var input = _demo.player.GetComponent<PlayerInput>();
            _mouse = InputSystem.AddDevice<Mouse>();
            InputUser.PerformPairingWithDevice(_mouse, input.user);
            Vector2 aim = new Vector2(Screen.width * 0.9f, Screen.height * 0.55f);
            InputSystem.QueueStateEvent(_mouse, new MouseState { position = aim }.WithButton(MouseButton.Left));
            yield return WaitFor(() => Object.FindAnyObjectByType<PlayerProjectile>() != null,
                "The equipped player did not fire through the active input map.");
        }

        [UnityTest]
        public IEnumerator CoordinateRun_ReturnToMenu_StartsFreshPlayerAndProgress()
        {
            var oldProgress = _demo.GetComponent<CoordinateWorldStreamer>().Progress;
            oldProgress.SetDifficulty(5f);
            Transform oldPlayer = _demo.player;
            _gameState.RequestReturnToMenu();
            yield return WaitFor(() => _gameState.CurrentStateType == GameStateType.Menu &&
                SceneManager.GetActiveScene().handle != _scene.handle, "Return to menu did not reload the run.");
            _scene = SceneManager.GetActiveScene();
            Assert.IsTrue(oldPlayer == null);
            _demo = Object.FindAnyObjectByType<WorldNetworkDemo>();
            Assert.IsNotNull(_demo);
            Assert.IsFalse(_demo.player.gameObject.activeInHierarchy);
            _gameState.RequestStart();
            yield return WaitFor(() => _demo.player.gameObject.activeInHierarchy &&
                _demo.GetComponent<CoordinateWorldStreamer>() != null, "Start from the menu did not activate a fresh run.");
            var progress = _demo.GetComponent<CoordinateWorldStreamer>().Progress;
            Assert.AreNotSame(oldProgress, progress);
            Assert.AreEqual(0f, progress.GetCurrentDifficulty(), 0.01f);
            Assert.AreEqual(0f, progress.GetCurrentDistance(), 0.01f);
        }

        private static MonoBehaviour FindService<T>() where T : class
        {
            foreach (MonoBehaviour component in Object.FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include))
                if (component is T) return component;
            return null;
        }

        private static IEnumerator WaitFor(Func<bool> condition, string failure)
        {
            float deadline = Time.realtimeSinceStartup + 10f;
            while (!condition())
            {
                Assert.Less(Time.realtimeSinceStartup, deadline, failure);
                yield return null;
            }
        }
    }
}
#endif
