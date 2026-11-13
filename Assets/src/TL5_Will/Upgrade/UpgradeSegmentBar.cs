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
        private void OnEnable()
        {
            plusButton.onClick.AddListener(OnPlusClicked);
            Refresh();
        }

        private void Start()
        {
            Refresh();
        }

        private void OnDisable()
        {
            plusButton.onClick.RemoveListener(OnPlusClicked);
        }

        private void OnPlusClicked()
        {
            MetaProgressionManager.Instance?.PurchaseUpgrade(type);
            Refresh();
            EventSystem.current?.SetSelectedGameObject(null); // clear Selected so hover glow keeps working
        }

        /// <summary>Shows the first (level) segments and hides the rest.</summary>
        public void Refresh()
        {
            int level = MetaProgressionManager.Instance?.GetUpgradeLevel(type) ?? 0;
            for (int i = 0; i < segments.Length; i++)
                segments[i].enabled = i < level;
        }
    }
}