# Turret Boss continuation handoff

## User's current goal

Continue the Escape the 90s turret/tank boss prototype, named Siege Core.
The user approved its basic combat and avoidance after personally playtesting.
Most recent feedback: the base idea is right; animations, attack variability and
timing still need work; add a proper boss-fight health bar.
Suggested next work was a boss HUD plus an editable tuning asset, then controlled
seeded variation and production animations. These suggestions have not been
implemented. User asked to transfer this work into a new chat, not to start coding
those next features during the transfer.

## Editing boundary

Only modify TL4-owned files. User explicitly forbids editing other teams' code.
Relevant areas: Assets/src/TL4_Tyler, Assets/prefabs/TL4_Tyler,
Assets/Scenes/TL4_Tyler, Assets/tst/TL4_Tyler, docs/TL4_Tyler and TL4's own shared
contracts in Assets/src/Contracts/TL4_Tyler. Leave Composition, FinalGame,
ProjectSettings and TL1/TL2/TL3/TL5/TL6 unchanged. Shared registration/integration
changes are handoff tasks for the relevant owner. Preserve existing changes;
check git status before editing. Do not push or send team messages unasked.

## Current playable scene

Assets/Scenes/TL4_Tyler/TurretBoss/TurretBossSandbox.unity

This is separate from Assets/Scenes/TL4_Tyler/BossSandbox.unity, the older generic
ground-boss prototype. Do not confuse them or replace one with the other.
The turret sandbox creates its camera, flat arena and cyan player at runtime.
Outside Play Mode, 'No cameras rendering' is normal. Use the editor's Play button,
not Test Runner's Player tab, to play it.

Controls: A/D move, Space jump, S/Down crouch, J debug hit (50 damage only when
core exposed), Escape pause, R restart. Crouching actually changes the dummy's
collider; no TL6 movement changes were made. It uses existing TL6 PlayerHealth.

## Design and implemented behavior

User chose concept #1 Siege Core: planted purple/teal turret with red optic and
heavy cannon. Potential phase two grows legs inspired by concept #3. Current
visuals are enlarged standard turret sprites, procedural leg segments, simple
hazard geometry, and debug GUI, not final generated boss artwork.

Phase one: low horizontal floor bolts (jump), alternating grenade or vertical
missile barrage (move off landing markers), charged horizontal beam (crouch),
then exposed-core recovery. Grenades have fixed marked landing spots and visible
lobs. Missile marks track briefly yellow, lock red before descent, then explode.
Warnings do no damage; missile damage is the ground explosion, not airborne contact.

At a nonlethal hit bringing HP to 40% or below: clear hazards, block damage during
a two-second transformation, lift body, unfold four placeholder legs. Phase two
is forced on for development; seeded probability is NOT implemented. A lethal
hit kills directly rather than forcing a health gate.

Phase two: marked lunge, short advancing low leg spin (jump), stomp waves (jump)
with independent launcher barrage, wait for payload completion, plant/lower,
charged beam, exposed core; repeat. Launcher alternates missiles/grenades across
mobile loops. No floor bolts return. Body does not physically trap/push player;
swept damage checks handle lunge/spin. Spin and beam never overlap. Beam waits
for launcher completion. Pause freezes scaled timers. Death/disable/restart
cancel both tracks.

Launcher is a separate clock/attack-field instance. Body-state changes clear only
the body field, preserving in-flight payload. Both tracks have independent warning
visuals and hit flags. TurretBossLauncher.OnStageChanged is an animation hook;
a small launcher module already flashes/recoils separately from legs. Final body,
cannon and launcher artwork/Animator layering still need production work.

## Files

- Assets/src/TL4_Tyler/TurretBoss/TurretBossController.cs — phases/body coordinator
- Assets/src/TL4_Tyler/TurretBoss/TurretBossAttackField.cs — hazards, telegraphs, hit checks
- Assets/src/TL4_Tyler/TurretBoss/TurretBossLauncher.cs — independent grenade/missile track
- Assets/src/TL4_Tyler/TurretBoss/TurretBossLegRig.cs — procedural temporary legs/spin
- Assets/src/TL4_Tyler/TurretBoss/TurretBossSandbox.cs — arena, dummy controls, debug HUD
- Assets/prefabs/TL4_Tyler/TurretBoss/TurretBoss.prefab — scaled turret prototype
- Assets/tst/TL4_Tyler/PlayMode/TurretBossTests.cs — regression tests
- docs/TL4_Tyler/TurretBoss/README.md — detailed behavior/setup

EnemyBase.TakeDamage was made virtual to allow the boss's closed-armor gate to
work through EnemyBase references. Ordinary enemy behavior is unchanged.

## Integration status

Existing IBossEncounter and BossEncounterController in TL4 belong to the earlier
BossEnemy ground prototype. They are NOT wired to this turret fight. No turret
encounter registration exists in Composition/FinalGame yet. A future TL4 adapter
must expose the turret via the agreed encounter contract; coordinate foreign-file
changes rather than editing others' files. Arena bounds and several timings are
currently hardcoded for the sandbox.

## Verification and known pitfalls

User has visually playtested the mechanics and approved the general approach.
Compiler checks passed, including the TL4 PlayMode assembly with its actual
references. Automated runtime tests have NOT been confirmed green. Do not equate
compilation with passing tests. Prior CLI Unity runs failed because the working
project was open and isolated Unity Package Manager hit sandbox socket restrictions.

TL4.Contracts.PlayMode.asmdef now explicitly references TL6Assembly because tests
use PlayerHealth; an earlier compiler check accidentally supplied extra references
and missed this. It uses optional TestAssemblies, so do not also add duplicate
UnityEngine.TestRunner references. BossEnemy's field was renamed bossAttacks to
avoid Unity's inherited duplicate-field error with GroundEnemy.attacks.

Other teams had unused UnityEditor imports blocking standalone Player builds.
Our edits to their files were reverted at the user's direction. Report foreign
failures; do not fix them. Use Test Runner EditMode/PlayMode for editor runs.

## Next proposed deliverables

1. Proper boss HUD: SIEGE CORE name, wide health fill, delayed damage trail,
   40% transformation marker, optional phase label; keep debug text separate.
2. One TL4 ScriptableObject tuning asset for timings, damage, movement, thresholds
   and overlap rules rather than scattered hardcoded values.
3. Seeded selection among approved patterns with repeat limits and cooldowns.
   Preserve fair dodge windows and beam/spin exclusion.
4. Final turret/leg transformation art and separate animation tracks after tuning.

Read current source before implementing; this handoff records intent/status and
may become stale after later edits. The repository files are authoritative.

## Continuation update — boss HUD

Implemented `TurretBossHUD.cs` and sandbox binding: bottom health overlay, delayed
orange damage trail, phase/armor status, shared 40% marker, and victory state.
Controller now exposes `MobilePhaseThreshold` so HUD and combat share the value.
Compilation passed with cached Unity references (existing DeleteExeAttack obsolete
API warnings only). Unity visual/runtime verification is still pending. Editable
tuning asset, seeded variation and production art remain unimplemented.

## Continuation update — requested combat changes

Beam direction now stays locked from charge through firing. Charge uses 3.5-unit
proximity selection, 0.45-second warning, 0.3-second dash and four-second cooldown.
Spin commits in one direction at 4.5 units/second until the arena edge or four
seconds; proximity can switch spin warning to charge but never interrupt firing
or active spin. Launcher follows current body position; projectiles snapshot the
launch origin instead of using the grenade's old hardcoded offscreen position.
Regression tests updated; source/test compilation verified, runtime still pending.

## Short charge and variable phase-two barrages

Charge warning reduced to 0.25 seconds. Phase-two launcher bursts now choose 1–5
shots per barrage and sample a fresh 0.15–0.85-second gap after each explosion
before beginning the next payload. Landing warning/lock and flight durations stay
consistent. Phase one retains its three-shot introductory rhythm. Launcher stays
active during gaps, so the body still waits before planting/beam; cancel, pause,
and restart retain their existing behavior. Gap range is serialized on the
launcher. Its Initialize accepts an optional local RNG seed for repeatable tests;
world-seed integration remains future work. No global Unity random state is used.
Added seeded regression coverage for shot counts, varying gaps, completion and
cancellation. Source and test assemblies compile; runtime execution is pending.

## Continuation update — reactive mobile combat

Implemented situational mobile openings, turn/reposition tells, capped branching
combos, follow-through/disengage, and heavier-chain recovery bonuses. Spin may now
also launch a barrage. Beam is a situational ranged finisher, not mandatory each
cycle. All launcher payloads finish before beam or core exposure. Full current
behavior is documented in README's "Reactive mobile combat" section, superseding
the old fixed-loop descriptions. Added procedural lean/brace/lift/slam/recoil
motion. Begin accepts an optional RNG seed; no world-seed integration yet.
Updated regression tests; source/test compilation passes. Unity runtime and visual
verification remain pending. Only TL4-owned files were modified.

## Continuation update — modular art preparation

Approved base and modular artwork generated. User requested a bare exposed reactor
without cage; corrected transparent atlas includes it. Leg pieces corrected to
separate thigh/shin links. Added named slices/custom pivots, reusable runtime visual
rig prefab, and isolated TurretBossArtPreview scene (Space shutters, F mirror).
See ART_ASSEMBLY.md for file paths and validation limits. Combat visuals remain
unchanged. Production animation and gameplay binding are still future work.

## Continuation update — idle and walking

User approved the revised bent-knee standing pose. Added procedural idle weight
shift, upper assembly lag and a one-foot-at-a-time world-anchored crawl to the
visual rig. Art preview A/D or arrows now translates the mobile form; Space
shutters and F mirroring remain. Feet finish their current step on release.
See ART_ASSEMBLY.md. Source/tests compile and numerical reach check passes;
Unity runtime tests and visual playtest remain pending. Combat art binding and
transform/attack/death animations remain future work.

## Reversible transformation preview

T now toggles the right-hand preview boss between planted and mobile forms over
two seconds. The chassis braces/lifts, legs unfold in a staggered sequence, and
the existing base skirt retracts/scales/fades into the body. This is procedural
presentation with the current art, not dedicated hatch sprites. Pressing T again
reverses continuously from the current deployment. Movement is blocked in the
preview during transition or while planted. Completion resets ground anchors
for seamless idle/crawl handoff. Space shutters and F mirror remain available.

Regression checks cover scaled-time freeze, mid-transition reversal, endpoint
visibility and planted feet after completion. Source/test compilation and numeric
reach checks pass; Unity runtime execution and visual review remain pending.
Combat visuals and phase timing are unchanged; transformation combat binding
and attack/death presentation remain future work.

## Physical docking-base revision (current transformation)

Replaced the skirt shrink/fade with a solid persistent docking base. Legs unfold
first and plant outside its rim, then push the body upward. The base and legs
remain opaque. The front rim occludes tucked machinery instead of using alpha.
The base stays at its original world position when the boss walks away. Press T
again to walk back to the base before folding down; reversing a transition still
continues from its current pose. Right preview now starts planted so the first T
shows deployment. Existing direct SetForm(true) can still initialize a mobile
assembly without an abandoned base; animated deployment parks the physical base.

Source/test compilation and sampled joint reach checks pass. Regression coverage
now expects the base to persist, remain opaque and stay behind during walking,
with docking before retraction. Unity runtime/visual verification remains pending.
This supersedes earlier notes describing disappearing/retracting skirt art.

## Weapon and core presentation preview

B previews the cannon's 1.5-second charge and 0.7-second firing/recoil cycle on
both assemblies. A small energy glow uses the existing bare-core sprite at the
muzzle; this is presentation, not a damaging beam or final VFX. L independently
previews launcher bracing, kick and settling. Both weapons can animate together
while idle, walking or transforming without taking over the leg track. Repeated
triggers on a busy weapon are ignored. Space now slides the two core shutters
over 0.3 seconds and can reverse mid-slide. SetExposed remains an immediate
setup API; AnimateCore is the interpolated presentation API. No combat damage,
projectiles or shared integration was changed.

Updated regression coverage checks concurrent clocks, restart suppression, pause,
recoil restoration and reversible shutters. Source and test compilation pass;
Unity runtime tests and visual review are still pending.
