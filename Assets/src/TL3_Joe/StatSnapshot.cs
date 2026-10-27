/*
 * ===================================================================
 *  MEMENTO DESIGN PATTERN
 * ===================================================================
 *
 *  Intent
 *    - Save an object's internal state outside the object, without
 *      breaking its encapsulation, so it can be put back later.
 *    - Acts like a sealed "checkpoint" token: whoever holds it can
 *      return it, but cannot open it.
 *    - Turns undo / rollback into a real object instead of ad hoc code.
 *
 *  Problem
 *    A temporary power-up (Speed Boost, Magnet) changes a player stat.
 *    When the effect expires, that stat must go back to exactly what it
 *    was before the power-up was applied.
 *
 *  How it works here
 *    1. Before applying an effect, PowerUpManager asks the effect for a
 *       snapshot: effect.CreateSnapshot(target).
 *    2. The effect fills a StatSnapshot with the one stat it is about
 *       to change and that stat's current value.
 *    3. PowerUpManager stores the snapshot in its snapshots collection
 *       (paired with the effect and timer in ActiveEffect). It never
 *       reads or changes what is inside.
 *    4. When the timer expires, PowerUpManager hands the snapshot back:
 *       effect.Remove(target, snapshot), and the effect restores the
 *       saved value.
 *    Permanent effects (Shield) return null from CreateSnapshot(),
 *    since there is nothing to restore.
 *
 *  Roles
 *    Originator  - PowerUpEffect
 *                  Knows how to save and restore its own changes.
 *    Caretaker   - PowerUpManager
 *                  Knows WHEN to save and restore, holds the snapshots.
 *    Memento     - StatSnapshot (this class)
 *                  The locked box: written and read only by the
 *                  Originator, carried around by the Caretaker.
 *
 *  Note: pairing a stack of Command objects with a stack of Mementos
 *  is a common way to build unlimited undo / redo.
 *
 *  Source: https://sourcemaking.com/design_patterns/memento
 * ===================================================================
 */

namespace EscapeThe90s.PowerUps
{
    /// <summary>Which player stat a snapshot holds.</summary>
    public enum StatKind
    {
        SpeedMultiplier,
        PickupRadius
    }

    /// <summary>
    /// Memento: one saved stat and its value. Immutable once created.
    /// PowerUpManager (the Caretaker) stores these but never reads inside them.
    /// </summary>
    public sealed class StatSnapshot
    {
        public StatKind Stat { get; }
        public float Value { get; }

        public StatSnapshot(StatKind stat, float value)
        {
            Stat = stat;
            Value = value;
        }
    }
}
