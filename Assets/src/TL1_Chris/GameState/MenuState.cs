using UnityEngine;

namespace EscapeThe90s.GameStates
{
    public class MenuState : GameState
    {
        private float previousTimeScale = 1f;
        private CursorLockMode previousCursorLockMode = CursorLockMode.None;
        private bool previousCursorVisible = true;

        public override void Enter()
        {
            previousTimeScale = Time.timeScale;
            previousCursorLockMode = Cursor.lockState;
            previousCursorVisible = Cursor.visible;

            // The menu is shown over the gameplay scene, so gameplay must be frozen while it is open.
            // Menu panels are shown by HUDController through OnStateChanged, not here, to keep UI decoupled from state logic.
            Time.timeScale = 0f;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        public override void Update()
        {
            // Other systems (camera, player input) may lock or hide the cursor; the menu must stay clickable.
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
