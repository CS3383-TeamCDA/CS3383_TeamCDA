using UnityEngine;

namespace EscapeThe90s.HUD
{
    public class UIPanel
    {
        public GameObject root;
        public bool isVisible { get; private set; }

        public virtual void Show()
        {
            root.SetActive(true);
            isVisible = true;
        }

        public virtual void Hide()
        {
            root.SetActive(false);
            isVisible = false;
        }

        public virtual void Update()
        {
            
        }

    }
}
