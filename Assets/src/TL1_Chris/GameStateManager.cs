using System;
using UnityEngine;
using EscapeThe90s.GameStates;

namespace EscapeThe90s
{
    public class GameStateManager : MonoBehaviour
    {
        public GameState currentState { get; private set; }
        public GameState previousState { get; private set; }

        public GameState CurrentState
        {
            get
            {
                return currentState;
            }
        }

        public event Action<GameState> OnStateChanged;
        private bool isChangingState;

        public void Initialize(GameState startState)
        {
            if (startState == null)
            {
                throw new ArgumentNullException(nameof(startState));
            }

            if (currentState != null || isChangingState)
            {
                return;
            }

            isChangingState = true;
            try
            {
                previousState = null;
                currentState = startState;
                currentState.Enter();
                OnStateChanged?.Invoke(currentState);
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

            if (currentState == null)
            {
                Initialize(newState);
                return;
            }

            if (newState.GetType() == currentState.GetType())
            {
                return;
            }

            isChangingState = true;
            try
            {
                currentState.Exit();

                previousState = currentState;
                currentState = newState;
                currentState.Enter();
                OnStateChanged?.Invoke(currentState);
            }
            finally
            {
                isChangingState = false;
            }
        }

        public void Start()
        {
            if (currentState == null)
            {
                Initialize(new MenuState());
            }
        }

        public void RequestStart()
        {
            if (currentState is MenuState)
            {
                ChangeState(new PlayingState());
            }
        }

        public void RequestPause()
        {
            if (currentState is PlayingState)
            {
                ChangeState(new PauseState());
            }
        }

        public void RequestResume()
        {
            if (currentState is PauseState)
            {
                ChangeState(new PlayingState());
            }
        }

        public void RequestRestart()
        {
            if (currentState is PlayingState)
            {
                ChangeState(new PlayingState());
            }
        }

        public void RequestReturnToMenu()
        {
            if (currentState is PlayingState || currentState is PauseState)
            {
                ChangeState(new MenuState());
            }
        }

        public void Update()
        {
            if (currentState != null)
            {
                currentState.Update();
            }
        }

        public GameState GetCurrentState()
        {
            return currentState;
        }
    }
}
