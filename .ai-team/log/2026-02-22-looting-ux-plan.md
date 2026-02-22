# Looting UX Planning Session
**Date:** 2026-02-22
**Requested by:** Anthony

## Audit Findings

### Hill - Looting Display Surfaces
Audited 10 looting display surfaces — all underutilize available item data

### Barton - Loot Systems & Item Data Model
Audited loot systems and item data model — 14 properties on Item, only Name shown on drops

## Synthesis — Coulson's 3-Phase Improvement Plan

### Phase 1 (5h, display-only)
- Loot drop cards
- Gold color
- Room item type icons
- Pickup stats
- Examine stat card
- Inventory enhancement

### Phase 2 (5h, model change)
- Add ItemTier enum + dependent tier-colored names
- Merchant affordability
- Crafting status

### Phase 3 (4h, polish)
- Consumable grouping
- Elite loot callout
- Weight warning
- Upgrade indicator

## Architectural Decision
Add ItemTier enum (Common/Uncommon/Rare) as explicit property rather than deriving from LootTable

## Status
Plan delivered to Anthony — awaiting approval before implementation begins
