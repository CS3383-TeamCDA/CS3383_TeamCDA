using UnityEngine;

namespace EscapeThe90s.HUD
{
    public class GameOverPanel : UIPanel
    {
        // Hook these to the panel's buttons (Button > On Click) in the Inspector.
        // There is no Resume: a finished run can only be restarted or left.
        public void Restart()
        {
            GameStateManager.Instance.RequestRestart();
        }

        public void ReturnToMenu()
        {
            GameStateManager.Instance.RequestReturnToMenu();
        }

        // Opens TL5's upgrade menu over this panel; its Back button returns here.
        public void OpenUpgrades()
        {
            UpgradeMenuPanel.OpenFrom(this);
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
