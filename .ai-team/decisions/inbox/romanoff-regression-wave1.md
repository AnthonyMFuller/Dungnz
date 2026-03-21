# Decision: Regression Wave 1 — Interface Conformance + MapRenderer Tests

**Author:** Romanoff (QA)
**Date:** 2026-03-14
**Status:** Proposed
**Scope:** Dungnz.Tests

## Context

The Avalonia migration Phase 0 split `IDisplayService` into `IGameDisplay` (output-only) and `IGameInput` (input-coupled), with `IDisplayService` as a union facade. Phase 1 extracted `MapRenderer` as a static utility class. Both changes shipped with zero regression tests.

## Decision

Add 36 regression tests across two test files:

1. **`Dungnz.Tests/Architecture/InterfaceSplitTests.cs`** (8 tests) — Reflection-based verification that the interface split is structurally sound: inheritance chain, method coverage, no extra surface, FakeDisplayService conformance, and Engine/Systems dependency direction.

2. **`Dungnz.Tests/MapRendererTests.cs`** (28 tests) — Behavioral tests for `BuildPlainTextMap()` and `BuildMarkupMap()` covering grid generation, BFS correctness, connector rendering, room symbol priority chain, fog of war visibility, legend generation, and edge cases.

## Rationale

- These are pure regression tests — they verify existing behavior, not new features
- The interface split is a load-bearing architectural decision; any drift should be caught immediately
- MapRenderer is used by both Console and Avalonia display implementations; correctness is critical
- Both systems had 0% test coverage before this change

## Consequences

- Test count: 2,154 → 2,190 (+36)
- No production code changes
- Coverage improves for Dungnz.Models (MapRenderer) and architecture rule enforcement
- Future changes to the interface hierarchy or map rendering will be caught by these tests
