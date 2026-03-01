### 2025-03-01: All game icons standardized to narrow Unicode symbols

**By:** Hill  
**What:** Replaced all wide emoji icons (U+1F300+) with narrow Unicode symbols (U+2600-U+27BF). Simplified EL() helper to IL() with uniform 2-space padding.  
**Why:** Anthony's executive directive — same character set, no width ambiguity. Eliminates the entire class of Spectre.Console cell-width misalignment bugs.

**Symbol mapping:**
- Equipment: ⚔ Weapon, ✦ Accessory, ⛑ Head, ◈ Shoulders, ⛨ Chest, ☞ Hands, ≡ Legs, ⤓ Feet, ↩ Back, ⛨ Off-Hand
- Stats: ★ Level, ✦ Combo
- Combat: ⚔ Attack, ✦ Ability, ↗ Flee, ⚗ Use Item
- Item types: ⚔ Weapon, ⛨ Armor, ⚗ Consumable, ✦ Accessory, ✶ CraftingMaterial

All icons are now EAW=N (1 terminal column), rendered consistently across all terminal emulators.
