using EscapeThe90s.Contracts;
using UnityEngine;

namespace EscapeThe90s.HUD
{
    /// <summary>
    /// Keeps the values on the gameplay HUD up to date: health (from IPlayerHealth), currency (from IMetaProgressionManager) and score.
    /// Which screen is visible for each game state is handled separately by UIPanelController.
    /// </summary>
    public class HUDController : MonoBehaviour, IDependent<IMetaProgressionManager>, IDependent<IPlayerHealth>, IDependent<IGameState>
    {
        [SerializeField] private HUDPanel hudPanel;

        [SerializeField] private int startingHealth = 100;
        [SerializeField] private int startingScore = 0;
        [SerializeField] private int startingCurrency = 0;

        private int currentHealth;
        private int currentScore;
        private int currentCurrency;
        private IMetaProgressionManager progression;
        private IPlayerHealth playerHealth;
        private IGameState gameState;

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

        // Called by ServiceContainer (via DependencyScope), which registers the scene's PlayerHealth as IPlayerHealth.
        public void Inject(IPlayerHealth dependency)
        {
            if (playerHealth != null)
            {
                playerHealth.OnHealthChanged -= HandlePlayerHealthChanged;
            }

            playerHealth = dependency;

            if (playerHealth != null)
            {
                playerHealth.OnHealthChanged += HandlePlayerHealthChanged;
                ShowPlayerHealth();
            }
        }

        // Called by ServiceContainer (via DependencyScope), which registers GameStateManager as IGameState.
        public void Inject(IGameState dependency)
        {
            if (gameState != null)
            {
                gameState.OnStateTypeChanged -= HandleStateTypeChanged;
            }

            gameState = dependency;

            if (gameState != null)
            {
                gameState.OnStateTypeChanged += HandleStateTypeChanged;
            }
        }

        private void HandlePlayerHealthChanged(int current, int max)
        {
            UpdateHealth(current);
        }

        private void HandleStateTypeChanged(GameStateChange change)
        {
            // By the time a run starts the player is active, so its real health is available.
            if (change.Current == GameStateType.Playing)
            {
                ShowPlayerHealth();
            }
        }

        // Shows the player's current health. Injection can happen before PlayerHealth.Awake has set it
        // (the player can start inactive), which reads as 0; a player can't be dead before the game starts,
        // so show full health in that case. It is read again when a run starts (see HandleStateTypeChanged).
        private void ShowPlayerHealth()
        {
            if (playerHealth == null || hudPanel == null)
            {
                return;
            }

            int health = playerHealth.CurrentHealth;
            UpdateHealth(health > 0 ? health : playerHealth.MaxHealth);
        }

        private void OnDestroy()
        {
            if (progression != null)
            {
                progression.OnCurrencyChanged -= UpdateCurrency;
            }

            if (playerHealth != null)
            {
                playerHealth.OnHealthChanged -= HandlePlayerHealthChanged;
            }

            // GameStateManager survives scene reloads, so this HUD must stop listening when it is destroyed.
            if (gameState != null)
            {
                gameState.OnStateTypeChanged -= HandleStateTypeChanged;
            }
        }

        private void Awake()
        {
            // The HUD panel is nested inside the HUD prefab, so find it there if the field was left empty.
            if (hudPanel == null)
            {
                hudPanel = GetComponentInChildren<HUDPanel>(true);
            }

            // Injection may already have happened (Awake order isn't guaranteed), so prefer the real balance.
            int initialCurrency = progression != null ? progression.Currency : startingCurrency;
            Initialize(startingHealth, startingScore, initialCurrency);
            ShowPlayerHealth();
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
    }
}
