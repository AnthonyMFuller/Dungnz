# Decision: Restore Visual Emojis, Use 🦺 for Chest/Armor

**Date:** 2026-03-02  
**Agent:** Hill  
**PR:** #833 (closes #832)

## Context

PR #830 replaced all wide visual emojis in `SpectreDisplayService.cs` with narrow Unicode symbols (⚔✦⛑◈✚☞≡⤓↩⛨) to fix an alignment issue. On investigation, the ONLY emoji that actually caused misalignment was 🛡 (U+1F6E1, SHIELD) — it has EAW=N (narrow, 1 terminal column) but was NOT included in the `NarrowEmoji` set, so `EL()` gave it only 1 space of padding instead of 2.

## Decision

Restore all original wide emojis. Replace ONLY 🛡 with 🦺 (U+1F9BA, safety vest).

### Emoji mapping restored
| Slot/Context | Old (#830) | New (#833) |
|---|---|---|
| Accessory slot | ✦ | 💍 |
| Head slot | ⛑ | 🪖 |
| Shoulders slot | ◈ | 🥋 |
| **Chest slot** | ✚ | **🦺** (not 🛡) |
| Hands slot | ☞ | 🧤 |
| Legs slot | ≡ | 👖 |
| Feet slot | ⤓ | 👟 |
| Back slot | ↩ | 🧥 |
| Prestige Level | ★ | ⭐ |
| Combat Ability | ✦ | ✨ |
| Combat Flee | ↗ | 🏃 |
| Combat Use Item | ⚗ | 🧪 |
| ItemType.Armor | ⛨ | 🦺 |
| ItemType.Consumable | ⚗ | 🧪 |
| ItemType.Accessory | ✦ | 💍 |
| ItemType.CraftingMaterial | ✶ | ⚗ |

### Why 🦺 for Chest
- EAW=W (2 terminal columns) — consistent with all other slot emojis
- Visually evokes body armor / breastplate / protective vest
- The original 🛡 was EAW=N and caused the alignment bug

## Helper: EL() replaces IL()

```csharp
private static readonly HashSet<string> NarrowEmoji = ["⚔", "⛨", "⚗", "☠", "★", "↩", "•"];
private static string EL(string emoji, string text) =>
    NarrowEmoji.Contains(emoji) ? $"{emoji}  {text}" : $"{emoji} {text}";
```

Wide emojis (EAW=W, 2 terminal columns) get 1 space. Narrow symbols get 2 spaces. Both produce consistent visual alignment.

## Rationale
The original broad replacement in #830 was unnecessary — 9 out of 10 emojis were fine. Restoring them makes the UI richer and more visually expressive.
