using EscapeThe90s.GameStates;
using UnityEngine;

namespace EscapeThe90s.HUD
{
    public class HUDController : MonoBehaviour
    {
        [SerializeField] private GameStateManager stateManager;
        [SerializeField] private MainMenuPanel mainMenuPanel;
        [SerializeField] private HUDPanel hudPanel;

        [SerializeField] private int startingHealth = 100;
        [SerializeField] private int startingScore = 0;
        [SerializeField] private int startingCurrency = 0;

        private int currentHealth;
        private int currentScore;
        private int currentCurrency;
        private GameState displayedState;

        private void Awake()
        {
            Initialize(startingHealth, startingScore, startingCurrency);
        }

        private void OnEnable()
        {
            if (stateManager == null)
            {
                return;
            }

            displayedState = stateManager.CurrentState;
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

                hudPanel.Hide();
            }
        }

        private void Update()
        {
            if (stateManager == null)
            {
                return;
            }

            GameState activeState = stateManager.CurrentState;
            if (activeState != displayedState)
            {
                displayedState = activeState;
                OnStateChange(activeState);
            }
        }

        public void Initialize(int initialHealth = 100, int initialScore = 0, int initialCurrency = 0)
        {
            UpdateHealth(initialHealth);
            UpdateScore(initialScore);
            UpdateCurrency(initialCurrency);
        }

        public void UpdateHealth(int health)
        {
            currentHealth = Mathf.Max(0, health);
            hudPanel.UpdateHealthDisplay(currentHealth);
        }

        public void UpdateScore(int score)
        {
            currentScore = Mathf.Max(0, score);
            hudPanel.UpdateScoreDisplay(currentScore);
        }

        public void UpdateCurrency(int currency)
        {
            currentCurrency = currency;
            hudPanel.UpdateCurrencyDisplay(currentCurrency);
        }

        public void Show()
        {
            if (stateManager != null && stateManager.CurrentState is PlayingState)
            {
                hudPanel.Show();
            }
        }

        public void Hide()
        {
            hudPanel.Hide();
        }

        private void OnStateChange(GameState state)
        {
            if (mainMenuPanel != null)
            {
                mainMenuPanel.Hide();
            }

            hudPanel.Hide();

            if (state == null || state is MenuState)
            {
                if (mainMenuPanel != null)
                {
                    mainMenuPanel.Show();
                }
            }
            else if (state is PlayingState)
            {
                hudPanel.Show();
            }
        }

    }
}
