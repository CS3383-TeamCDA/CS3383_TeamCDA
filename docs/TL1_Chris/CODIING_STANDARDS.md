# Team C# Coding Standards

This document defines the required C# style for CS3383 TeamCDA. Use it when
writing, reviewing, or modifying code in this repository.

## Quick Rules

These rules are the authoritative summary for code review and AI-assisted work:

1. Use descriptive names that clearly communicate purpose or behavior.
2. Use PascalCase for classes and methods.
3. Name interfaces with an `I` prefix and PascalCase.
4. Use camelCase for variables and attributes/fields.
5. Do not use `var`; declare the variable's explicit type.
6. Always use braces for methods, conditionals, and other scopes. Ternary statements are the exception.
7. Put each opening brace on its own line.
8. Add comments only when they explain non-obvious intent or reasoning.
9. Use regions to organize substantial class sections when they improve navigation.
10. Do not add unrelated formatting or refactoring to a change.

## Naming

Names must be descriptive and consistent with the item's role.

| Code element       | Convention       | Example             |
| ------------------ | ---------------- | ------------------- |
| Class              | PascalCase       | `PlayerController`  |
| Interface          | `I` + PascalCase | `IDamageable`       |
| Method             | PascalCase       | `CalculateDamage()` |
| Variable           | camelCase        | `currentHealth`     |
| Attribute or field | camelCase        | `movementSpeed`     |

Avoid abbreviations unless they are universally understood in the project.
Names should describe what a value represents or what an action does, not how it
is currently implemented.

## Interfaces

Interfaces define capabilities or contracts. Name each interface with an
uppercase `I` followed by a descriptive PascalCase name. Prefer names that
describe what an implementing type can do or provide, such as `IDamageable` or
`IMovable`, rather than how it works internally.

Interface methods and properties use PascalCase. Keep the interface focused on
its contract; implementation details belong in the implementing class.

```csharp
public interface IDamageable
{
    void TakeDamage(int damageAmount);
}

public class Player : IDamageable
{
    private int health;

    public void TakeDamage(int damageAmount)
    {
        health -= damageAmount;
    }
}
```

## Variables and Types

Declare variables with their explicit type. Do not use `var`.

```csharp
int score = 0;
string playerName = "Player";
```

## Braces and Scope

Every method, conditional, loop, and other scope must use opening and closing
braces. Opening braces must be on their own line.

```csharp
if (currentHealth <= 0)
{
    HandlePlayerDefeat();
}
else if (currentHealth < maximumHealth)
{
    RegenerateHealth();
}
else
{
    KeepCurrentHealth();
}
```

Do not omit braces for single-line statements.

## Classes and Regions

Keep related code together and organize larger classes with regions. Use regions
only when they make a file easier to navigate; do not create empty or overly
small regions.

Recommended order:

1. Attributes and fields
2. Constructors
3. Public methods
4. Private methods

```csharp
public class Player
{
    #region Attributes

    private int health;
    private int speed;

    #endregion

    #region Constructors

    public Player(int startingHealth, int startingSpeed)
    {
        health = startingHealth;
        speed = startingSpeed;
    }

    #endregion

    #region Player Movement

    public void Move()
    {
        UpdatePosition();
    }

    #endregion
}
```

Keep fields private unless the design explicitly requires another access level.
Expose state through appropriate properties or methods when needed.

## Comments

Code should be understandable through clear names and simple structure. Do not
add comments that merely restate the code. Add a concise comment only when it
explains non-obvious intent, an important constraint, or a reason that is not
visible from the implementation.

## AI Review Instructions

When reviewing a change, evaluate issues in this order:

1. Functional correctness: bugs, broken behavior, and incorrect assumptions.
2. Safety and reliability: crashes, invalid state, data loss, and security risks.
3. Testing: missing or inadequate tests for changed behavior.
4. Maintainability: unclear design, excessive duplication, and difficult-to-follow code.
5. Style: violations of the naming, type, scope, comment, and region rules in this document.

Report only actionable findings supported by the code or tests. Do not report
style preferences that are not defined here. For each finding, include the
affected file and line, explain the impact, and suggest a focused fix. Clearly
separate confirmed problems from questions or assumptions. If no issues are
found, state that explicitly and mention any remaining test gaps or uncertainty.

## Review Checklist

- Names are descriptive and use the required casing.
- Interfaces use an `I` prefix, and their methods and properties use PascalCase.
- No `var` declarations were added.
- Every scope has braces, with opening braces on their own line.
- Comments explain intent rather than narrating code.
- Regions, if used, improve navigation and contain related code.
- The change does not include unrelated refactoring or formatting.
- The project builds and relevant tests pass.
