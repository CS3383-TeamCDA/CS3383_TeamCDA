using System;
using System.Collections.Generic;
using EscapeThe90s.Contracts;
using TMPro;
using UnityEngine;

namespace EscapeThe90s.HUD
{
    /// <summary>
    /// Shows which power-ups are active on the HUD, with their stack count and time left.
    /// Listens to TL3's IPowerUpSpawner contract (injected by ServiceContainer).
    /// </summary>
    /// <remarks>
    /// The contract reports pickups but not expiry, so the time left is counted down here. TL3 counts down with
    /// Time.deltaTime too, so both stop while the game is paused and run out together.
    /// </remarks>
    public class PowerUpHUD : MonoBehaviour, IDependent<IPowerUpSpawner>
    {
        /// <summary>One HUD icon for one power-up type. Assign these in the Inspector.</summary>
        [Serializable]
        public class PowerUpSlot
        {
            public PowerUpType type;
            [Tooltip("Shown while this power-up is active, hidden otherwise.")]
            public GameObject icon;
            [Tooltip("Optional. Shows the stack count, e.g. x2, when more than one is stacked.")]
            public TMP_Text stackText;
            [Tooltip("Optional. Shows the seconds left; empty for permanent power-ups.")]
            public TMP_Text timerText;
        }

        private class ActiveEffect
        {
            public int Stacks;
            public float RemainingSeconds;
            public bool IsPermanent;
        }

        [SerializeField] private PowerUpSlot[] slots = new PowerUpSlot[0];

        private readonly Dictionary<PowerUpType, ActiveEffect> activeEffects = new Dictionary<PowerUpType, ActiveEffect>();
        private readonly List<PowerUpType> expired = new List<PowerUpType>();
        private IPowerUpSpawner spawner;

        // Called by ServiceContainer (via DependencyScope), which registers the scene's PowerUpManager as IPowerUpSpawner.
        public void Inject(IPowerUpSpawner dependency)
        {
            if (spawner != null)
            {
                spawner.OnPowerUpCollected -= HandlePowerUpCollected;
            }

            spawner = dependency;

            if (spawner != null)
            {
                spawner.OnPowerUpCollected += HandlePowerUpCollected;
            }
        }

        public bool IsActive(PowerUpType type)
        {
            return activeEffects.ContainsKey(type);
        }

        public int GetStacks(PowerUpType type)
        {
            return activeEffects.TryGetValue(type, out ActiveEffect effect) ? effect.Stacks : 0;
        }

        public float GetRemainingSeconds(PowerUpType type)
        {
            return activeEffects.TryGetValue(type, out ActiveEffect effect) ? effect.RemainingSeconds : 0f;
        }

        private void Awake()
        {
            // Nothing is active at the start of a run.
            Refresh();
        }

        private void Update()
        {
            Tick(Time.deltaTime);
        }

        private void OnDestroy()
        {
            if (spawner != null)
            {
                spawner.OnPowerUpCollected -= HandlePowerUpCollected;
            }
        }

        /// <summary>Counts down active power-ups and hides the ones that have run out.</summary>
        public void Tick(float deltaTime)
        {
            // deltaTime is 0 while paused, so nothing counts down then.
            if (deltaTime <= 0f || activeEffects.Count == 0)
            {
                return;
            }

            expired.Clear();
            foreach (KeyValuePair<PowerUpType, ActiveEffect> pair in activeEffects)
            {
                if (pair.Value.IsPermanent)
                {
                    continue;
                }

                pair.Value.RemainingSeconds -= deltaTime;
                if (pair.Value.RemainingSeconds <= 0f)
                {
                    expired.Add(pair.Key);
                }
            }

            foreach (PowerUpType type in expired)
            {
                activeEffects.Remove(type);
            }

            Refresh();
        }

        private void HandlePowerUpCollected(PowerUpCollectedInfo info)
        {
            // A timed power-up with no time left is applied and ended by TL3 in the same frame (e.g. an instant heal),
            // so there is nothing to show.
            if (!info.IsPermanent && info.RemainingSeconds <= 0f)
            {
                activeEffects.Remove(info.Type);
                Refresh();
                return;
            }

            // Picking one up again stacks it and restarts its timer, which the info already reflects.
            activeEffects[info.Type] = new ActiveEffect
            {
                Stacks = Mathf.Max(1, info.StackCount),
                RemainingSeconds = info.RemainingSeconds,
                IsPermanent = info.IsPermanent
            };
            Refresh();
        }

        private void Refresh()
        {
            foreach (PowerUpSlot slot in slots)
            {
                if (slot == null)
                {
                    continue;
                }

                bool isActive = activeEffects.TryGetValue(slot.type, out ActiveEffect effect);

                if (slot.icon != null)
                {
                    slot.icon.SetActive(isActive);
                }

                if (slot.stackText != null)
                {
                    slot.stackText.text = isActive && effect.Stacks > 1 ? $"x{effect.Stacks}" : "";
                }

                if (slot.timerText != null)
                {
                    slot.timerText.text = isActive && !effect.IsPermanent ? $"{Mathf.CeilToInt(effect.RemainingSeconds)}s" : "";
                }
            }
        }
    }
}
