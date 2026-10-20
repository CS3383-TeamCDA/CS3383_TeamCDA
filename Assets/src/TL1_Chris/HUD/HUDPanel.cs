using UnityEngine;
using TMPro;
using UnityEngine.UI;

namespace EscapeThe90s.HUD
{
    public class HUDPanel : UIPanel
    {
        [SerializeField] private TMP_Text healthText;
        [SerializeField] private TMP_Text scoreText;
        [SerializeField] private TMP_Text currencyText;
        [SerializeField] private TMP_Text ammoCount;
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
            if (scoreText != null)
            {
                scoreText.text = $"Score: {score}";
            }
        }

        public void UpdateCurrencyDisplay(int currency)
        {
            if (currencyText != null)
            {
                currencyText.text = $"Currency: {currency}";
            }
        }

        public void UpdateAmmoDisplay(int ammoInMag, int maxAmmo)
        {
            if (ammoCount != null)
            {
                ammoCount.text = $"{ammoInMag}/{maxAmmo}";
            }
        }
    }
}
