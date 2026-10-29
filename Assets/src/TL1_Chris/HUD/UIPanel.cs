using UnityEngine;

namespace EscapeThe90s.HUD
{
    public class UIPanel : MonoBehaviour
    {
        public bool isVisible { get; private set; }

        public virtual void Show()
        {
            gameObject.SetActive(true);
        }

        public virtual void Hide()
        {
            gameObject.SetActive(false);
        }

        public virtual void Update()
        {
            
        }

    }
}
