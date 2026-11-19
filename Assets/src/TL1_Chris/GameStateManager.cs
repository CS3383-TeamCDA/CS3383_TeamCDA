using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using EscapeThe90s.State;
using EscapeThe90s.GameStates;
using EscapeThe90s.Contracts;

namespace EscapeThe90s
{
    public class GameStateManager : MonoBehaviour, IGameState, IDependent<IPlayerHealth>
    {
        #region Fields

        private readonly StateMachine<GameState> machine = new StateMachine<GameState>();
        // True from a scene-reload request until the new scene has loaded (Restart and Return to Menu).
        private bool isReloading;
        private GameState stateAfterReload;

        // Reloads the active scene. Tests replace this so they can check a reload without unloading the test scene.
        private Action reloadScene = ReloadActiveScene;
        private IPlayerHealth playerHealth;

        #endregion

        #region Properties

        public static GameStateManager Instance { get; private set; }
        public GameState CurrentState => machine.CurrentState;
        public GameState PreviousState => machine.PreviousState;

        // Other features cannot reference the GameState classes, so IGameState reports GameStateType values.
        public GameStateType CurrentStateType => GetStateType(CurrentState);
        private GameStateType PreviousStateType => GetStateType(PreviousState);

        #endregion

        #region Events

        public event Action<GameState> OnStateChanged
        {
            add
            {
                machine.OnStateChanged += value;
            }
            remove
            {
                machine.OnStateChanged -= value;
            }
        }

        public event Action<GameStateChange> OnStateTypeChanged;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            machine.OnStateChanged += HandleStateChanged;

            // This keeps the GameObject alive across scene changes
            DontDestroyOnLoad(gameObject);
        }

        private void Start()
        {
            if (CurrentState == null)
            {
                machine.ChangeState(new MenuState());
            }
        }

        private void Update()
        {
            machine.Update();
        }

        private void OnDestroy()
        {
            if (Instance != this)
            {
                return;
            }

            machine.OnStateChanged -= HandleStateChanged;
            SceneManager.sceneLoaded -= OnReloadedSceneLoaded;
            if (playerHealth != null)
            {
                playerHealth.OnPlayerDied -= HandlePlayerDied;
            }
            Instance = null;
        }

        #endregion

        #region Player

        // Called by the scene's ServiceContainer. This manager survives scene loads, so each scene
        // (including every restart) injects its own player; stop listening to the previous one first.
        public void Inject(IPlayerHealth dependency)
        {
            if (playerHealth != null)
            {
                playerHealth.OnPlayerDied -= HandlePlayerDied;
            }

            playerHealth = dependency;

            if (playerHealth != null)
            {
                playerHealth.OnPlayerDied += HandlePlayerDied;
            }
        }

        // Losing all health ends the run. RequestGameOver only works from Playing,
        // so a death reported while paused or already in GameOver is ignored.
        private void HandlePlayerDied()
        {
            RequestGameOver();
        }

        #endregion

        #region State Requests

        public void RequestStart()
        {
            if (CurrentStateType == GameStateType.Menu)
            {
                machine.ChangeState(new PlayingState());
            }
        }

        public void RequestPause()
        {
            if (CurrentStateType == GameStateType.Playing)
            {
                machine.ChangeState(new PauseState());
            }
        }

        public void RequestResume()
        {
            if (CurrentStateType == GameStateType.Paused && PreviousStateType == GameStateType.Playing)
            {
                machine.ChangeState(PreviousState);
            }
        }

        public void RequestGameOver()
        {
            if (CurrentStateType == GameStateType.Playing)
            {
                machine.ChangeState(new GameOverState());
            }
        }

        public void RequestRestart()
        {
            if (isReloading || (CurrentStateType != GameStateType.GameOver && CurrentStateType != GameStateType.Paused))
            {
                return;
            }

            ReloadSceneInto(new PlayingState());
        }

        public void RequestReturnToMenu()
        {
            if (isReloading)
            {
                return;
            }

            if (CurrentStateType == GameStateType.Playing || CurrentStateType == GameStateType.Paused || CurrentStateType == GameStateType.GameOver)
            {
                // Leaving a run must reset it: Start from the menu begins a new run (IsNewRun), so the player, enemies
                // and level have to be fresh. Without the reload, Start continued the old run, e.g. with a dead player.
                ReloadSceneInto(new MenuState());
            }
        }

        public void RequestQuit()
        {
            if (CurrentStateType == GameStateType.Menu)
            {
                Application.Quit();
            }
        }

        #endregion

        #region Scene Reload

        private void ReloadSceneInto(GameState nextState)
        {
            isReloading = true;
            stateAfterReload = nextState;
            SceneManager.sceneLoaded += OnReloadedSceneLoaded;
            reloadScene();
        }

        private void OnReloadedSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            SceneManager.sceneLoaded -= OnReloadedSceneLoaded;
            GameState nextState = stateAfterReload;
            stateAfterReload = null;
            machine.ChangeState(nextState);
            isReloading = false;
        }

        private static void ReloadActiveScene()
        {
            Scene active = SceneManager.GetActiveScene();
            if (active.buildIndex >= 0)
            {
                SceneManager.LoadScene(active.buildIndex);
                return;
            }

#if UNITY_EDITOR
            // Scenes not added to Build Settings can still be reloaded while testing in the Editor.
            UnityEditor.SceneManagement.EditorSceneManager.LoadSceneInPlayMode(
                active.path, new LoadSceneParameters(LoadSceneMode.Single));
#else
            Debug.LogError($"GameStateManager: scene '{active.name}' is not in Build Settings, so it can't be reloaded for restart.");
#endif
        }

        #endregion

        #region Event Dispatch

        private void HandleStateChanged(GameState state)
        {
            GameStateType previous = PreviousStateType;
            GameStateType current = CurrentStateType;

            // A restart also leaves the paused state, but it begins a new run rather than resuming one.
            bool isNewRun = current == GameStateType.Playing && (previous != GameStateType.Paused || isReloading);

            OnStateTypeChanged?.Invoke(new GameStateChange(previous, current, isNewRun));
        }

        private static GameStateType GetStateType(GameState state)
        {
            return state == null ? GameStateType.None : state.Type;
        }

        #endregion
    }
}
