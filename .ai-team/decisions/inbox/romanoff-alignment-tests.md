### 2026-03-01: Alignment regression tests written
**By:** Romanoff  
**What:** Added AlignmentRegressionTests.cs with 6 regression tests for the alignment bugs  
**Why:** Prevent regressions — Boss was hitting alignment issues frequently

## Test Coverage

Wrote 6 regression tests in `Dungnz.Tests/AlignmentRegressionTests.cs`:

1. **ShowItemDetail_WeaponWithWideIcon_AllBoxLinesHaveConsistentVisualWidth**  
   Tests weapon items with ⚔ icon alignment

2. **ShowShop_WeaponWithWideIcon_AllBoxLinesHaveConsistentVisualWidth**  
   Tests shop display with weapon items containing ⚔ icon

3. **ShowEnemyDetail_EliteEnemyWithWideIcon_AllBoxLinesHaveConsistentVisualWidth**  
   Tests elite enemy display with ⭐ icon alignment

4. **ShowVictory_AllBoxLinesHaveConsistentVisualWidth**  
   Tests victory screen with ✦ icon alignment

5. **ShowGameOver_AllBoxLinesHaveConsistentVisualWidth**  
   Tests game over screen with ☠ icon alignment

6. **ShowItemDetail_ArmorWithSurrogatePairIcon_AllBoxLinesHaveConsistentVisualWidth**  
   Tests armor items with 🛡 surrogate pair icon

## Pattern Used

Each test:
- Captures console output via `StringWriter`
- Uses `BoxWidth()` helper to find expected width from ╔...╗ border line
- Strips ANSI codes from all ║ content lines using `ColorCodes.StripAnsiCodes()`
- Asserts all stripped content lines match the border width

## Current Status

✅ **Tests compile successfully**  
❌ **All 6 tests currently fail** (expected behavior)

The failures correctly detect the pre-fix alignment bugs:
- Tests 1-4: 1 column short (wide BMP char like ⚔, ⭐, ✦)
- Test 5: 2 columns too wide (☠ over-corrected)
- Test 6: Fails due to related issue

These tests should **pass after Hill applies the alignment fixes**.

## Integration

- Uses `[Collection("console-output")]` to prevent console capture conflicts
- Follows existing test pattern from `ShowEquipmentComparisonAlignmentTests.cs`
- Tests are marked as regression tests to prevent future alignment regressions
