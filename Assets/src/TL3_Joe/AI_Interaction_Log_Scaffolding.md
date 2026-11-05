# AI Interaction Log: Power-Up Code Scaffolding

Nov 3, 2026 · Joe

I used Claude (Anthropic) between Sep 28 and Oct 8, 2026 to generate the code scaffolding for the TL3 Power-Up & Ability System in Unity/C#: the class skeletons from my class diagram, with every method logging its name and the real logic left as TODO comments. I reviewed each file, added it to the team project, and checked that it compiled and logged correctly in Unity.

## Tool and scope

| Item | Detail |
| --- | --- |
| Tool | Claude (Anthropic), in the Claude app |
| Feature | TL3 Power-Up & Ability System, CS3383 Team CDA, "Escape the 90s" |
| Inputs I gave it | My feature requirements, my champion document and class diagram, and the project RFP |
| What the AI did | Wrote scaffold classes matching my class diagram, with logging stubs and TODO comments, and answered my questions about the design choices in them |
| What stayed mine | The design (classes, Strategy and Memento roles, interface signatures), deciding what to keep or change, adding the files to the project, and checking them in Unity |

This log covers only the scaffolding phase. Filling in the real logic, switching to 2D, integration with TL2, and the remaining power-ups are logged separately.

## Interaction log

Oldest first. Prompts are paraphrased or quoted from my messages.

| # | Date | My prompt | AI output | What I did with it |
| --- | --- | --- | --- | --- |
| 1 | Sep 28 | "Show me the base code for the PowerUpEffect in C#, for now just have each method log a message instead of the actual code implementation" | Abstract `PowerUpEffect` with `Duration`, `IsPermanent`, `MaxStacks`, and virtual `Apply`, `Remove`, `CreateSnapshot` that log `GetType().Name`; stub `StatSnapshot` (with `StatKind`) and `IPowerUpTarget` so it compiles | Added it to the project; used the logs to check which version of each method runs |
| 2 | Sep 28 | "What does this do? `{ get; protected set; }`" and whether public members can be grouped under one `public:` like C++ | Explained auto-properties with a protected setter, and that C# needs an access modifier on each member | Kept the properties as written |
| 3 | Oct 8 | "Give me the basic scaffolding code for my PowerUpManager, PowerUpEffect, PowerUpItem, StatSnapshot, and each power class, IPowerUpTarget" | 10 files: `PowerUpManager`, `PowerUpEffect`, `SpeedBoostEffect`, `ShieldEffect`, `MagnetEffect`, `PowerUpItem`, `StatSnapshot`, `IPowerUpTarget`, plus `PowerUpTypes` and `PowerUpDefinition` so it would compile; every method logs, logic as TODOs | Added all 10 files to the project as the starting point |
| 4 | Oct 8 | "Is PowerUpManager a MonoBehaviour?" | Yes: it needs `Instantiate`, `Update` for timers, and `[SerializeField]`; described a plain-class alternative with a runner | Kept it a MonoBehaviour; noted the class diagram needs the stereotype |
| 5 | Oct 8 | "activeEffects in the manager is a dictionary instead of list?" | Explained: one active entry per type, quick lookup on pickup, same key as `snapshots`; warned not to remove entries while looping in `Tick` | Kept the dictionary |
| 6 | Oct 8 | "Explain PowerUpTypes and Definition and why we need it" | Explained the enum, the event payload, `ActiveEffect`, and the `PowerUpDefinition` ScriptableObject; flagged that effect values were hard-coded in two places | Kept both files; left the duplicate values as a later fix |
| 7 | Oct 8 | "Could these all live in the manager?" | Only `ActiveEffect` should be nested (private); `PowerUpType` and the event args are used by other features; Unity needs `PowerUpDefinition` in its own file | Moved `ActiveEffect` into `PowerUpManager` |
| 8 | Oct 8 | "I am not sure the TL6 is in charge of player health?" then attached the RFP | Checked the RFP: section 3.6 gives health to TL6 and lists `PlayerHealth.Heal` | No change; the scaffold's `Heal(int)` target was already correct |

## Issues and open items

These came out of the scaffolding phase, either flagged by the AI or found when I reviewed or used the code.

| Issue | Status |
| --- | --- |
| `PowerUpItem` used 3D physics (`Collider`, `OnTriggerEnter`) because the team hadn't confirmed 2D or 3D | Changed to 2D in the next phase |
| Duration and max stacks hard-coded in each effect's constructor and also in `PowerUpDefinition` | Open: the definition asset should become the single source |
| Class diagram doesn't show `PowerUpManager` as a MonoBehaviour | Open: add the stereotype |
| The health owner came from my requirements, not the RFP | Checked against the RFP: correct (TL6) |

None of these stopped the scaffold from compiling.

## How AI output was verified

- **Compared every class and signature with my class diagram** before adding it.
- **Compiled in Unity** with no Console errors.
- **Logging stubs:** each method logs its class name, so Play mode shows whether a subclass override runs or the base version does, which checks dynamic binding before any real logic exists.
- **Checked ownership claims against the RFP,** such as who owns player health.
