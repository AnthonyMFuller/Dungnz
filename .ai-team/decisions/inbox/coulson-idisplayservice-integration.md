### 2026-02-20: IDisplayService Integration — Verify Entrypoints After Refactoring

**By:** Coulson  
**Context:** GitHub #1, PR #27

**What:**  
Always verify entrypoint files (Program.cs, Main methods) are updated after interface extraction or class renaming refactors.

**Why:**  
IDisplayService extraction was completed in commit 32184c6 (test infrastructure work):
- Interface extracted ✓
- DisplayService renamed to ConsoleDisplayService ✓  
- GameLoop/CombatEngine constructors updated ✓
- TestDisplayService test double created ✓
- BUT Program.cs still instantiated `new DisplayService()` ✗

This caused build failure when attempting to ship the refactor. The test suite passed because tests used TestDisplayService, masking the production entrypoint issue.

**Decision:**  
When completing interface extraction or class renaming:
1. Run `dotnet build` from clean state (not just `dotnet test`)
2. Explicitly check all entrypoints that instantiate the renamed/extracted class
3. Search for `new OldClassName()` references across the codebase
4. Verify production code paths, not just test code paths

**Pattern:**  
```bash
# After renaming DisplayService → ConsoleDisplayService
$ rg "new DisplayService\(\)" --type cs
Program.cs:5:var display = new DisplayService();  # ← Missed this!

# Fix before committing
$ sed -i 's/new DisplayService()/new ConsoleDisplayService()/g' Program.cs
```

**Applies To:**
- Interface extraction refactors
- Class/type renaming
- Namespace changes
- Factory method introductions
