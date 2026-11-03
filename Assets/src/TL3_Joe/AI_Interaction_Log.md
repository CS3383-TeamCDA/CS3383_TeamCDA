# AI Interaction Log: SpeedBoostEffect.Apply()

**Feature:** TL3 Power-Up & Ability System, *Escape the 90s* (CS3383)
**AI tool:** Claude
**Date:** November 3, 2026

## Summary

I used Claude to draft `SpeedBoostEffect.Apply()` from my Champion document, class diagram, and an empty method stub. After working through two compile errors with it, I kept only the input validation (the null check and the stack count range check) and wrote the rest of the method myself.

## Log

| # | Who | What happened |
|---|---|---|
| 1 | Me | Asked Claude to write `Apply()` for `SpeedBoostEffect` from my Champion doc, class diagram, and empty stub, plus a markdown documentation file. |
| 2 | AI | Read the Champion doc and pulled out the requirements: multiplier `1.0 + 0.5 x stacks`, max 3 stacks, the [1.0, 2.5] range guarantee, and the rule that effects only reach TL6 through `IPowerUpTarget`. Drafted a full `Apply()` with a null check, a range check, a `base.Apply()` call, and a clamped multiplier calculation. |
| 3 | AI | Pointed out an inconsistency in my doc: the 3.3 decision tree says `Apply()` is not called at the stack cap, but the Max stack boundary test in 3.6 expects four multiplier values sent. |
| 4 | Me | Pasted the method into my project and got a compile error: `ArgumentNullException` could not be found. |
| 5 | AI | Explained that my file was missing `using System;` and changed the exceptions to their full names (`System.ArgumentNullException`, `System.ArgumentOutOfRangeException`). |
| 6 | Me | Got a second compile error: `BaseMultiplier` does not exist in the current context. |
| 7 | AI | Explained that the constants were declared at class level and had not been pasted with the method. Moved them inside `Apply()` as local constants. |
| 8 | Me | Decided to keep only the null check and the stack count range check from the AI's code, and wrote the rest of `Apply()` myself. |

## What I kept from the AI

```csharp
if (target == null)
    throw new System.ArgumentNullException(nameof(target));

if (stackCount < 1)
    throw new System.ArgumentOutOfRangeException(
        nameof(stackCount), stackCount, "Stack count must be at least 1.");
```

These guard against a missing adapter and an invalid stack count before anything is sent to TL6, which matches how my contract handles invalid input elsewhere (throwing on bad spawn arguments).

## What I did not use

- The multiplier calculation, clamp, and `base.Apply()` call.
- The class-level and local constants (`BaseMultiplier`, `BonusPerStack`, `MaxMultiplier`).
- The AI's markdown documentation file, which described the full drafted version of the method.

## Reflection

The AI was useful for reading the requirements out of my document and catching the decision tree vs. test inconsistency. Its code did not drop cleanly into my project because it assumed `using` lines and class-level declarations my file did not have, so I had to understand each error before fixing it. I verified the parts I kept and am responsible for the final code.
