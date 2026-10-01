using EscapeThe90s.GameStates;

namespace EscapeThe90s.HUD
{
    public class HUDController
    {
        public int health { get; private set; }
        public int score { get; private set; }
        public int currency { get; private set; }

        public void Initialize(int initialHealth = 100, int initialScore = 0, int initialCurrency = 0)
        {
            // Code here
            health = initialHealth;
            score = initialScore;
            currency = initialCurrency;
        }

        public void UpdateHealth(int hp)
        {
            // Code here
            this.health = hp;
        }
        public void UpdateScore(int score)
        {
            // Code here
            this.score = score;
        }

        public void UpdateCurrency(int currency)
        {
            this.currency = currency;
        }

        public void Show()
        {
            // Code here
        }

        public void Hide()
        {
            // Code here
        }

        public void OnStateChange(GameStateType newState)
        {
            // Code here
        }

    }
}
