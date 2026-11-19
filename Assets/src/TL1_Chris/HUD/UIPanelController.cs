using EscapeThe90s.GameStates;
using EscapeThe90s.HUD;
using UnityEngine;

namespace EscapeThe90s.UI
{
    /// <summary>
    /// Shows the right screen for the current game state: main menu, gameplay HUD, pause panel or game over panel.
    /// Its only job is which panel is visible; the values on the HUD are kept up to date by HUDController.
    /// </summary>
    public class UIPanelController : MonoBehaviour
    {
        [SerializeField] private MainMenuPanel mainMenuPanel;
        [SerializeField] private HUDPanel hudPanel;
        [SerializeField] private PausePanel pausePanel;
        [SerializeField] private GameOverPanel gameOverPanel;

        private GameState displayedState;

        private void Awake()
        {
            // The HUD, pause and game over panels are nested inside the HUD prefab, so find them there if the fields were left empty.
            // 'true' includes inactive children, since the panels start hidden.
            if (hudPanel == null)
            {
                hudPanel = GetComponentInChildren<HUDPanel>(true);
            }

            if (pausePanel == null)
            {
                pausePanel = GetComponentInChildren<PausePanel>(true);
            }

            if (gameOverPanel == null)
            {
                gameOverPanel = GetComponentInChildren<GameOverPanel>(true);
            }
        }

        private void OnEnable()
        {
            if (GameStateManager.Instance == null)
            {
                return;
            }

            displayedState = GameStateManager.Instance.CurrentState;
            if (displayedState != null)
            {
                OnStateChange(displayedState);
            }
            else
            {
                if (mainMenuPanel != null)
                {
                    mainMenuPanel.Show();
                }

                if (hudPanel != null)
                {
                    hudPanel.Hide();
                }
            }
        }

        private void Update()
        {
            if (GameStateManager.Instance == null)
            {
                return;
            }

            GameState activeState = GameStateManager.Instance.CurrentState;
            if (activeState != displayedState)
            {
                displayedState = activeState;
                OnStateChange(activeState);
            }
        }

        public void Show()
        {
            if (hudPanel != null && GameStateManager.Instance != null && GameStateManager.Instance.CurrentState is PlayingState)
            {
                hudPanel.Show();
            }
        }

        public void Hide()
        {
            if (hudPanel != null)
            {
                hudPanel.Hide();
            }
        }

        private void OnStateChange(GameState state)
        {
            // The main menu is a separate prefab, so it can't be assigned on the HUD prefab. Find it in the scene.
            // Look each time until found: it may be hidden (inactive) by other systems when the game starts.
            if (mainMenuPanel == null)
            {
                mainMenuPanel = FindAnyObjectByType<MainMenuPanel>(FindObjectsInactive.Include);
            }

            // The game over menu is its own prefab in the scene too, so it isn't under the HUD either.
            if (gameOverPanel == null)
            {
                gameOverPanel = FindAnyObjectByType<GameOverPanel>(FindObjectsInactive.Include);
            }

            if (mainMenuPanel != null)
            {
                mainMenuPanel.Hide();
            }

            if (pausePanel != null)
            {
                pausePanel.Hide();
            }

            if (gameOverPanel != null)
            {
                gameOverPanel.Hide();
            }

            if (hudPanel != null)
            {
                hudPanel.Hide();
            }

            if (state == null || state is MenuState)
            {
                if (mainMenuPanel != null)
                {
                    mainMenuPanel.Show();
                }
            }
            else if (state is PlayingState)
            {
                if (hudPanel != null)
                {
                    hudPanel.Show();
                }
            }
            else if (state is PauseState)
            {
                // Gameplay HUD stays hidden while paused (doc Scenario 2, step 5a); values are kept.
                if (pausePanel != null)
                {
                    pausePanel.Show();
                }
                else
                {
                    Debug.LogWarning("UIPanelController: no PausePanel assigned or found under this HUD, so nothing is shown while paused.", this);
                }
            }
            else if (state is GameOverState)
            {
                // Gameplay HUD stays hidden at game over too; the run's values are kept until a restart reloads the scene.
                if (gameOverPanel != null)
                {
                    gameOverPanel.Show();
                }
                else
                {
                    Debug.LogWarning("UIPanelController: no GameOverPanel assigned or found under this HUD, so nothing is shown at game over.", this);
                }
            }
        }
    }
}
