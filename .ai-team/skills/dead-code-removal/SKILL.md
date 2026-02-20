# Dead Code Removal Strategy

**Confidence:** medium  
**Source:** earned  
**Domain:** refactoring, code-quality, technical-debt  
**Date:** 2026-02-20  

## Problem

Codebases accumulate dead code (unused classes, redundant implementations, obsolete utilities). Common causes:

- **Premature abstraction:** Manager/service layers that are never used
- **Refactoring residue:** Old implementations left behind after consolidation
- **Feature removal:** Code deleted but dependencies remain
- **Architecture churn:** Early designs abandoned for simpler patterns

Dead code increases cognitive load, slows navigation, and creates maintenance burden. Teams hesitate to delete because "what if we need it later?"

## Pattern: Safe Dead Code Removal

### 1. Verify Zero Callers (Grep Analysis)

**Critical:** Never delete based on assumptions. Always grep the entire codebase.

```bash
# Search all file types (code, tests, configs, docs)
grep -r "InventoryManager" .

# Include hidden/ignored files if necessary
grep -r "ClassName" . --exclude-dir=node_modules --exclude-dir=.git
```

**Check:**
- Production code (*.cs, *.js, *.py, etc.)
- Test files (*Tests.cs, *_test.go, test_*.py)
- Configuration (appsettings.json, package.json, *.csproj)
- Documentation (README.md, *.md)
- Build scripts (Makefile, build.sh, CI configs)

**Red flags (do NOT delete):**
- Reflection usage (`Assembly.Load`, `Type.GetType`)
- Dependency injection container registration (even if not directly referenced)
- Public API classes in libraries (external consumers may exist)

### 2. Identify Redundant Logic

If class has callers but logic is duplicated elsewhere:

```csharp
// InventoryManager.TakeItem() (dead file)
public bool TakeItem(Player player, Room room, string itemName) {
    var item = room.Items.FirstOrDefault(i => i.Name.Contains(itemName));
    room.Items.Remove(item);
    player.Inventory.Add(item);
}

// GameLoop.HandleTake() (live code)
private void HandleTake(string itemName) {
    var item = _currentRoom.Items.FirstOrDefault(i => i.Name.Contains(itemName));
    _currentRoom.Items.Remove(item);
    _player.Inventory.Add(item);
}
```

**Decision:** If GameLoop already has inline implementation, InventoryManager is redundant delegation. Delete entire manager class.

**When to keep manager:**
- Complex business logic (validation, side effects, multi-step workflows)
- Reused across multiple callsites
- Testability benefit (mock manager instead of inline logic)

### 3. Consolidate Missing Logic (If Any)

**Before deleting, audit for unique functionality:**

```diff
# InventoryManager has logging, GameLoop doesn't
- _logger.LogInfo($"Player picked up {item.Name}");
  
# Solution: Add logging to GameLoop if needed
+ _display.ShowMessage($"You take the {item.Name}.");
```

**Checklist:**
- Error handling (try/catch blocks, validation)
- Logging/telemetry
- Side effects (events, notifications)
- Edge case handling (null checks, boundary conditions)

### 4. Delete and Verify Clean Build

```bash
# Delete the file
rm Systems/InventoryManager.cs

# Immediate build verification
dotnet build --nologo --verbosity quiet

# If build fails, check:
# - Missing usings in other files
# - Test files still referencing deleted class
# - Configuration/registration code
```

**Test strategy:**
- Unit tests: May need deletion (tests for dead code)
- Integration tests: Should pass (unused code has no impact)
- Manual smoke test: Run app, verify no runtime errors

### 5. Git Commit with Context

```bash
git commit -m "Remove dead InventoryManager code

- Delete Systems/InventoryManager.cs (zero production callers)
- All inventory logic already consolidated in GameLoop
- Grep verification: only test files referenced it
- Build verified clean"
```

**Commit message best practices:**
- State what was deleted and why
- Document verification method (grep, build)
- Reference consolidation target (where logic lives now)
- Note any breaking changes (test files, configs)

## Anti-Patterns

**❌ Delete without grep:**
```bash
# WRONG: "This class looks unused, let's delete it"
rm Systems/InventoryManager.cs
```
Result: Runtime errors, broken tests, frustrated team.

**❌ Comment out instead of delete:**
```csharp
// Old implementation (commented out 2023-05-12)
// public class InventoryManager { ... }
```
Result: Code rot, confusion, git history cluttered.

**❌ "Soft delete" with feature flags:**
```csharp
if (!FeatureFlags.UseInventoryManager) {
    // New inline logic
} else {
    // Old manager logic
}
```
Result: Branch complexity, double maintenance, eventual tech debt.

## When NOT to Delete

**Keep code if:**
1. **Feature flag gated:** Intentionally disabled for gradual rollout
2. **API compatibility:** Public interface consumed by external clients
3. **Regulatory/audit:** Code required for compliance, even if unused
4. **Planned reactivation:** Temporary disablement (document timeline)

**Alternative to deletion:**
- Mark as obsolete with timeline: `[Obsolete("Remove after 2024-Q1")]`
- Move to archive namespace: `Legacy.InventoryManager`
- Extract to separate package: `Dungnz.Deprecated`

## Measurable Benefits

**Example: InventoryManager removal**
- **-59 lines of code** (class + tests)
- **-2 files** (Systems/InventoryManager.cs, Tests/InventoryManagerTests.cs)
- **-1 dependency** (GameLoop no longer needs to inject manager)
- **+1 design clarity** (single ownership in GameLoop)

**Team velocity impact:**
- Faster codebase navigation (fewer irrelevant search results)
- Reduced cognitive load (one less abstraction to understand)
- Simpler onboarding (fewer "what does this do?" questions)

## Summary

**Checklist for safe dead code removal:**

1. ✅ Grep entire codebase (code + tests + configs + docs)
2. ✅ Verify zero production callers (only test/coverage references OK)
3. ✅ Audit deleted code for unique logic (consolidate if needed)
4. ✅ Delete file(s)
5. ✅ Build verification (clean compile)
6. ✅ Test verification (existing tests pass)
7. ✅ Git commit with explanation
8. ✅ Update documentation if class was referenced

**Key insight:** Dead code is not "just in case" insurance—it's maintenance debt. Git history preserves old implementations if recovery is needed. Delete confidently with verification.
