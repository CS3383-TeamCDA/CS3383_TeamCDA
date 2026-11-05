# MenuState.Exit()

**Feature:** TL1 — UI/HUD, Game State & Menu System
**Author:** Chris Bailey
**File:** `Assets/src/TL1_Chris/GameState/MenuState.cs`
**Namespace:** `EscapeThe90s.GameStates`

## What the code does

`MenuState.Exit()` runs once when the game leaves the main menu. It overrides the
virtual `GameState.Exit()`.

```csharp
public override void Exit()
{
    Time.timeScale = previousTimeScale;
    Cursor.lockState = previousCursorLockMode;
    Cursor.visible = previousCursorVisible;
}
```

`MenuState.Enter()` saves three values before it changes them:

| Field | Saved in `Enter()` from | Restored in `Exit()` to |
| ----- | ----------------------- | ----------------------- |
| `previousTimeScale` | `Time.timeScale` | `Time.timeScale` |
| `previousCursorLockMode` | `Cursor.lockState` | `Cursor.lockState` |
| `previousCursorVisible` | `Cursor.visible` | `Cursor.visible` |

`Exit()` puts those values back, so the menu leaves global Unity settings as it
found them. Each state cleans up after itself instead of relying on the next
state to fix whatever it left behind.

The fields default to `1f`, `CursorLockMode.None`, and `true`. If `Exit()` were
ever called without `Enter()`, it would still restore normal, safe values.

### Why restore, rather than hard-code `Time.timeScale = 1f`?

`StateMachine.ChangeState()` always runs the old state's `Exit()` **before** the
new state's `Enter()`. For example:

| Transition | `Exit()` restores | Then the next `Enter()` sets |
| ---------- | ----------------- | ---------------------------- |
| Launch → Menu → Playing | `timeScale = 1` (value at launch) | `PlayingState`: `timeScale = 1` |
| Pause → Menu → Playing | `timeScale = 0` (value saved from Pause) | `PlayingState`: `timeScale = 1` |

In both cases the next state's `Enter()` has the final say. `MenuState` doesn't
need to know which state comes next. That knowledge stays in `GameStateManager`
and in each state's own `Enter()`, which keeps the states loosely coupled.

### What it deliberately does NOT do

- **It does not hide the main menu panel.** `HUDController` does that when
  `OnStateChanged` reports the new state.
- **It does not reset score, health, or currency.** That belongs to the restart
  flow (`RequestRestart()` / `PlayingState`).

## How it fits the State pattern (dynamic binding)

`StateMachine.ChangeState()` calls `CurrentState?.Exit()` through a `GameState`
reference. When the current object is a `MenuState`, its override runs. If
`Exit()` were not virtual, the base (empty) method would run instead, and the
menu's frozen time and cursor settings could carry into the next state.

## Use

`Exit()` is never called directly. It runs during any transition out of the menu:

| Trigger | Result |
| ------- | ------ |
| Start button → `MainMenuPanel.StartGame()` → `GameStateManager.RequestStart()` | `MenuState.Exit()`, then `PlayingState.Enter()` |
| `RequestQuit()` | The application quits. `Exit()` is **not** called, because no state change happens. |

Order inside `StateMachine.ChangeState()`:

1. **`MenuState.Exit()`**: restores the time scale and cursor.
2. `PreviousState` is set to the `MenuState`, and `CurrentState` to the new state.
3. The new state's `Enter()` runs.
4. `OnStateChanged` fires, and the UI updates.

### Tests

Implemented in [MenuStateTests.cs](../../Assets/tst/TL1_Chris/Tests/MenuStateTests.cs) (assembly `Tests_TL1`). Example:

```csharp
[Test]
public void MenuStateExit_RestoresSettingsFromBeforeEnter()
{
    Time.timeScale = 1f;
    MenuState menuState = new MenuState();

    menuState.Enter();
    menuState.Exit();

    Assert.AreEqual(1f, Time.timeScale);
}
```

## AI interaction log

**Tool:** Claude Code (Claude Opus 5.5), in the VS Code extension
**Date:** 2026-11-03

| # | Who | Summary |
| - | --- | ------- |
| 1 | Chris | Asked the AI to do the same as `Enter()` for `Update()` and `Exit()`: implement each one and write a markdown file per function. |
| 2 | AI | Re-read `StateMachine.ChangeState()` to confirm the call order: old `Exit()`, then new `Enter()`, then `OnStateChanged`. Also checked every transition into and out of `MenuState` in `GameStateManager`. |
| 3 | AI | Design decision: hard-coding `timeScale = 1f` would assume `PlayingState` always comes next. Instead, `Enter()` now saves the original values and `Exit()` restores them, which makes `MenuState` self-contained. This required adding three private fields and three lines to `Enter()`; the Enter doc was updated to match. |
| 4 | AI | Implemented the code following `CODIING_STANDARDS.md` and wrote this document. |

**Human review:** Chris should check in Play mode that pressing Start resumes
gameplay with `Time.timeScale = 1`. Also check that going Pause → Menu → Start
does not leave the game frozen.
