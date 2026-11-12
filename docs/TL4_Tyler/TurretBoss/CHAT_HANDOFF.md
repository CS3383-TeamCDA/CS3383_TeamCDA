# Siege Core closeout and continuation

## Current checkpoint

The user accepted the current sandbox fight and requested documentation
consolidation for closeout. Gameplay is frozen for this documentation pass.

- [README.md](README.md): authoritative setup, controls, combat rules, tuning,
  collision, shared boss HUD and integration status.
- [ART_ASSEMBLY.md](ART_ASSEMBLY.md): current assets, rig/VFX architecture and art
  preview controls.

These references replace the appended historical iterations. Git history retains
the earlier notes. Do not use old spin/lunge names: the attacks are **Long Charge**
and **Short Charge**. Latest changes include rapid overlapping phase-two payloads,
mandatory Long Charge launcher support, landing shadows, solid boss/player
collision, reduced waits/core openings, and the dome-plus-cannon boss portrait.

## Boundaries

Work only in TL4-owned source, prefabs, scenes, tests, docs and TL4 contracts.
Leave other teams, Composition, FinalGame and ProjectSettings unchanged. Preserve
existing work. Do not commit, push or send team messages without instruction.
The older BossSandbox/BossEnemy encounter is distinct from this turret sandbox.

## Verification and outstanding work

- Unity Roslyn compilation with cached project references passes for the current
  source and TL4 test assembly; diff whitespace checks pass.
- The user has manually playtested the fight. Automated PlayMode results remain
  unconfirmed: earlier isolated Unity startup failed AssetDatabase/UDS setup,
  and desktop controls could not operate the editor reliably.
- Before calling this production-ready, run the TurretBoss test classes in Unity
  PlayMode. Review old timing/selection assertions against current tuning;
  deterministic old sequences may need updated expectations. Do not report
  compilation as passing runtime tests.
- Smoke-test both phases, hit/crouch/jump avoidance, boss blocking, charge stop,
  overlapping payloads/shadows, core windows, portrait, pause/restart and death.
- Production registration, real-player integration, arena/navigation and world
  seed wiring remain outstanding. Existing IBossEncounter registration targets
  the older boss. Coordinate cross-owner changes; do not silently edit them.

Closeout here means a documented sandbox checkpoint, not a release or completed
FinalGame integration. No gameplay changes are needed solely for this closeout.
