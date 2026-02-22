# Content Expansion Implementation — 2026-02-22

**Requested by:** Anthony

## What Happened
Full content expansion plan implemented across 4 phases.

### Phase 1: Code Hardening
- **PRs merged:** #249, #250, #251
- ItemConfig hardening, display truncation, Accessory equip fixes

### Phase 2: Content
- **PRs merged:** #255 (8 new enemies), #259 (40 new items)
- Game now has 58 items and 18 enemies

### Phase 3: Testing
- **PRs merged:** #262 (loot distribution), #263 (UI regression), #264 (combat balance)
- 416 tests total
- Romanoff flagged Lich King balance bug

### Phase 4: Map
- **PRs merged:** #260 (color constants), #261 (ShowMap overhaul)
- Features: fog of war, corridor connectors, room-type colors, legend

### Balance Fix
- **PR merged:** #265
- Lich King HP: 120 → 170, ATK: 28 → 38
- Win rate at Lvl 12: ~79%
