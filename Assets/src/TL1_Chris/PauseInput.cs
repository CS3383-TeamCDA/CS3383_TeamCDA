using EscapeThe90s.GameStates;
using UnityEngine;
using UnityEngine.InputSystem;

namespace EscapeThe90s
{
    /// <summary>
    /// Toggles pause with the Escape key by sending requests to GameStateManager.
    /// Lives on an always-active object (the GameStateManager prefab) because the pause panel
    /// is disabled while hidden and could not listen for the key itself.
    /// Update still runs when Time.timeScale is 0, so Escape can also resume.
    /// </summary>
    public class PauseInput : MonoBehaviour
    {
        private void Update()
        {
            if (Keyboard.current == null || !Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                return;
            }

            GameStateManager manager = GameStateManager.Instance;
            if (manager == null)
            {
                return;
            }

            // The manager validates both requests; Escape does nothing in Menu or GameOver.
            if (manager.CurrentState is PlayingState)
            {
                manager.RequestPause();
            }
            else if (manager.CurrentState is PauseState)
            {
                manager.RequestResume();
            }
        }
    }
}
