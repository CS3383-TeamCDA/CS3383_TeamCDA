# MenuState.Enter()

**Feature:** TL1 — UI/HUD, Game State & Menu System
**Author:** Chris Bailey
**File:** `Assets/src/TL1_Chris/GameState/MenuState.cs`
**Namespace:** `EscapeThe90s.GameStates`

## What the code does

`MenuState.Enter()` runs once each time the game enters the main menu. It
overrides the virtual `GameState.Enter()`.

```csharp
public override void Enter()
{
    previousTimeScale = Time.timeScale;
    previousCursorLockMode = Cursor.lockState;
    previousCursorVisible = Cursor.visible;

    Time.timeScale = 0f;
    Cursor.lockState = CursorLockMode.None;
    Cursor.visible = true;
}
```

| Line | Effect | Reason |
| ---- | ------ | ------ |
| `previous... = ...;` (3 lines) | Saves the time scale and cursor settings that were active before the menu opened. | `Exit()` uses these to undo exactly what `Enter()` changed (see [MenuState_Exit.md](MenuState_Exit.md)). |
| `Time.timeScale = 0f;` | Freezes all scaled-time gameplay (physics, `Time.deltaTime` movement, animations). | The menu is drawn over the gameplay scene. Without this, enemies and the player would keep moving behind the menu. |
| `Cursor.lockState = CursorLockMode.None;` | Releases any cursor lock. | The player has to be able to click the menu buttons. |
| `Cursor.visible = true;` | Shows the system cursor. | Same as above. |

### What it deliberately does NOT do

- **It does not show or hide UI panels.** `HUDController` handles that. It
  watches `GameStateManager.CurrentState` and calls `MainMenuPanel.Show()` and
  `HUDPanel.Hide()` when the state becomes `MenuState`. This follows the champion
  doc's GRASP rules: *Low Coupling* (UI only listens) and *Pure Fabrication*
  (HUDController keeps display logic separate from state logic).
- **It does not load a scene.** The menu is a panel in the same scene, not a
  separate scene.
- **It does not reset score, health, or currency.** Restarting the run is the job
  of `RequestRestart()` and `PlayingState`.

## How it fits the State pattern (dynamic binding)

`GameStateManager` holds a `StateMachine<GameState>`. When a transition is
requested, the machine calls `CurrentState.Enter()` through a `GameState`
reference. At runtime the override is chosen by the object's concrete type:

| Concrete state | `Enter()` behavior |
| -------------- | ------------------ |
| `MenuState` | `timeScale = 0`, cursor unlocked and visible |
| `PlayingState` | `timeScale = 1` |
| `PauseState` | `timeScale = 0` |

If `Enter()` were not virtual, every state would run the same base (empty)
method. Returning to the menu would then leave gameplay running behind it.

## Use

`Enter()` is never called directly. It runs on these transitions:

| Trigger | Legal from | Result |
| ------- | ---------- | ------ |
| `GameStateManager.Start()` (game launch, no current state) | — | `null → MenuState` |
| `GameStateManager.Instance.RequestReturnToMenu()` | `PlayingState`, `PauseState`, `GameOverState` | `→ MenuState` |

Sequence, matching Scenario GF1 in the champion doc:

1. A caller asks `GameStateManager` for a transition.
2. `GameStateManager` checks that the transition is legal.
3. `StateMachine.ChangeState()` calls `Exit()` on the old state and sets `CurrentState`
   to the new `MenuState`.
4. **`MenuState.Enter()` runs**: gameplay freezes and the cursor is released.
5. `OnStateChanged` is raised with the new `MenuState`.
6. `HUDController` sees the change, hides the HUD, and shows `MainMenuPanel`.

When the player presses Start, `MainMenuPanel.StartGame()` calls
`RequestStart()`. That moves to `PlayingState`, whose `Enter()` restores
`Time.timeScale = 1f`.

### Note for other features

While the menu is open, `Time.timeScale` is `0`, so `Time.deltaTime` is `0`.
Any menu UI animation or tween must use **unscaled time**
(`Time.unscaledDeltaTime`, or Animator update mode *Unscaled Time*). Otherwise it
will not move.

### Tests

Implemented in [MenuStateTests.cs](../../Assets/tst/TL1_Chris/Tests/MenuStateTests.cs) (assembly `Tests_TL1`). Example:

```csharp
[Test]
public void MenuStateEnter_FreezesTimeAndShowsCursor()
{
    Time.timeScale = 1f;
    MenuState menuState = new MenuState();

    menuState.Enter();

    Assert.AreEqual(0f, Time.timeScale);
    Assert.AreEqual(CursorLockMode.None, Cursor.lockState);
}
```

## Changes made to the file

- Implemented `Enter()` as described above.
- Replaced the unused `using System.Diagnostics;` and `using Unity.VectorGraphics;`
  with `using UnityEngine;`, which `Time` and `Cursor` need.
  `System.Diagnostics` would also make `Debug` ambiguous alongside `UnityEngine`.
- Later revision: added the three `previous...` fields so that `Exit()` can
  restore the settings. `Update()` and `Exit()` are documented in
  [MenuState_Update.md](MenuState_Update.md) and [MenuState_Exit.md](MenuState_Exit.md).

## AI interaction log

**Tool:** Claude Code (Claude Opus 5.5), in the VS Code extension
**Date:** 2026-11-03

| # | Who | Summary |
| - | --- | ------- |
| 1 | Chris | Asked the AI to create `MenuState.Enter()` from the champion doc (`TL1_Chris_Bailey_Champion_UI_HUD_Game_State_Menu_System.docx`), the class diagram/questions doc, `AI_prompt_Champion.docx`, and the existing code, plus a markdown file covering what the code does, its use, and an AI log. |
| 2 | AI | Read the champion doc (State pattern, Scenario GF1, GRASP Low Coupling and Pure Fabrication), the class diagram answers (dynamic binding through `Enter()`), `CODIING_STANDARDS.md`, and the TL1 code: `GameState`, `GameStateManager`, `StateMachine`, the other concrete states, `HUDController`, and the panels. |
| 3 | AI | Design decision: `HUDController` already shows `MainMenuPanel` when the state becomes `MenuState`. Putting UI calls in `Enter()` would duplicate that and break Low Coupling, so `Enter()` only handles state-owned concerns: game time and the cursor. This matches the existing `PlayingState`/`PauseState` pattern, which only sets `Time.timeScale`. |
| 4 | AI | Implemented `Enter()` following the team coding standards (explicit types, braces on their own line, comments only for non-obvious intent) and swapped out the unused `using` directives. |
| 5 | AI | Wrote this document. |

**Human review:** Chris should verify the change in Unity: there should be no
compile errors, and the menu should appear on launch with gameplay frozen. Also
check that returning to the menu from Pause or Game Over works.
