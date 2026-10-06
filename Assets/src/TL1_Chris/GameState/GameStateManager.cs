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
        }

        public void ChangeState(GameState newState)
        {
            // Code here
            previousState = currentState;
            currentState = newState;
        }

        public void RequestStart()
        {
            // Code here

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
            // Code here
        }

        public GameState GetCurrentState()
        {
            return currentState;
        }
    }
}
