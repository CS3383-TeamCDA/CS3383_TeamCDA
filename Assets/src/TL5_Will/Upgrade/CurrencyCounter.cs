using TMPro;
using UnityEngine;

namespace EscapeThe90s.MetaProgression
{
    /// <summary>Shows the player's current currency in the upgrade menu.</summary>
    public class CurrencyCounter : MonoBehaviour
    {
        [SerializeField] private TMP_Text label;

        private void OnEnable()
        {
            var manager = MetaProgressionManager.Instance;
            if (manager != null)
                manager.OnCurrencyChanged += Refresh;
            Refresh(manager != null ? manager.Currency : 0);
        }

        private void OnDisable()
        {
            if (MetaProgressionManager.Instance != null)
                MetaProgressionManager.Instance.OnCurrencyChanged -= Refresh;
        }

        private void Refresh(int currency)
        {
            label.text = currency.ToString();
        }
    }
}