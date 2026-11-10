using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using EscapeThe90s.State;
using EscapeThe90s.GameStates;
using EscapeThe90s.Contracts;

namespace EscapeThe90s
{
    public class GameStateManager : MonoBehaviour, IGameState
    {
        #region Fields

        private readonly StateMachine<GameState> machine = new StateMachine<GameState>();
        private bool isRestarting;

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
            SceneManager.sceneLoaded -= OnRestartSceneLoaded;
            Instance = null;
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
            if (isRestarting || (CurrentStateType != GameStateType.GameOver && CurrentStateType != GameStateType.Paused))
            {
                return;
            }

            // Reloading the scene resets the runtime (level, player, enemies, HUD start values).
            // This manager survives the reload (DontDestroyOnLoad) and enters PlayingState once the new scene is loaded,
            // so only the fresh scene's objects receive the state change.
            isRestarting = true;
            SceneManager.sceneLoaded += OnRestartSceneLoaded;
            ReloadActiveScene();
        }

        public void RequestReturnToMenu()
        {
            if (CurrentStateType == GameStateType.Playing || CurrentStateType == GameStateType.Paused || CurrentStateType == GameStateType.GameOver)
            {
                machine.ChangeState(new MenuState());
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

        #region Restart

        private void OnRestartSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            SceneManager.sceneLoaded -= OnRestartSceneLoaded;
            machine.ChangeState(new PlayingState());
            isRestarting = false;
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
            bool isNewRun = current == GameStateType.Playing && (previous != GameStateType.Paused || isRestarting);

            OnStateTypeChanged?.Invoke(new GameStateChange(previous, current, isNewRun));
        }

        private static GameStateType GetStateType(GameState state)
        {
            return state == null ? GameStateType.None : state.Type;
        }

        #endregion
    }
}
