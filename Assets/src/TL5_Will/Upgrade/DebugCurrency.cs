using UnityEngine;
using UnityEngine.InputSystem;

namespace EscapeThe90s.MetaProgression
{
    /// <summary>Testing only: press C to add currency, X to remove it.</summary>
    public class DebugCurrencyCheat : MonoBehaviour
    {
        [SerializeField] private int amount = 100;

        private void Update()
        {
            var keyboard = Keyboard.current;
            var manager = MetaProgressionManager.Instance;
            if (keyboard == null || manager == null)
                return;

            if (keyboard.cKey.wasPressedThisFrame)
                manager.AddCurrency(amount);
            else if (keyboard.xKey.wasPressedThisFrame)
                manager.RemoveCurrency(amount);
        }
    }
}