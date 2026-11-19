using System;
using UnityEngine;
using EscapeThe90s.Stats;

namespace EscapeThe90s.HUD
{
    public class MainMenuPanel : UIPanel
    {
        [SerializeField] private StatsPanel statsPanel;

        public void StartGame()
        {
            GameStateManager.Instance.RequestStart();
        }

        public void OpenStats()
        {
            statsPanel.Open();
        }

        public void CloseStats()
        {
            statsPanel.Close();
        }

        // Opens TL5's upgrade menu over the main menu; its Back button returns here.
        public void OpenUpgrades()
        {
            UpgradeMenuPanel.OpenFrom(this);
        }

        public void QuitGame()
        {
            GameStateManager.Instance.RequestQuit();
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
