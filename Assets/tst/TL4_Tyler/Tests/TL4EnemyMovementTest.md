# Enemy movement and animation test

Open TL4EnemyMovementTest.unity and press Play. Use a 16:9 Game view for the intended layout. This is a standalone scene and does not need the main menu, level generator, player or GameStateManager.

Three lanes show the ground, turret and flying enemy prefabs. Auto-cycle visits all 17 character animation clips. Walk/Run/Fly move side to side; other clips stay in place. Turret aiming poses fire at 0/45/90/135/180 degrees, and shooting previews launch the real projectile prefab.

Controls:
- Auto-cycle: toggle automatic selection; manual Previous/Next still work and clips keep playing.
- Level −/+: change difficulty from 1 to 50 for all three enemies. Health percentage is preserved.
- Reset all enemies: restore full health and restart each animation list.
- Per enemy: Previous, Next, Fire, Take 10 damage, Respawn.

Each panel displays current animation, enemy and movement type, attack mode, level, current/max health, coin reward, melee damage, ranged damage and alive/defeated status. Unsupported attack damage is zero. Hit previews reduce actual health; Death previews set health to zero. Advancing beyond death respawns the preview. Respawn on the Death selection deliberately previews death again; use Next to return to a live animation.

EnemyAnimationShowcase lives under Assets/src/TL4_Tyler so the scene is usable without UNITY_INCLUDE_TESTS. It samples the actual AnimationClip assets with the prefab Animator and animation driver disabled to prevent gameplay transitions from interrupting a selected preview. This is scripted preview motion, not movement AI. Projectile collisions, coin payouts and combat targets are not simulated.

Play Mode regression test: ShowcaseSceneTests checks the three prefabs, level scaling, and sprite references while stepping through every animation.
