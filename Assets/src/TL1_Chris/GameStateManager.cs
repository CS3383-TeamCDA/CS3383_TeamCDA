using System;
using UnityEngine;
using EscapeThe90s.GameStates;

namespace EscapeThe90s
{
    public class GameStateManager : MonoBehaviour
    {
        public GameState CurrentState { get; private set; }
        private GameState previousState { get; set; }

        public event Action<GameState> OnStateChanged;
        private bool isChangingState;

        public void Initialize(GameState startState)
        {
            if (startState == null)
            {
                throw new ArgumentNullException(nameof(startState));
            }

            if (CurrentState != null || isChangingState)
            {
                return;
            }

            isChangingState = true;
            try
            {
                previousState = null;
                CurrentState = startState;
                CurrentState.Enter();
                OnStateChanged?.Invoke(CurrentState);
            }
            finally
            {
                isChangingState = false;
            }
        }

        public void ChangeState(GameState newState)
        {
            if (newState == null || isChangingState)
            {
                return;
            }

            if (CurrentState == null)
            {
                Initialize(newState);
                return;
            }

            if (newState.GetType() == CurrentState.GetType())
            {
                return;
            }

            isChangingState = true;
            try
            {
                CurrentState.Exit();

                previousState = CurrentState;
                CurrentState = newState;
                CurrentState.Enter();
                OnStateChanged?.Invoke(CurrentState);
            }
            finally
            {
                isChangingState = false;
            }
        }

        public void Start()
        {
            if (CurrentState == null)
            {
                Initialize(new MenuState());
            }
        }

        public void RequestStart()
        {
            if (CurrentState is MenuState)
            {
                ChangeState(new PlayingState());
            }
        }

        public void RequestPause()
        {
            if (CurrentState is PlayingState)
            {
                ChangeState(new PauseState());
            }
        }

        public void RequestResume()
        {
            if (CurrentState is PauseState)
            {
                ChangeState(previousState);
            }
        }

        public void RequestGameOver()
        {
            if (CurrentState is PlayingState)
            {
                ChangeState(new GameOverState());
            }
        }

        public void RequestRestart()
        {
            if (CurrentState is GameOverState || CurrentState is PauseState)
            {
                ChangeState(new PlayingState());
            }
        }

        public void RequestReturnToMenu()
        {
            if (CurrentState is PlayingState || CurrentState is PauseState || CurrentState is GameOverState)
            {
                ChangeState(new MenuState());
            }
        }

        public void Update()
        {
            if (CurrentState != null)
            {
                CurrentState.Update();
            }
        }

        public GameState GetPreviousState()
        {
            return previousState;
        }
    }
}
