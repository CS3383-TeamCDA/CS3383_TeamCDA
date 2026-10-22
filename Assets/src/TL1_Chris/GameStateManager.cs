using System;
using UnityEngine;
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
            if (CurrentState is GameOverState || CurrentState is PauseState)
            {
                machine.ChangeState(new PlayingState());
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
