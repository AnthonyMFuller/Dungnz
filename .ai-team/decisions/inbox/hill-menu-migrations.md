# Decision: Arrow-Key Menu Pattern for User Choices

**Date:** 2026-02-28  
**Author:** Hill  
**Status:** Implemented  

## Context

The game had a mix of UI patterns for user choices:
- Some interactions used numbered text-entry: "Enter 1, 2, or 3"
- The combat menu used `SelectFromMenu<T>()` for arrow-key navigation
- Test mode used `IInputReader.IsInteractive` to automatically fall back to text-entry

## Decision

**All user choice interactions should use the `SelectFromMenu<T>()` pattern via public wrapper methods in DisplayService.**

### Pattern Structure

1. **DisplayService private helper:**
   ```csharp
   private T SelectFromMenu<T>(
       IReadOnlyList<(string Label, T Value)> options,
       IInputReader input,
       string? header = null)
   ```
   - Handles both arrow-key navigation (interactive) and text-entry fallback (tests)
   - Delegates to `IMenuNavigator.Select()` for interactive mode
   - Performs number-parsing for non-interactive mode

2. **Public wrapper methods:**
   ```csharp
   public int ShowSomethingMenuAndSelect(/* params */)
   {
       var options = new (string Label, int Value)[]
       {
           ("Option 1 text", 1),
           ("Option 2 text", 2),
           ("Cancel/Leave", 0),
       };
       return SelectFromMenu(options.AsReadOnly(), _input, "=== Header ===");
   }
   ```

3. **Interface definition:**
   ```csharp
   /// <summary>Brief description of what menu this presents.</summary>
   int ShowSomethingMenuAndSelect(/* params */);
   ```

4. **GameLoop usage:**
   ```csharp
   var choice = _display.ShowSomethingMenuAndSelect(/* args */);
   switch (choice)
   {
       case 1: /* handle option 1 */ break;
       case 2: /* handle option 2 */ break;
       case 0: /* handle cancel/leave */ break;
   }
   ```

### Benefits

- **Consistency:** Same UI behavior everywhere (arrow-key navigation in game, number-entry in tests)
- **Separation of concerns:** Display layer owns all UI presentation and input handling
- **Test coverage:** Test helpers return fixed values to exercise all branches without interactive input
- **Zero logic changes:** GameLoop receives validated int/enum/object directly, no string parsing

### Examples Implemented

- `ShowCombatMenuAndSelect()` — Attack/Ability/Flee
- `ShowLevelUpChoiceAndSelect()` — HP/ATK/DEF stat boosts
- `ShowCraftMenuAndSelect()` — Recipe selection
- `ShowAbilityMenuAndSelect()` — Ability selection
- `ShowTrapChoiceAndSelect()` — Trap room approach (2 options + Leave)
- `ShowForgottenShrineMenuAndSelect()` — Shrine blessings (3 options + Leave)
- `ShowContestedArmoryMenuAndSelect()` — Armory approach (2 options + Leave)

### Test Helper Pattern

Both `FakeDisplayService` and `TestDisplayService` must implement stub versions:
```csharp
public int ShowSomethingMenuAndSelect(/* params */) 
{ 
    AllOutput.Add("something_menu"); 
    return 0; // or a fixed test value
}
```

## Impact

- **No breaking changes:** Logic layer unchanged (just replaces ReadLine/switch with menu call)
- **Test compatibility:** All tests continue to work via fallback text-entry mode
- **Future migrations:** Remaining numbered-choice interactions (if any) should follow this pattern

## Related PRs

- #635 — Initial SelectFromMenu pattern for combat/levelup/craft
- #642 — Migrated trap rooms, Forgotten Shrine, Contested Armory to this pattern
