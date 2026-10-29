using TMPro;
using UnityEngine;

namespace EscapeThe90s.Stats
{
    public class StatsPanel : MonoBehaviour
    {
        [SerializeField] private TMP_Text statsText;

        public void Open()
        {
            gameObject.SetActive(true);
        }

        public void Close()
        {
            gameObject.SetActive(false);
        }
    }
}
