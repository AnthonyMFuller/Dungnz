# Decision: ReadKey() Contract on IInputReader

**Author:** Coulson  
**Date:** 2026-02-24  
**Phase:** 9 — Interactive Menus Foundation (WI-1)  
**Branch:** feat/interactive-menus

---

## Decision

`IInputReader` gains a second method:

```csharp
ConsoleKeyInfo? ReadKey();
```

Return type is **nullable** (`ConsoleKeyInfo?`).

---

## Rationale

### Why nullable?

`FakeInputReader` (and any future test double) cannot provide a real `ConsoleKeyInfo` without spinning up a terminal. Returning `null` is the idiomatic signal that "this source does not support single-key reads." Callers must null-check before acting on the result, which is the natural defensive pattern for menus that fall back to numbered input.

An alternative (`bool SupportsReadKey` + non-nullable `ReadKey()`) was rejected — it adds ceremony for no benefit given C# nullable reference types already communicate the optionality.

### Why `intercept: true`?

Arrow keys produce ANSI escape sequences. Without `intercept: true`, these appear as garbage characters in the console output. Intercepting is mandatory for any real UX.

---

## Scope

This is **interface scaffolding only**. No menu logic is implemented here. Hill and Barton consume `ReadKey()` when implementing the bounded menus in their respective work items.

---

## Impact

| File | Change |
|------|--------|
| `Engine/IInputReader.cs` | New method on interface + `ConsoleInputReader` implementation |
| `Dungnz.Tests/Helpers/FakeInputReader.cs` | Stub returning `null` |

No existing tests broken. Build: 0 errors.
