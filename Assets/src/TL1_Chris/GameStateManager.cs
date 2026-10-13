using UnityEngine;
using EscapeThe90s.GameStates;

namespace EscapeThe90s
{
    public class GameStateManager : MonoBehaviour
    {
        public GameState currentState { get; private set; }
        public GameState previousState { get; private set; }

        public void Initialize(GameState startState)
        {
            previousState = null;
            currentState = startState;
            currentState.Enter();
        }

        public void ChangeState(GameState newState)
        {
            if (newState == null || newState == currentState)
            {
                return;
            }

            if (currentState != null)
            {
                currentState.Exit();
            }

            previousState = currentState;
            currentState = newState;
            currentState.Enter();
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
            // Code here    
        }

        public void RequestResume()
        {
            // Code Here            
        }

        public void RequestRestart()
        {
            // Code here
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
