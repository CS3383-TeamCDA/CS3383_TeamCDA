# EnemyProjectile.TryHitPlayer

## Purpose and sources

`public bool TryHitPlayer(Collider2D target)` connects an enemy projectile impact to
TL6's `PlayerHealth.TakeDamage(int)`. TL4 owns impact detection and projectile
removal; TL6 continues to own player health and health/death events.

The requirements come from `Champion_Document_TL4_Enemy_Combat.docx`, especially
scenario EN2, the interface contract, and task 5 (damage/collision integration).
Its system/sequence diagram descriptions identify the same TL4 → TL6 damage
boundary. Existing `EnemyProjectile`, `EnemyRangedAttack`, `PlayerHealth`, and
projectile tests supply the actual current interfaces. Diagram images were not
independently visually reviewed for this change. No separate TL4 interaction-log
file was found; the brief log below records this chat, without inventing earlier
interactions. The reusable Champion prompt kit supplies documentation context.

## Behavior

1. Reject an unlaunched or consumed projectile, a null/disabled/inactive collider,
   or a projectile with zero damage.
2. Ignore the owner's own collider and child colliders.
3. Find `PlayerHealth` on the collider or its parents, supporting child hurtboxes.
   Reject missing, disabled, inactive, or dead health components.
4. Mark the projectile consumed and schedule its destruction **before** invoking
   health listeners. This prevents duplicate hits from multiple colliders or
   callbacks that attempt another hit during `TakeDamage`.
5. Call `health.TakeDamage(Damage)` once and return `true`.

`false` means no damage call occurred and this attempt did not consume the shot.
`Damage` is the snapshot captured by `Launch`; changes to the enemy's level after
launch do not change a shot already in flight. Destruction occurs at Unity's
normal deferred destruction point; the consumed flag takes effect immediately.

`Update` integrates the function into a circle sweep over each frame's travel.
It includes trigger colliders, sorts contacts nearest first, and ignores unrelated
colliders. A player hit consumes the projectile; a collider in `blockingLayers`
also consumes it, preventing a shot from damaging a player behind a wall.
Projectile lifetime expiration remains in place.

## Unity setup and usage

- Keep `EnemyProjectile` on the projectile prefab and launch it through the
  existing `EnemyRangedAttack.TryShoot` flow. `Launch` requires a valid owner.
- Put `PlayerHealth` on the player root or an ancestor of its `Collider2D`
  hurtboxes. Enable both components. Trigger and solid hurtboxes are supported.
- Set the projectile's `blockingLayers` to terrain layers (currently Ground),
  and tune `collisionRadius` to its visible size. Player detection uses the health
  component, without requiring a Player tag or dedicated layer.
- The projectile uses swept physics queries; it does not require a Rigidbody2D,
  collider, or trigger callback on the projectile itself. Moving shots resolve
  contacts in `Update`; zero-speed shots do not perform that sweep.
- `TL4Assembly` and its test assembly now reference `TL6Assembly` so they can
  access `PlayerHealth` directly.

For another impact detector, call `projectile.TryHitPlayer(hitCollider)` after
confirming contact. Do not also call `TakeDamage` for that same impact.

## Verification

In Unity, open **Window → General → Test Runner**, select **PlayMode**, and run
`EnemyProjectileTests`.

Added checks cover:

- Child hurtbox receives exactly the launch-time damage snapshot.
- Repeated and reentrant hit attempts produce exactly one health change.
- Null, owner, disabled, and dead targets are rejected without consuming the shot.
- A fast projectile hits a player trigger, while an earlier thin Ground wall
  prevents damage.

Existing checks cover projectile visibility, movement, terrain collision, and
lifetime expiration. For a manual check, fire at a player with 100 HP and verify
one health decrease equal to `shot.Damage`; repeat with a Ground wall between
the shooter and player and verify no decrease.

Validation status: whitespace checks and local assembly dependency-cycle checks
passed. A Unity 6000.6.0f1 batch PlayMode run was attempted, but Unity refused
because this project was already open in another instance. The tests have **not
been executed successfully** in this session; run them in the open editor.

## Current limitation

The Champion document's invulnerability-window exception cannot currently be
implemented through the supplied `PlayerHealth` interface: it exposes no
invulnerability state or damage-acceptance result. This function respects its
current dead-player guard and adds no player health policy. Coordinate any future
invulnerability interface with TL6. Melee damage remains separate work.

## Brief AI interaction log

1. User asked which enemy function remained to implement, suggesting boss logic.
2. AI inspected the Champion document and code, finding that projectiles stored
   damage but did not deliver it to player health; melee damage was also absent.
3. User requested `EnemyProjectile.TryHitPlayer` and Markdown documentation.
4. AI implemented guarded single-hit damage, connected the swept collision path,
   added assembly references and regression tests, and documented setup and limits.
5. Automated Unity execution was blocked by the already-open project; this result
   is recorded explicitly rather than reported as passing tests.
