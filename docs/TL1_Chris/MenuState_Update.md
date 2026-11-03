# MenuState.Update()

**Feature:** TL1 — UI/HUD, Game State & Menu System
**Author:** Chris Bailey
**File:** `Assets/src/TL1_Chris/GameState/MenuState.cs`
**Namespace:** `EscapeThe90s.GameStates`

## What the code does

`MenuState.Update()` runs once per frame while the main menu is the active
state. It overrides the virtual `GameState.Update()`.

```csharp
public override void Update()
{
    if (Cursor.lockState != CursorLockMode.None || !Cursor.visible)
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }
}
```

`Update()` is a guard. If any other system locks or hides the cursor while the
menu is open, `Update()` unlocks it and shows it again on the next frame. That
could be the camera, a player controller, or code that runs when the window
regains focus. Without the guard, the player could be stuck on a menu they can't
click.

The `if` check means the cursor is only written to when something has actually
changed it. On a normal frame, nothing is assigned.

### What it deliberately does NOT do

- **It does not read keyboard or gamepad input.** TL2's `PlayerInputHandler`
  listens to `OnStateChanged` and switches the Input System to the `UI` action
  map. Menu navigation then goes through Unity's `EventSystem` and the buttons on
  `MainMenuPanel`. Reading input here as well would duplicate that and couple TL1
  to TL2's input setup.
- **It does not change state.** Starting the game happens when the Start button
  calls `MainMenuPanel.StartGame()`, which calls `GameStateManager.RequestStart()`.
  This keeps the transition rules inside `GameStateManager` (GRASP Controller),
  as the champion doc requires.
- **It does not depend on `Time.deltaTime`.** `Time.timeScale` is `0` in the menu,
  so `deltaTime` is `0`. The guard doesn't need time at all.

## How it fits the State pattern (dynamic binding)

`GameStateManager.Update()` calls `machine.Update()`, which calls
`CurrentState.Update()` through a `GameState` reference. Only the active state's
override runs:

| Concrete state | `Update()` behavior |
| -------------- | ------------------- |
| `MenuState` | Keeps the cursor unlocked and visible |
| `PlayingState` | No per-frame work yet |
| `PauseState` | No per-frame work yet |

When the menu closes, `MenuState.Update()` stops running. It therefore can't
interfere with how gameplay handles the cursor.

## Use

`Update()` is never called directly. It runs every frame from the moment
`MenuState.Enter()` finishes until `MenuState.Exit()` is called:

```
Unity frame → GameStateManager.Update() → StateMachine.Update() → MenuState.Update()
```

### Tests

Implemented in [MenuStateTests.cs](../../Assets/tst/TL1_Chris/Tests/MenuStateTests.cs) (assembly `Tests_TL1`). Example:

```csharp
[Test]
public void MenuStateUpdate_UnlocksCursorIfAnotherSystemLocksIt()
{
    MenuState menuState = new MenuState();
    menuState.Enter();
    Cursor.lockState = CursorLockMode.Locked;

    menuState.Update();

    Assert.AreEqual(CursorLockMode.None, Cursor.lockState);
}
```

## AI interaction log

**Tool:** Claude Code (Claude Opus 5.5), in the VS Code extension
**Date:** 2026-11-03

| # | Who | Summary |
| - | --- | ------- |
| 1 | Chris | After `Enter()` was done, asked the AI to do the same for `Update()` and `Exit()`: implement each one and write a markdown file per function. |
| 2 | AI | Searched the project for input and state-change listeners. Found that TL2's `PlayerInputHandler` / `GameStateEventPublisher` already switch to the `UI` action map on `OnStateChanged`, and that the project uses the new Input System. |
| 3 | AI | Design decision: polling input in `Update()` would duplicate TL2's work and bypass `GameStateManager`. The one menu-specific concern that can break during a frame is the cursor, so `Update()` became a cheap guard that only writes when the cursor has been changed. |
| 4 | AI | Implemented the code following `CODIING_STANDARDS.md` and wrote this document. |

**Human review:** Chris should check in Play mode that the cursor stays visible
and the buttons stay clickable on the menu, including after alt-tabbing away
and back.
