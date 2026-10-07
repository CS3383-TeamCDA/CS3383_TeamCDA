using UnityEngine;

namespace EscapeThe90s.PowerUps
{
    /// <summary>
    /// Abstract base for every power-up effect.
    /// Strategy pattern: the Abstraction that PowerUpManager (the Client) calls.
    /// Memento pattern: the Originator that creates and restores its own StatSnapshot.
    /// </summary>
    public abstract class PowerUpEffect
    {
        /// <summary>Seconds the effect lasts. Ignored when IsPermanent is true.</summary>
        public float Duration { get; protected set; }

        /// <summary>True for effects that never expire (ShieldEffect).</summary>
        public bool IsPermanent { get; protected set; }

        /// <summary>Most stacks this effect allows at once. Always 1 or more.</summary>
        public int MaxStacks { get; protected set; }

        protected PowerUpEffect(float duration, bool isPermanent, int maxStacks)
        {
            Duration = duration;
            IsPermanent = isPermanent;
            MaxStacks = maxStacks;
        }

        public virtual void Apply(IPowerUpTarget target, int stackCount)
        {
            Debug.Log($"[PowerUpEffect base] Apply on {GetType().Name}, stack {stackCount}: generic behavior only.");
        }

        /// <summary>Undoes a temporary effect by restoring the saved stat.</summary>
        public virtual void Remove(IPowerUpTarget target, StatSnapshot snapshot)
        {
            Debug.Log($"[PowerUpEffect base] Remove on {GetType().Name}: nothing to restore.");
        }

        /// <summary>Saves the one stat this effect is about to change. Base returns null (nothing to save).</summary>
        public virtual StatSnapshot CreateSnapshot(IPowerUpTarget target)
        {
            Debug.Log($"[PowerUpEffect base] CreateSnapshot on {GetType().Name}: no stat saved, returning null.");
            return null;
        }
    }
}
