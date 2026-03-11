# Dev Process — Dungnz

## Definition of "Done" for Display Bugs

A display bug fix is **not done** until all three of the following are satisfied:

1. **CI is green** — all tests pass on the PR branch.
2. **Regression test exists** — a `_DoesNotThrow` or rendering-assertion test covers the fixed rendering path. The test must live in `Dungnz.Tests/` and reference constants from `LayoutConstants.cs` (not magic numbers).
3. **"Verified in terminal" attestation** — the PR body must include a `## Verified in terminal` section describing what was run and what was observed (e.g. floor, scenario, exact behaviour seen).

PRs that fix display bugs and omit any of these three items must not be merged.

---

## PR Review Gate

**Romanoff must approve all PRs** before merge. No exceptions.

---

## Display/ PR Gate

Any PR that touches files under `Dungnz.Display/` must include at least one new or updated test in `Dungnz.Tests/` that covers the changed rendering path. A `_DoesNotThrow` test is the minimum bar. PRs that touch `Display/` without a corresponding test must not be merged.

---

## Fury Loop-In Gate

Any PR that changes **panel content or layout bounds** must loop in Fury before merge, unless the change provably has no content impact (e.g. pure refactor with no text changes). If in doubt, loop Fury in.

---

## Panel Height Constants

All panel height values are defined in `Dungnz.Display/Spectre/LayoutConstants.cs`. Renderers and tests must reference these constants — not magic numbers. See `LayoutConstants.cs` for the baseline terminal height and per-panel height bounds.
