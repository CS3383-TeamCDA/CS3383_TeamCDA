/*
 * ===================================================================
 *  STRATEGY DESIGN PATTERN
 * ===================================================================
 *
 *  Intent
 *    - Define a family of interchangeable behaviors, wrap each one in
 *      its own class, and let the caller swap between them freely.
 *    - Keep the caller depending on an abstraction, not on any one
 *      concrete behavior.
 *    - Add new behaviors without changing the code that uses them.
 *
 *  Problem
 *    Each power-up does something different when collected (faster
 *    movement, a shield, a bigger pickup radius). Putting all of that
 *    in PowerUpManager as a big switch statement would mean editing the
 *    manager every time a new power-up is added.
 *
 *  How it works here
 *    1. PowerUpEffect (this class) declares Apply() and Remove() as
 *       virtual, with a generic base version.
 *    2. Each concrete effect overrides them with its own behavior.
 *    3. PowerUpManager keeps a Dictionary<PowerUpType, PowerUpEffect>
 *       called strategies. When a power-up is collected it looks up the
 *       effect by type and calls effect.Apply(target, stacks).
 *    4. The manager only ever sees a PowerUpEffect reference. Dynamic
 *       binding picks the right subclass's Apply() at run time.
 *    Adding a new power-up means writing one new subclass and adding
 *    one dictionary entry. PowerUpManager does not change.
 *
 *  Roles
 *    Client          - PowerUpManager
 *                      Chooses which strategy to use and calls it.
 *    Abstraction     - PowerUpEffect (this class)
 *                      The common interface every strategy shares.
 *    Implementations - SpeedBoostEffect, ShieldEffect, MagnetEffect
 *                      The interchangeable behaviors.
 *
 *  Note: PowerUpEffect is also the Originator in the Memento pattern
 *  (see StatSnapshot). Strategy picks what the effect does; Memento
 *  lets that effect be undone when it expires.
 *
 *  Source: https://sourcemaking.com/design_patterns/strategy
 * ===================================================================
 */

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
