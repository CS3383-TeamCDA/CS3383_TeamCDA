using UnityEngine.EventSystems;
using UnityEngine;
using UnityEngine.UI;
using EscapeThe90s.Contracts;

namespace EscapeThe90s.MetaProgression
{
    /// <summary>
    /// Segmented level bar for one upgrade. One segment is shown per level
    /// purchased; the + button buys the next level immediately.
    /// </summary>
    public class UpgradeSegmentBar : MonoBehaviour
    {
        [SerializeField] private UpgradeType type;
        [SerializeField] private Button plusButton;
        [SerializeField] private Image[] segments;

        private void Start()
        {
            var m = MetaProgressionManager.Instance;
            if (m != null)
            {
                m.OnCurrencyChanged -= HandleCurrencyChanged;
                m.OnCurrencyChanged += HandleCurrencyChanged;
            }
            Refresh();
        }

        private void OnEnable()
        {
            plusButton.onClick.AddListener(OnPlusClicked);
            if (MetaProgressionManager.Instance != null)
                MetaProgressionManager.Instance.OnCurrencyChanged += HandleCurrencyChanged;
            Refresh();
        }

        private void OnDisable()
        {
            plusButton.onClick.RemoveListener(OnPlusClicked);
            if (MetaProgressionManager.Instance != null)
                MetaProgressionManager.Instance.OnCurrencyChanged -= HandleCurrencyChanged;
        }

        private void HandleCurrencyChanged(int _) => Refresh();

        private void OnPlusClicked()
        {
            MetaProgressionManager.Instance?.PurchaseUpgrade(type);
            EventSystem.current?.SetSelectedGameObject(null);
        }

        /// <summary>Shows the first (level) segments and hides the rest.</summary>
        public void Refresh()
        {
            var manager = MetaProgressionManager.Instance;
            int level = manager?.GetUpgradeLevel(type) ?? 0;
            for (int i = 0; i < segments.Length; i++)
                segments[i].enabled = i < level;

            plusButton.interactable = manager != null && manager.CanPurchase(type);
        }
    }
}