# Changelog

All notable changes to this project will be documented in this file.

## [Unreleased]

## [2026-03-02] — PR #847: Inventory UX Features

### Added
- **COMPARE command** — Display side-by-side stat comparison for an inventory item vs. currently equipped gear; omit item name for interactive menu (issue #844)
- **Enhanced EXAMINE** — Auto-shows comparison for equippable inventory items after detail card (issue #845)
- **Interactive INVENTORY** — Arrow-key navigable menu for selecting items with detail and comparison display (issue #846)

### Technical Details
- Branch: `squad/844-845-846-inspect-compare`
- Issues Closed: #844, #845, #846
- Tests Added: 15 (CommandParser: 3, GameLoop: 8, InventoryDisplay: 4)
- Build Status: ✅ All tests passing, 0 errors
- Backward Compatibility: ✅ All changes are non-breaking

### Files Modified
- Engine/CommandParser.cs — Added Compare enum and parsing
- Engine/GameLoop.cs — Added HandleCompare, enhanced HandleExamine, updated Inventory dispatcher
- Display/IDisplayService.cs — Added ShowInventoryAndSelect signature
- Display/SpectreDisplayService.cs — Implemented ShowInventoryAndSelect with SelectionPrompt
- Display/DisplayService.cs — Implemented ShowInventoryAndSelect with numbered input fallback
- Dungnz.Tests/Helpers/FakeDisplayService.cs — Added ShowInventoryAndSelect stub
- Dungnz.Tests/Helpers/TestDisplayService.cs — Added ShowInventoryAndSelect stub
- Dungnz.Tests/CommandParserTests.cs — Added 2 Compare parsing tests
- Dungnz.Tests/GameLoopCommandTests.cs — Added 8 Compare/Examine tests
- Dungnz.Tests/InventoryDisplayRegressionTests.cs — Added 4 ShowInventoryAndSelect tests
- README.md — Updated commands table with new functionality
