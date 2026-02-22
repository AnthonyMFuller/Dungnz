# UI/UX Improvement Plan — Visual Examples

**Before & After Comparisons**

---

## Example 1: Player Stats Display

### BEFORE (Current)
```
═══ PLAYER STATS ═══
Name: Thorin
Level: 5
HP: 45/60
Mana: 15/30
Attack: 18
Defense: 12
Gold: 320
XP: 450/500
```

### AFTER (Phase 1)
```
═══ PLAYER STATS ═══
Name: Thorin
Level: 5
HP: 45/60        ← yellow (75% health)
Mana: 15/30      ← cyan (50% mana)
Attack: 18       ← bright red
Defense: 12      ← cyan
Gold: 320        ← yellow
XP: 450/500      ← green
```

### AFTER (Phase 2 - with status effects)
```
═══ PLAYER STATS ═══
Name: Thorin
Level: 5
HP: 45/60        ← yellow
Mana: 15/30      ← cyan
Attack: 18       ← bright red
Defense: 12      ← cyan
Gold: 320        ← yellow
XP: 450/500      ← green

Active Effects:
  Poison (2 turns) - Taking 3 damage per turn     ← red
  Regen (3 turns) - Healing 4 HP per turn         ← green
```

---

## Example 2: Combat Status Line

### BEFORE (Current)
```
[You: 45/60 HP] vs [Goblin: 12/30 HP]

  You strike Goblin for 15 damage!
  Goblin attacks you for 8 damage!
```

### AFTER (Phase 1)
```
[You: 45/60 HP] vs [Goblin: 12/30 HP]    ← HP values colored by threshold
   ↑ yellow       ↑ red

  You strike Goblin for 15 damage!       ← 15 highlighted red
  Goblin attacks you for 8 damage!       ← 8 highlighted red
```

### AFTER (Phase 2 - Enhanced HUD)
```
[You: 45/60 HP | 15/30 MP | P(2) R(3)] vs [Goblin: 12/30 HP | W(2)]
      ↑ yellow    ↑ cyan   ↑red ↑green       ↑ red         ↑yellow

  You strike Goblin for 15 damage!       ← 15 bright red
  Goblin attacks you for 8 damage!       ← 8 bright red

Legend: P=Poison, R=Regen, W=Weakened, (X)=turns remaining
```

---

## Example 3: Equipment Comparison

### BEFORE (Current)
```
You equipped Iron Sword. Attack +5.
```

### AFTER (Phase 2)
```
════════════════════════════════════
Equipping: Iron Sword
────────────────────────────────────
Current Weapon: Rusty Dagger
  Attack: 10 → 15  (+5)    ← green for increase
  Defense: 5 → 5   (—)     ← gray for no change
════════════════════════════════════
Equipped Iron Sword
```

---

## Example 4: Inventory Display

### BEFORE (Current)
```
═══ INVENTORY ═══
• Health Potion (Consumable)
• Iron Sword (Weapon)
• Leather Armor (Armor)
• Mana Potion (Consumable)
• Rusty Dagger (Weapon)
```

### AFTER (Phase 2)
```
═══ INVENTORY ═══
Slots: 5/8  |  Weight: 42/50  |  Value: 320g
              ↑ green (<80%)        ↑ yellow
────────────────────────────────────────────
• Health Potion (Consumable) [3 wt] [25g]
• Iron Sword (Weapon) [8 wt] [50g]
• Leather Armor (Armor) [12 wt] [75g]
• Mana Potion (Consumable) [3 wt] [20g]
• Rusty Dagger (Weapon) [5 wt] [15g]
                         ↑ weights shown
```

---

## Example 5: Ability Menu

### BEFORE (Current)
```
Choose an ability:
[1] Power Strike (10 MP, CD: 2 turns)
[2] Defensive Stance (8 MP, CD: 3 turns)
[3] Poison Dart (12 MP, CD: 4 turns)
[4] Second Wind (15 MP, CD: 5 turns)

Mana: 15/30
```

### AFTER (Phase 3)
```
Choose an ability:
[1] Power Strike (10 MP, ready)        ← green bold (ready!)
[2] Defensive Stance (8 MP, ready)     ← green bold
[3] Poison Dart (12 MP, 2 turns)       ← gray (on cooldown)
[4] Second Wind (15 MP, 3 turns)       ← gray (on cooldown)

Mana: 15/30  ← cyan
```

---

## Example 6: Combat Critical Hit

### BEFORE (Current)
```
  💥 CRUSHING BLOW! You put your entire body into it — 30 devastating damage to Goblin!
```

### AFTER (Phase 2)
```
  💥 CRUSHING BLOW! You put your entire body into it — 30 devastating damage to Goblin!
                                                        ↑ bright yellow with bold
```

---

## Example 7: Achievement Progress

### BEFORE (Current - on game end)
```
═══ ACHIEVEMENTS UNLOCKED ═══
🏆 Glass Cannon — Win with HP below 10
```

### AFTER (Phase 3 - shows locked achievements with progress)
```
═══ ACHIEVEMENTS ═══

UNLOCKED:
🏆 Glass Cannon — Win with HP below 10

PROGRESS:
❌ Speed Runner: 142 turns (need <100) — 71% progress    ← red (far from goal)
❌ Hoarder: 320g / 500g — 64% progress                   ← yellow (moderate)
❌ Elite Hunter: 8/10 enemies defeated — 80% progress    ← green (close!)
```

---

## Example 8: Room Description

### BEFORE (Current)
```
🏛 Ancient runes line the walls. This chamber feels sacred.

Exits: NORTH, EAST
⚠ Dark Knight is here!
Items: Health Potion
```

### AFTER (Phase 3)
```
🏛 Ancient runes line the walls. This chamber feels sacred.
↑ cyan (safe room type)

Exits: NORTH, EAST
⚠ Dark Knight is here!    ← bright red bold (danger!)
Items: Health Potion       ← yellow (loot)
```

---

## Example 9: Combat Turn Log

### BEFORE (Current - can scroll indefinitely)
```
Turn 1: You attack Goblin for 12 damage
Turn 2: Goblin attacks you for 8 damage
Turn 3: You use Power Strike for 24 damage!
Turn 4: Goblin attacks you for 8 damage
Turn 5: You attack Goblin for 12 damage
Turn 6: Goblin misses!
Turn 7: You attack Goblin for 12 damage
```

### AFTER (Phase 3 - last 5 turns, colored)
```
Recent Turns (last 5):
  Turn 3: You use Power Strike for 24 damage!    ← green (player action)
  Turn 4: Goblin attacks you for 8 damage        ← red (enemy action)
  Turn 5: You attack Goblin for 12 damage        ← green
  Turn 6: Goblin misses!                         ← red
  Turn 7: You attack Goblin for 12 damage        ← green
```

---

## Color Palette Reference

| Element | ANSI Code | Example Use |
|---------|-----------|-------------|
| Red | `\u001b[31m` | HP (low), damage taken, errors |
| Green | `\u001b[32m` | HP (high), healing, XP, success |
| Yellow | `\u001b[33m` | HP (medium), gold, warnings |
| Blue | `\u001b[34m` | Mana (high), abilities |
| Cyan | `\u001b[36m` | Mana (medium), defense |
| Bright Red | `\u001b[91m` | Attack stat, critical damage |
| Bright Yellow | `\u001b[93m` | Critical hits, legendary items |
| Gray | `\u001b[90m` | Cooldowns, disabled options |

---

## Key Benefits

✅ **Instant health assessment** — Color-coded HP bars let players judge danger at a glance  
✅ **Active effect visibility** — Combat HUD shows buffs/debuffs persistently  
✅ **Informed decisions** — Equipment comparison shows stat changes before committing  
✅ **Goal clarity** — Achievement progress shows how close players are to unlocks  
✅ **Combat clarity** — Colored damage/healing stands out from narrative text  
✅ **Resource management** — Mana threshold colors warn when running low  
✅ **Ability readiness** — Cooldown colors instantly show what's available  

All while maintaining **full accessibility** — every color enhancement preserves existing emoji/text indicators!
