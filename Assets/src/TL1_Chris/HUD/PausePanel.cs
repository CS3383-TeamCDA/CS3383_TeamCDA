using UnityEngine;

namespace EscapeThe90s.HUD
{
    public class PausePanel : UIPanel
    {
        // Hook these to the panel's buttons (Button > On Click) in the Inspector.
        public void Resume()
        {
            GameStateManager.Instance.RequestResume();
        }

        public void Restart()
        {
            GameStateManager.Instance.RequestRestart();
        }

        public void ReturnToMenu()
        {
            GameStateManager.Instance.RequestReturnToMenu();
        }

        public override void Show()
        {
            base.Show();
        }

        public override void Hide()
        {
            base.Hide();
        }
    }
}
