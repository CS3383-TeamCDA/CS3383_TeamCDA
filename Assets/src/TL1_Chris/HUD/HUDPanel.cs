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

        [Tooltip("Heart icons ordered left to right. If left empty, the children of a child object named 'Hearts' are used.")]
        [SerializeField] private GameObject[] hearts;
        [Tooltip("How many hearts represent full health. Extra slots stay hidden (e.g. for a future max-health power-up).")]
        [SerializeField, Min(1)] private int heartsInUse = 5;

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

            UpdateHearts(health);
        }

        private void UpdateHearts(int health)
        {
            if (hearts == null || hearts.Length == 0)
            {
                hearts = FindHeartsUnderContainer();
            }

            int usableHearts = Mathf.Min(heartsInUse, hearts.Length);
            if (usableHearts == 0)
            {
                return;
            }

            // Each heart is worth maxHealth / usableHearts (20% with 5 hearts). A heart only disappears once its whole
            // share is gone, so 81-99% still shows 5 hearts and 80% shows 4. Integer ceiling avoids float rounding.
            int maxHealth = Mathf.Max(1, maximumHealth);
            int clampedHealth = Mathf.Clamp(health, 0, maxHealth);
            int heartsShown = (clampedHealth * usableHearts + maxHealth - 1) / maxHealth;

            // Hearts are ordered left to right, so hiding the higher indices removes them from right to left.
            for (int i = 0; i < hearts.Length; i++)
            {
                if (hearts[i] != null)
                {
                    hearts[i].SetActive(i < heartsShown);
                }
            }
        }

        private GameObject[] FindHeartsUnderContainer()
        {
            foreach (Transform child in GetComponentsInChildren<Transform>(true))
            {
                if (child.name == "Hearts")
                {
                    GameObject[] found = new GameObject[child.childCount];
                    for (int i = 0; i < child.childCount; i++)
                    {
                        found[i] = child.GetChild(i).gameObject;
                    }
                    return found;
                }
            }

            return new GameObject[0];
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
                currencyText.text = $"{currency}";
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
