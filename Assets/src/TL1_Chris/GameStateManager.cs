using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using EscapeThe90s.State;
using EscapeThe90s.GameStates;

namespace EscapeThe90s
{
    public class GameStateManager : MonoBehaviour
    {
        #region Properties

        public static GameStateManager Instance { get; private set; }
        private StateMachine<GameState> machine = new StateMachine<GameState>();
        public GameState CurrentState => machine.CurrentState;
        public GameState PreviousState => machine.PreviousState;
        private bool isRestarting;

        #endregion

        #region Events

        public event Action<GameState> OnStateChanged
        {
            add { machine.OnStateChanged += value; }
            remove { machine.OnStateChanged -= value; }
        }

        #endregion

        #region private Methods

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;

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

        #endregion

        #region public Methods

        public void RequestStart()
        {
            if (CurrentState is MenuState)
            {
                machine.ChangeState(new PlayingState());
            }
        }

        public void RequestPause()
        {
            if (CurrentState is PlayingState)
            {
                machine.ChangeState(new PauseState());
            }
        }

        public void RequestResume()
        {
            if (CurrentState is PauseState && PreviousState is PlayingState)
            {
                machine.ChangeState(PreviousState);
            }
        }

        public void RequestGameOver()
        {
            if (CurrentState is PlayingState)
            {
                machine.ChangeState(new GameOverState());
            }
        }

        public void RequestRestart()
        {
            if (isRestarting || !(CurrentState is GameOverState || CurrentState is PauseState))
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

        private void OnRestartSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            SceneManager.sceneLoaded -= OnRestartSceneLoaded;
            isRestarting = false;
            machine.ChangeState(new PlayingState());
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

        public void RequestQuit()
        {
            if (CurrentState is MenuState)
            {
                Application.Quit();
            }
        }

        public void RequestReturnToMenu()
        {
            if (CurrentState is PlayingState || CurrentState is PauseState || CurrentState is GameOverState)
            {
                machine.ChangeState(new MenuState());
            }
        }

        #endregion

    }
}
