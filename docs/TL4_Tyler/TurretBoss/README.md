# Siege Core turret boss

Current sandbox implementation and tuning baseline. Earlier incremental design
notes have been consolidated here; this document describes current behavior.
For presentation details see [ART_ASSEMBLY.md](ART_ASSEMBLY.md), and for closeout
status and continuation boundaries see [CHAT_HANDOFF.md](CHAT_HANDOFF.md).

## Run and controls

Open `Assets/Scenes/TL4_Tyler/TurretBoss/TurretBossSandbox.unity` and press Play.
The scene constructs its camera and arena at runtime; “No cameras rendering”
outside Play Mode is expected. This is separate from the older `BossSandbox`.

| Control | Action |
| --- | --- |
| A / D | Move |
| Space | Jump |
| S / Down | Crouch; shrinks the collider while preserving the feet position |
| J | Debug hit for 50 damage, accepted only while the core is exposed |
| Escape | Pause/resume |
| R | Rebuild the arena and reset the encounter |

The cyan player is a local dummy using existing PlayerHealth, not the production
player controller. Boss level is configurable on the sandbox root. The prefab
starts at 600 HP (+100 per additional level) and supports both melee and ranged
attacks. Closed armor rejects damage through the EnemyBase entry point.

## Fight behavior

Phase one fires three jumpable low bolts, then a three-payload serial grenade or
missile barrage, then a crouchable continuous beam and an exposed-core opening.
Payload type alternates across cycles. Beam facing locks during charging/firing.

A nonlethal hit bringing HP to 40% or lower starts a two-second transformation.
Legs unfold and step out of the persistent docking platform. Phase two is enabled
for development, not a random unlock. Lethal damage kills directly.

Phase two chooses between Short Charge, Long Charge and stomp using seeded,
distance-based weights. Distance favors pursuit; proximity permits Short Charge
when its cooldown is ready. The last opener receives reduced selection weight.
A nearby target may prompt a retreat/reposition. Chains cap at two body attacks;
extensions are probabilistic and stomp ends the chain. Crossing behind the boss
causes a turn before the next opener, not a reversal of a committed charge.

Every Long Charge windup requests grenade/rocket support, preserving an existing
active barrage. Charge entry retries if the barrage has already finished. Stomp
also requests launcher support. Phase-two barrages contain 1–5 payloads, starting
0.3–0.4 seconds apart on independent warning/flight/blast tracks. The body waits
for payload completion before planting, an optional beam, and core exposure.
Long Charge ends on player hit, travel limit or timeout.

Grenades snapshot their landing target at warning entry. Missiles track briefly,
lock, then descend at the locked X. Soft oval ground shadows replace payload
landing boxes, tightening/darkening during descent. A muted rim keeps them
visible on the dark floor. Airborne payloads and warnings do not damage; their
landing explosions do. Each bolt, charge, stomp, beam or blast has its own
single-hit gate. Explosion/skid aftermath is visual only.

## Current timing baseline

Defaults below are in seconds unless stated otherwise. Serialized overrides can
change defaults; the controller and launcher remain the implementation authority.

| Parameter | Current value |
| --- | --- |
| Floor warning / volley exit | 0.7 / 2.0 |
| Beam warning / firing | 1.1 / 0.7 |
| Short Charge warning / travel | 0.25 / 0.3 |
| Short Charge proximity / cooldown | 3.5 units / 4.0 |
| Long Charge warning / maximum travel time | 0.7 / 2.8 |
| Long Charge speed | 4.5 × 2.197 = 9.8865 units/second |
| Stomp warning / active state | 0.8 / 1.5 |
| Assessment / turn / follow-through | 0.11 / 0.2 / 0.225 |
| Reposition / disengage / planting | 0.35 / 0.325 / 1.0 |
| Base core exposure | 0.8 |
| Mobile core exposure | (1.6 + 0.4 × chain attacks + 0.4 if ending in stomp) / 2 |
| Combo extension chance / maximum body attacks | 50% / 2 |
| Phase-two launch gap / barrage size | 0.3–0.4 / 1–5 |
| Grenade warning / flight / blast | 0.8 / 1.2 / 0.35 |
| Missile ascent / warning / target lock / descent / blast | 0.65 / 1.0 / 0.6 / 0.45 / 0.35 |

## Collision and lifecycle

The sandbox installs a grounded 1.8 × 2.1 solid kinematic boss body. Cosmetic
lift/lean does not lift collision off the floor. Movement clamps against the
player; the player cannot walk through the boss. Defeat disables body collision.
Sandbox jump/headroom checks recognize the boss as support.

Attack queries use actual non-trigger player collider shapes, including child
colliders, and synchronize crouch/transform changes immediately. Bolts, stomp
waves and charge use swept hit checks. The grounded charge effect/contact box
shares a 2.25 × 0.6875 footprint, mirrored offset (1.15, -0.64), and 0.04 ground
inset. Contact damage is enabled only during active charges.

Body state changes preserve independent payload tracks. Transformation, defeat,
disable and arena restart cancel hazards; pause freezes scaled time. Sandbox
body-center movement limits remain -8.4 to 3.3 on a flat arena. These are not
production navigation or terrain support.

## Shared boss HUD

`BossFightHUD.Bind(EnemyBase boss, string displayName, Sprite portrait,
Func<string> statusText = null)` supplies the standard bottom-right HUD:
framed portrait, boss name, diagonal magenta health segments, delayed damage
trail and status. `SetPortraitOverlay(Sprite)` supports an additional head part.
The Siege Core adapter uses dome plus cannon barrel. Encounter owners must
create/destroy their own HUD. This does not replace the shared player HUD.

## Ownership and integration

| Area | Location / responsibility |
| --- | --- |
| Gameplay, hazards, launcher, collision, HUD | `Assets/src/TL4_Tyler/TurretBoss/` |
| Boss and visual prefabs, atlases, materials | `Assets/prefabs/TL4_Tyler/TurretBoss/` |
| Sandbox and art preview | `Assets/Scenes/TL4_Tyler/TurretBoss/` |
| Regression sources | `Assets/tst/TL4_Tyler/PlayMode/TurretBoss*Tests.cs` |

The turret is not registered in Composition/FinalGame. Existing IBossEncounter
wiring targets the older BossEnemy prototype. Production integration requires a
TL4 adapter, real-player validation, production arena placement and world-seed
wiring. Local Begin/launcher seeds already support repeatable decisions.

## Validation status

The user playtested and accepted the current sandbox direction. Unity Roslyn
source/test compilation and diff checks have passed. Automated Unity runtime
results are **not confirmed green**; compilation is not a test run. Isolated
editor initialization and desktop automation previously blocked runtime checks.
See the handoff for the remaining closeout checklist.
