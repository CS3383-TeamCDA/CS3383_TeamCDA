using UnityEngine;

namespace EscapeThe90s.HUD
{
    public class MainMenuPanel : UIPanel
    {
        [SerializeField] private GameStateManager _gameStateManager;

        public void StartGame()
        {
            _gameStateManager.RequestStart();
            Debug.Log(_gameStateManager.CurrentState);
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
