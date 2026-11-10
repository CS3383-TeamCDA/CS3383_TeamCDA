using UnityEngine;

namespace EscapeThe90s.GameStates
{
    public class GameOverState : GameState
    {
        private float previousTimeScale = 1f;
        private CursorLockMode previousCursorLockMode = CursorLockMode.None;
        private bool previousCursorVisible = true;

        public override void Enter()
        {
            previousTimeScale = Time.timeScale;
            previousCursorLockMode = Cursor.lockState;
            previousCursorVisible = Cursor.visible;

            // Stop gameplay (enemies, projectiles) once the player has lost.
            // The game over panel is shown by HUDController through OnStateChanged, not here.
            Time.timeScale = 0f;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        public override void Update()
        {
            // Other systems (camera, player input) may lock or hide the cursor; the game over buttons must stay clickable.
            if (Cursor.lockState != CursorLockMode.None || !Cursor.visible)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
        }

        public override void Exit()
        {
            // Restores only what Enter() changed; the next state's Enter() runs afterward and sets its own values.
            Time.timeScale = previousTimeScale;
            Cursor.lockState = previousCursorLockMode;
            Cursor.visible = previousCursorVisible;
        }
    }
}
