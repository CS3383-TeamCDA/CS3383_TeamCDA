using EscapeThe90s.Contracts;
using EscapeThe90s.GameStates;
using UnityEngine;

namespace EscapeThe90s.HUD
{
    public class HUDController : MonoBehaviour, IDependent<IMetaProgressionManager>
    {
        // [SerializeField] private GameStateManager stateManager;
        [SerializeField] private MainMenuPanel mainMenuPanel;
        [SerializeField] private HUDPanel hudPanel;
        [SerializeField] private PausePanel pausePanel;

        [SerializeField] private int startingHealth = 100;
        [SerializeField] private int startingScore = 0;
        [SerializeField] private int startingCurrency = 0;

        private int currentHealth;
        private int currentScore;
        private int currentCurrency;
        private GameState displayedState;
        private IMetaProgressionManager progression;

        // Called by ServiceContainer (via DependencyScope) during scene composition.
        public void Inject(IMetaProgressionManager dependency)
        {
            if (progression != null)
            {
                progression.OnCurrencyChanged -= UpdateCurrency;
            }

            progression = dependency;

            if (progression != null)
            {
                progression.OnCurrencyChanged += UpdateCurrency;
                UpdateCurrency(progression.Currency);
            }
        }

        private void OnDestroy()
        {
            if (progression != null)
            {
                progression.OnCurrencyChanged -= UpdateCurrency;
            }
        }

        private void Awake()
        {
            // PauseMenu is nested inside the HUD prefab, so find it there if the field was left empty.
            // 'true' includes inactive children, since the panel starts hidden.
            if (pausePanel == null)
            {
                pausePanel = GetComponentInChildren<PausePanel>(true);
            }

            // Injection may already have happened (Awake order isn't guaranteed), so prefer the real balance.
            int initialCurrency = progression != null ? progression.Currency : startingCurrency;
            Initialize(startingHealth, startingScore, initialCurrency);
        }

        private void OnEnable()
        {
            // if (stateManager == null)
            if (GameStateManager.Instance == null)
            {
                return;
            }

            // displayedState = stateManager.CurrentState;
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

                hudPanel.Hide();
            }
        }

        private void Update()
        {
            // if (stateManager == null)
            if (GameStateManager.Instance == null)
            {
                return;
            }

            // GameState activeState = stateManager.CurrentState;
            GameState activeState = GameStateManager.Instance.CurrentState;
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
            // if (stateManager != null && stateManager.CurrentState is PlayingState)
            if (GameStateManager.Instance != null && GameStateManager.Instance.CurrentState is PlayingState)
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
            // The main menu is a separate prefab, so it can't be assigned on the HUD prefab. Find it in the scene.
            // Look each time until found: it may be hidden (inactive) by other systems when the game starts.
            if (mainMenuPanel == null)
            {
                mainMenuPanel = FindAnyObjectByType<MainMenuPanel>(FindObjectsInactive.Include);
            }

            if (mainMenuPanel != null)
            {
                mainMenuPanel.Hide();
            }

            if (pausePanel != null)
            {
                pausePanel.Hide();
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
            else if (state is PauseState)
            {
                // Gameplay HUD stays hidden while paused (doc Scenario 2, step 5a); values are kept.
                if (pausePanel != null)
                {
                    pausePanel.Show();
                }
                else
                {
                    Debug.LogWarning("HUDController: no PausePanel assigned or found under this HUD, so nothing is shown while paused.", this);
                }
            }
        }

    }
}
