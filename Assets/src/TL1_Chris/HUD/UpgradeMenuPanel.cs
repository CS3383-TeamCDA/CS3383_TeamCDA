using UnityEngine;

namespace EscapeThe90s.HUD
{
    /// <summary>
    /// Shows TL5's upgrade menu as a sub-screen of another panel (game over or pause).
    /// Put this on the root of the UpgradeMenu object in the scene. Opening it hides the panel that opened it,
    /// and Back shows that panel again. The game state does not change, so time stays frozen while upgrading.
    /// Buying upgrades is handled by TL5's own scripts inside the menu.
    /// </summary>
    public class UpgradeMenuPanel : UIPanel
    {
        private UIPanel returnTo;

        public void Open(UIPanel opener)
        {
            returnTo = opener;
            if (returnTo != null)
            {
                returnTo.Hide();
            }

            Show();
        }

        // Hook this to the menu's Back button (Button > On Click) in the Inspector.
        public void Back()
        {
            Hide();
            if (returnTo != null)
            {
                returnTo.Show();
            }

            returnTo = null;
        }

        // Closes the menu without going back, e.g. when the game state changes underneath it.
        public void Close()
        {
            returnTo = null;
            Hide();
        }

        // The upgrade menu is its own prefab in the scene, not part of the panel that opens it, so look it up.
        public static void OpenFrom(UIPanel opener)
        {
            UpgradeMenuPanel menu = FindAnyObjectByType<UpgradeMenuPanel>(FindObjectsInactive.Include);
            if (menu != null)
            {
                menu.Open(opener);
            }
            else
            {
                Debug.LogWarning("UpgradeMenuPanel: no upgrade menu with an UpgradeMenuPanel in the scene, so Upgrades does nothing.", opener);
            }
        }
    }
}
