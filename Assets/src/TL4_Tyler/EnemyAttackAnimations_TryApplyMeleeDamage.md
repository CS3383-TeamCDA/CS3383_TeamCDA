# EnemyAttackAnimations.TryApplyMeleeDamage

## What it does

`public bool TryApplyMeleeDamage()` resolves one scheduled melee strike against
the enemy's aggro target. It calls TL6's `PlayerHealth.TakeDamage(int)` and returns
`true` only when that call occurs. Otherwise it returns `false`.

This implements the melee side of scenario EN2 and damage integration task 5 in
`Champion_Document_TL4_Enemy_Combat.docx`. It follows the document's TL4 → TL6
interface boundary: enemy code decides when a hit lands; `PlayerHealth` owns HP,
clamping, and health/death events. It extends the existing attack controller's
wind-up, follow-up pause, recovery, and cooldown sequence.

## How it works

When `BeginAttack` successfully starts a melee animation, it captures the current
aggro target and `EnemyBase.MeleeDamage`. It schedules the hit at:

`attack start time + attack duration × meleeHitNormalizedTime`

The default normalized time is `0.5`, halfway through the duration reported by
`EnemyAnimationDriver.AttackDuration`. Damage is a snapshot: leveling the enemy
during that swing does not change its scheduled damage.

`Update` calls `TryApplyMeleeDamage` before advancing out of the attacking phase.
This also resolves the strike if a frame crosses both the strike time and the
end of the attack. Before the scheduled time, calls return `false` without using
up the pending strike.

At the scheduled time, the function consumes the pending attempt and checks:

- The controller is enabled, the enemy is alive, and it is still attacking.
- Melee and attacking the target are still allowed by the enemy's rules.
- The original target is still the active aggro target.
- The target is within `closeAttackRange`, measured as 2D distance between the
  enemy and target transforms.
- The target or one of its parents has an enabled, active, living `PlayerHealth`.
- The health component is outside the enemy's own transform hierarchy.
- A line from the enemy transform to the target transform is clear of colliders
  in `meleeBlockingLayers`.

A valid strike calls `TakeDamage` once. A failed check is a miss and consumes
that attempt too: stepping back into range cannot replay the same swing.
Consuming before the health call prevents reentrant health listeners from
applying the damage again. Each accepted follow-up swing schedules its own
independent strike. Ranged attacks do not schedule melee damage.

Disabling or defeating the enemy clears the pending strike through the existing
sequence cancellation path. After applying damage, `Update` also checks whether
a health listener disabled or defeated the attacker before continuing.

## Unity setup and use

1. Use an enemy with the existing `EnemyAttackAnimations`, `EnemyAnimationDriver`,
   `EnemyBase` subtype, and Animator setup. Its controller must support the
   `Melee` trigger, and its attack mode must allow melee.
2. Put `PlayerHealth` on the aggro target or an ancestor of it. Existing target
   acquisition must select that transform. Melee uses a range check rather than
   a contact callback, so it does not require a player collider to deliver damage.
3. In the attack component, tune **Close Attack Range** to the intended reach.
   The check is radial, with no separate facing cone or weapon-shaped hitbox.
4. Tune **Melee Hit Normalized Time** between 0 and 1 to align damage with the
   visible strike frame. Existing prefabs use the field's default of `0.5` until
   explicitly configured. Match the `Melee` clip name for accurate duration lookup;
   the animation driver falls back to 0.7 seconds when no matching clip is found.
5. Set **Melee Blocking Layers** to terrain that should stop strikes. The default
   is Ground (layer 3). Keep the player hurtboxes out of that mask. Because the
   query starts at the enemy's transform, keep that point clear of blocking
   terrain and ensure the line represents the intended strike path.
6. Let the existing `TryTriggerAttack` and `Update` flow schedule and deliver hits.
   No animation event is required. Do not add a second direct `TakeDamage` call
   from an animation event or collision callback for the same swing.

The public function can be called by another controller, but it still respects
the scheduled time and single-attempt guard. Changing Animator speed after a
swing starts does not reschedule its captured strike time. Timing uses scaled
`Time.time`, consistent with the existing attack sequence.

The TL4 runtime and test assemblies already reference `TL6Assembly` from the
projectile integration, so no additional assembly change is needed here.

## Verification

Open **Window → General → Test Runner**, select **PlayMode**, and run
`EnemyAttackAnimationTests`. Added regression checks cover:

- No damage before the strike; exactly one snapshot damage application afterward.
- Repeated and reentrant calls cannot damage twice.
- Moving outside reach causes a miss that cannot be replayed by returning.
- A Ground wall blocks a strike.
- Disabling the controller clears its pending hit, including after re-enabling.
- A scheduled double melee sequence delivers exactly two health changes.

Existing tests also check attack selection, follow-up cancellation, movement
locking, cooldowns, and death cancellation. Manually verify a single swing against
a stationary player, dodging during wind-up, and attacking across a Ground wall.
Match the visible impact frame to the normalized timing setting.

Validation performed: `git diff --check` passed. A batch PlayMode test run using
Unity 6000.6.0f1 was attempted, but Unity reported another instance already had
this project open. Automated tests have not successfully run in this session;
execute them in the open editor before treating runtime behavior as verified.

## Current limits

`PlayerHealth` currently exposes no invulnerability state or damage-acceptance
result. The Champion document's invulnerability-window exception still requires
a TL6 interface update. The function rejects dead players but adds no separate
invulnerability policy. This is one radial, targeted strike, not an area attack
or a physics weapon-hitbox system. Animation interruption outside the attack
controller does not automatically cancel its scheduled hit; disable/cancel the
controller when introducing such interruption behavior.

## Brief AI interaction log

1. User requested melee damage after the projectile-to-player damage integration,
   with Markdown explaining behavior, use, and the AI interaction.
2. AI inspected the current attack controller, animation driver, and attack tests.
3. AI added `TryApplyMeleeDamage`, scheduled it from accepted melee attacks, and
   guarded each strike against duplicate damage, misses, terrain, and cancellation.
4. AI added regression tests and this documentation. The Unity test attempt was
   blocked by the project already being open; that limitation is recorded here.
