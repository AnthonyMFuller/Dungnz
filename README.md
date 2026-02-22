# Dungnz

A turn-based roguelike dungeon-crawler for the terminal, written in C# (.NET 10). Explore procedurally generated floors, fight enemies, level up, unlock abilities and skills, craft gear, and defeat the final boss to escape.

---

## Demo

```
══════════════════════════════════════════
  Floor 2 — Ancient Chamber
══════════════════════════════════════════
  The air here smells of old stone and
  burnt incense. Faded runes line the walls.

  Exits: NORTH, EAST
  Enemy: Dark Knight [45 HP]
  Items: Iron Sword

> _
```

---

## Getting Started

**Prerequisites:** [.NET 10 SDK](https://dotnet.microsoft.com/download)

```bash
git clone https://github.com/AnthonyMFuller/Dungnz.git
cd Dungnz
dotnet run
```

---

## Gameplay

### Core loop

```
Choose class → Explore floor → Fight enemies → Loot rooms
    → Level up → Unlock skills/abilities → Descend → Defeat boss
```

- **Win:** Reach the exit room and defeat the dungeon boss on **Floor 5**.
- **Lose:** Your HP drops to 0 in combat.
- Each floor is a freshly generated grid of interconnected rooms.
- Every 2 levels you are offered a bonus trait: +5 Max HP, +2 Attack, or +2 Defense.
- Defeating the boss grants a **prestige point** every 3 wins, giving permanent cross-run bonuses.

---

## Player Classes

Choose one class at the start of each run. Bonuses are applied on top of base stats (100 HP, 10 ATK, 5 DEF, 30 Mana).

| Class | HP Mod | ATK Mod | DEF Mod | Mana Mod | Passive Trait |
|-------|--------|---------|---------|----------|---------------|
| **Warrior** | +20 | +3 | +2 | −10 | +5% damage when HP < 50% |
| **Mage** | −10 | — | −1 | +30 | Spells deal +20% damage |
| **Rogue** | — | +2 | — | — | +10% dodge chance |

---

## Commands

| Command | Aliases | Description |
|---------|---------|-------------|
| `go <dir>` | `north`/`n`, `south`/`s`, `east`/`e`, `west`/`w` | Move to adjacent room |
| `look` | `l` | Redescribe current room |
| `examine <target>` | `ex` | Inspect an enemy or item |
| `take <item>` | `get` | Pick up an item |
| `use <item>` | | Use consumable or equip gear |
| `inventory` | `inv`, `i` | List carried items |
| `equipment` | `gear` | Show equipped items and their bonuses |
| `equip <item>` | | Equip a weapon or armour from inventory |
| `unequip <slot>` | | Remove equipped item back to inventory |
| `stats` | `status` | View current player stats |
| `map` | `m` | Display ASCII dungeon map |
| `shop` | `buy` | Browse merchant's wares |
| `skills` | `skill` | List skills and unlock status |
| `learn <skill>` | | Unlock a skill (requires minimum level) |
| `craft <recipe>` | | Craft an item from ingredients |
| `save <name>` | | Save current game state |
| `load <name>` | | Load a saved game |
| `list` | `saves` | List all save files |
| `descend` | `down` | Descend to next floor at a cleared exit |
| `prestige` | `p` | View prestige level and bonuses |
| `leaderboard` | `lb`, `scores` | View achievements and run history |
| `help` | `?`, `h` | Show command list |
| `quit` | `exit`, `q` | Exit the game |

---

## Combat

### Turn structure

1. Player chooses to **attack**, **use an ability**, or **use an item**.
2. Status effects tick at the end of each round.
3. If the enemy survives, it attacks the player.
4. Combat ends when either side reaches 0 HP.

### Damage formula

```
damage = Math.Max(1, attacker.Attack - defender.Defense)
```

### Abilities

Abilities cost mana and have a cooldown measured in turns. Unlocked automatically on reaching the required level.

| Ability | Mana | Cooldown | Unlocked | Effect |
|---------|------|----------|----------|--------|
| Power Strike | 10 | 2 turns | Level 1 | Deal 2× normal damage |
| Defensive Stance | 8 | 3 turns | Level 3 | +50% DEF for 2 turns (Fortified) |
| Poison Dart | 12 | 4 turns | Level 5 | Apply Poison to enemy |
| Second Wind | 15 | 5 turns | Level 7 | Heal 30% of Max HP |

### Status effects

| Effect | Per-turn | Duration | Notes |
|--------|----------|----------|-------|
| Poison | −3 HP | 3 turns | Applied by Lich King, Poison Dart, certain rooms |
| Bleed | −5 HP | varies | Applied by bleed weapons |
| Stun | skip turn | 1 turn | Target cannot act |
| Regen | +4 HP | varies | Applied by shrines and items |
| Fortified | +50% DEF | 2 turns | Applied by Defensive Stance |
| Weakened | −50% ATK | varies | Applied by certain enemies |

---

## Progression

### Levelling

Every **100 XP** = 1 level. On level-up:

| Gain | Amount |
|------|--------|
| Attack | +2 |
| Defense | +1 |
| Max HP | +10 |
| Max Mana | +10 |
| HP/Mana | fully restored |

Every **2 levels** you also choose a bonus trait: +5 Max HP, +2 Attack, or +2 Defense.

### Skill tree

Skills are passive bonuses unlocked for free with `learn <skill>` once you meet the level requirement. Each skill can only be unlocked once per run.

| Skill | Min Level | Effect |
|-------|-----------|--------|
| PowerStrike | 3 | +15% attack damage |
| IronSkin | 3 | +3 Defense (immediate) |
| Swiftness | 5 | +5% dodge chance |
| ManaFlow | 4 | +10 max mana, +5 mana/turn |
| BattleHardened | 6 | Take 5% less damage |

### Prestige

Prestige data is saved to `%AppData%/Dungnz/prestige.json` and persists across all runs.

- Every **3 wins** grants +1 prestige level.
- Each prestige level adds **+1 ATK, +1 DEF, +5 Max HP** applied at the start of every future run.
- View current prestige with the `prestige` command.

---

## Enemies

### Regular enemies

| Enemy | HP | ATK | DEF | Notable |
|-------|----|-----|-----|---------|
| Goblin | 20 | 8 | 2 | Fast, weak |
| Skeleton | 30 | 12 | 5 | Drops Rusty Sword |
| Troll | 60 | 10 | 8 | Drops Troll Hide armour |
| Dark Knight | 45 | 18 | 12 | Drops Dark Blade + Knight's Armor |
| Goblin Shaman | 25 | 10 | 4 | Can heal nearby enemies |
| Stone Golem | 90 | 8 | 20 | Immune to status effects |
| Wraith | 35 | 18 | 2 | 30% flat dodge chance |
| Vampire Lord | 80 | 16 | 12 | 50% lifesteal on attacks |
| Mimic | 40 | 14 | 8 | Disguised as a chest |

### Boss variants (one randomly selected per run)

| Boss | HP | ATK | DEF | Special |
|------|----|-----|-----|---------|
| Dungeon Boss | 100 | 22 | 15 | Enrages at ≤40% HP (+50% ATK) |
| Lich King | 120 | 18 | 5 | Applies Poison on every hit |
| Stone Titan | 200 | 22 | 15 | Extreme tank, no specials |
| Shadow Wraith | 90 | 25 | 3 | 25% flat dodge chance |
| Vampire Boss | 110 | 20 | 8 | 30% lifesteal on attacks |

---

## Dungeon Systems

### Room types

Rooms have an environmental flavour that affects the description: **Standard**, **Dark**, **Mossy**, **Flooded**, **Scorched**, **Ancient**.

### Hazards

Some rooms contain a floor hazard that triggers on entry:

| Hazard | Effect |
|--------|--------|
| Spike | Deals physical damage |
| Poison | Applies Poison status |
| Fire | Deals burn damage |

### Shrines

One-use interactive altars found in some rooms (`use shrine`):

| Option | Cost | Effect |
|--------|------|--------|
| Heal | 30g | Restore HP to full |
| Bless | 50g | +2 ATK and +2 DEF permanently |
| Fortify | 75g | +10 Max HP permanently |
| Meditate | 75g | +10 Max Mana permanently |

### Merchants

Shop rooms contain a **Merchant** (`shop` command) selling consumables and gear including Health Potions (25g), Mana Potions (20g), Iron Sword, Leather Armor, and Elixir of Strength (80g).

### Crafting

Use `craft <recipe>` to combine ingredients. Gold is consumed along with the listed items.

| Recipe | Ingredients | Gold | Result |
|--------|------------|------|--------|
| Health Elixir | 2× Health Potion | — | Heals 75 HP |
| Reinforced Sword | 1× Iron Sword | 30g | +8 ATK weapon |
| Reinforced Armor | 1× Leather Armor | 25g | +8 DEF armour |

---

## Display & Colours

Dungnz uses native ANSI escape codes — no third-party dependencies. Colour support is automatic on modern terminals (Windows Terminal, macOS Terminal, Linux).

### What's colour-coded

| Element | Colour |
|---------|--------|
| HP — healthy (> 70%) | Green |
| HP — injured (40–70%) | Yellow |
| HP — critical (20–40%) | Red |
| HP — near death (≤ 20%) | Bright Red |
| Mana — high / medium / low | Blue / Cyan / Gray |
| Gold | Yellow |
| XP gained | Green |
| Attack stat | Bright Red |
| Defense stat | Cyan |
| Combat damage | Red |
| Healing / regen | Green |
| Critical hits | Yellow + Bold |
| Room danger level | Scales green → red |
| Ability on cooldown | Dimmed |

All console output is routed through `IDisplayService` / `DisplayService` — colour constants live in `Systems/ColorCodes.cs`. When equipping items, `EquipmentManager` shows stat deltas with green `+` / red `−` indicators.

The `ColorizeDamage` helper in `CombatEngine` targets only the **last** occurrence of the damage value in a narration string (via `LastIndexOf`) to avoid accidentally colourising an identical number that appears earlier in the message text.

---

## Save System

Game state is persisted to JSON in the user's AppData folder:

```
%AppData%/Dungnz/saves/<name>.json   (Windows)
~/.config/Dungnz/saves/<name>.json   (Linux/macOS)
```

| Command | Description |
|---------|-------------|
| `save <name>` | Save current run to a named slot |
| `load <name>` | Restore a saved run by name |
| `list` / `saves` | Show all available save files |

Prestige data lives at `Dungnz/prestige.json` and achievement history at `Dungnz/achievements.json` in the same folder — separate from run saves.

---

## Achievements

Achievements are evaluated on a **won run** only and are saved permanently.

| Achievement | Condition |
|-------------|-----------|
| Glass Cannon | Win with HP below 10 |
| Untouchable | Win without taking any damage |
| Hoarder | Collect 500+ gold in a single run |
| Elite Hunter | Defeat 10+ enemies in a single run |
| Speed Runner | Win in fewer than 100 turns |

View unlocked achievements with `leaderboard`.

---

## Architecture

```
Dungnz/
├── Program.cs                   # Entry point — wires dependencies and starts game
├── Engine/
│   ├── GameLoop.cs              # Main state machine; all command dispatch and win/loss logic
│   ├── CombatEngine.cs          # Turn-based combat, XP award, level-up flow
│   ├── ICombatEngine.cs         # Interface enabling test doubles
│   ├── StubCombatEngine.cs      # Deterministic test stub
│   ├── CommandParser.cs         # Maps raw text input → ParsedCommand (verb + argument)
│   ├── DungeonGenerator.cs      # Procedural grid generation with BFS connectivity check
│   └── EnemyFactory.cs          # Spawns regular enemies or boss variants by floor
├── Models/                      # Pure data: Player, Room, Enemy, Item, enums, events
├── Data/
│   ├── enemy-stats.json         # Override stats for enemies (used by EnemyFactory)
│   └── item-stats.json          # Override stats for loot items
├── Display/
│   ├── IDisplayService.cs       # Output abstraction (all console writes go through here)
│   └── DisplayService.cs        # Concrete ANSI console implementation
└── Systems/
    ├── AbilityManager.cs        # Ability registry, cooldown tracking, ability execution
    ├── AchievementSystem.cs     # Run-end achievement evaluation and persistence
    ├── AmbientEvents.cs         # Random flavour events on room entry
    ├── BossNarration.cs         # Boss-specific combat flavour text
    ├── ColorCodes.cs            # ANSI colour constants and helper methods
    ├── CraftingSystem.cs        # Recipe catalogue and TryCraft logic
    ├── EquipmentManager.cs      # Equip/unequip with stat delta application (colour-coded deltas)
    ├── InventoryManager.cs      # Take, use, and item dispatch
    ├── NarrationService.cs      # General combat and exploration flavour text
    ├── PrestigeSystem.cs        # Cross-run win tracking and prestige bonuses
    ├── SaveSystem.cs            # Full game-state serialisation/deserialisation
    ├── SkillTree.cs             # Passive skill unlock and bonus application
    ├── StatusEffectManager.cs   # Per-turn effect ticking and stat modifiers
    └── Enemies/                 # One class per enemy type + BossVariants.cs
```

---

## Testing

**267 tests** across **19 test files** in `Dungnz.Tests/`.

```bash
dotnet test Dungnz.Tests
```

Coverage includes: `CombatEngine`, `CommandParser`, `CraftingSystem`, `SkillTree`, `PrestigeSystem`, `AchievementSystem`, `StatusEffectManager`, `EquipmentSystem`, `InventoryManager`, `SaveSystem`, `GameLoop` (integration), `DungeonGenerator`, `EnemyFactory`, `LootTable`, `Player`, `DisplayService`, `NarrationService`, and stub combat engine.

---

## Contributing

- **XML docs required** on all public members — the project enforces this via existing doc comments.
- **Update README.md** when changing any documented system (commands, enemies, abilities, skills, crafting, saves, achievements). The `readme-check` CI workflow will fail PRs that modify `Engine/`, `Systems/`, `Models/`, or `Data/` without a matching `README.md` change.
- Run `dotnet test Dungnz.Tests` before opening a PR — all 267 tests must pass.
- Follow existing code style: one class per file, `PascalCase` types, `camelCase` locals.
