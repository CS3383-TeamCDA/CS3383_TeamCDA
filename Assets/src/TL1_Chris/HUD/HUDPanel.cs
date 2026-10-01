using UnityEngine;
using UnityEngine.Analytics;
using UnityEngine.UI;

namespace EscapeThe90s.HUD
{
    public class HUDPanel : UIPanel
    {
        public Text healthText;
        public Text scoreText;
        public Text currencyText;
        public Slider healthBar;

        public override void Show()
        {
            base.Show();
        }

        public override void Hide()
        {
            base.Hide();
        }

        public void UpdateHealthDisplay(int hp)
        {
            healthText.text = $"Health: {hp}";
            healthBar.value = hp;
        }

        public void UpdateScoreDisplay(int score)
        {
            scoreText.text = $"Score: {score}";
        }

        public void UpdateCurrencyDisplay(int amount)
        {
            currencyText.text = $"Currency: {amount}";
        }
    }
}
