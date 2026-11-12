# TL4 boss prototype

## Try it

Open `Assets/Scenes/TL4_Tyler/BossSandbox.unity`, then enter Play Mode. The harness
builds a flat arena, simple cyan player and camera and starts a level-one boss.

- A/D: move; Space: jump.
- J: deal 50 debug damage from anywhere. This tests phases/defeat, not player weapon balance.
- Escape: pause/resume; R: reset player and begin a fresh encounter.
- Read boss HP/phase, player HP and encounter outcome in the upper left.

The sandbox provides its own IGameState implementation. Do not add BossSandbox to
FinalGame or put it alongside Chris's manager. It uses existing TL6 PlayerHealth
without modifying TL6, so existing player damage paths can be exercised.

## Boss behavior

PrototypeBoss reuses our ground-enemy art, animator, melee and projectile systems.
It is an initial combat prototype, not final boss art or an arena design.

| Stat | Level 1 | Increase per additional level |
| --- | --- | --- |
| Health | 600 | 100 |
| Melee damage | 20 | 2 |
| Projectile damage | 16 | 2 |
| Coin reward | 50 | 10 |
| Score | 500 | Fixed |

At half health or below, movement speed increases by 40%, melee/ranged cooldowns
shorten and follow-up attacks become more likely. The request seed picks between
two opening follow-up profiles. It does not yet make combat fully deterministic:
existing TL4 attacks and awareness still use UnityEngine.Random.

The controller removes the boss when defeated and reports its reward values once.
It does not credit a wallet/HUD, spawn an arena, lock gates, stop ordinary enemy
spawners, or teleport the player. Random power-up drops are disabled on this boss
prefab. It is created outside EnemySpawner, so the normal spawner's legacy reward
events do not fire for it. A consumer must handle boss rewards separately.

## Caleb's entry point

Shared types are in `Assets/src/Contracts/TL4_Tyler/IBossEncounter.cs`:

```csharp
bool TryStart(BossEncounterRequest request);
bool IsActive { get; }
void Cancel();
event Action<BossEncounterResult> OnEncounterEnded;
```

Request: EncounterId (ulong), Position (WorldPosition2D), Level (int, >=1), Seed (int).
Position is the boss spawn origin. The caller must supply clear, loaded terrain.
Caleb should pass a stable gate/encounter ID and the resolved enemy level, not an
unconverted zero-based difficulty tier. Duplicate active starts and defeated IDs
within one run are rejected. A cancelled/despawned encounter can be retried.

Results: Defeated, Cancelled, Despawned, with encounter ID and score/coin values.
Only Defeated carries rewards. Unlock progression only for Defeated, not for any
end event. Subscribe/unsubscribe through the normal dependency lifecycle.

Pause retains the encounter and blocks new boss attacks; Chris's scaled-time
pause freezes physics and attack timers. Menu/game over, a new run, or disabling
the controller cancels without rewards. New runs clear completed encounter IDs.
This controller is scene-owned and must not survive scene reloads.

## Integration handoff (not applied outside TL4)

1. Place `Assets/prefabs/TL4_Tyler/BossEncounter.prefab` in the gameplay scene.
2. Composition owner registers its BossEncounterController as IBossEncounter
   before scene injection. It consumes IGameState and must receive that dependency.
3. Assign the player Transform or let it resolve the active Player-tagged object
   at encounter start. Keep the PrototypeBoss prefab reference (or swap in final TL4 art).
4. Consumers request IDependent `IBossEncounter` and call TryStart with the gate data.
5. TL1/TL5 consume boss outcome/rewards; TL2 handles gate/arena/chunk lifetime and
   calls Cancel before unloading the arena. No other team's scripts/scene were edited.

## Verification

BossEncounterTests exercises dependency activation, invalid requests, duplicate
starts, level scaling, half-health phase, pause, exactly-once rewards, completed-ID
rejection, fresh-run reset, cancellation and unexpected despawn without rewards.
Affected source and tests compile using Unity's cached compiler references.
Unity runtime/Test Runner execution remains pending; compilation is not a passing
runtime test result. Test in the sandbox and run TL4.Contracts.PlayMode in Unity.
