using UnityEngine;
using UnityEngine.Analytics;
using UnityEngine.UI;

namespace EscapeThe90s.HUD
{
    public class HUDPanel : UIPanel
    {
        [SerializeField] private Text healthText;
        [SerializeField] private Text scoreText;
        [SerializeField] private Text currencyText;
        [SerializeField] private Slider healthBar;
        [SerializeField] private int maximumHealth = 100;

        public override void Show()
        {
            base.Show();
        }

        public override void Hide()
        {
            base.Hide();
        }

        public void UpdateHealthDisplay(int health)
        {
            //healthText.text = $"Health: {hp}";
            //healthBar.value = hp;

            if (healthText != null)
            {
                healthText.text = $"Health: {health}";
            }

            if (healthBar != null)
            {
                healthBar.minValue = 0;
                healthBar.maxValue = Mathf.Max(1, maximumHealth);
                healthBar.value = health;
            }
        }

        public void UpdateScoreDisplay(int score)
        {
            //scoreText.text = $"Score: {score}";
            if (scoreText != null)
            {
                scoreText.text = $"Score: {score}";
            }
        }

        public void UpdateCurrencyDisplay(int currency)
        {
            //currencyText.text = $"Currency: {amount}";
            if (currencyText != null)
            {
                currencyText.text = $"Currency: {currency}";
            }
        }
    }
}
