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

### Enhanced Intro Sequence

The game features an immersive introduction with:
- **ASCII art title screen** with color-coded borders and dramatic tagline
- **Atmospheric lore narrative** setting the scene before your descent
- **Prestige card display** showing your accumulated bonuses from previous wins
- **Interactive class selection** with ASCII stat bars showing HP, Attack, Defense, and Mana with prestige bonuses highlighted
- **Difficulty cards** with clear mechanical impact summaries (enemy power, loot, and gold multipliers)

### TUI Mode (Experimental)

An optional split-screen TUI display powered by [Terminal.Gui](https://github.com/gui-cs/Terminal.Gui) is available.

```bash
dotnet run -- --tui
```

**Features:**
- Real-time split-screen layout: map (top-left), player stats (top-right), content area (bottom), and message log (persistent footer)
- Live dungeon map visualization with your position marked
- Responsive inventory and menu interfaces
- Improved visual separation of game information

**Requirements:**
- Terminal supporting Unicode/UTF-8 (Linux/macOS shells, Windows Terminal, ConEmu, etc.)
- Minimum terminal size: 100×30 characters recommended

**Note:** TUI mode is **experimental** and optional. The default display uses [Spectre.Console](https://spectreconsole.net/) and remains the stable choice. If you encounter issues with TUI rendering, either resize your terminal or use the default mode (`dotnet run`).

For detailed TUI architecture and troubleshooting, see [docs/TUI-ARCHITECTURE.md](docs/TUI-ARCHITECTURE.md). For rollback instructions, see [docs/ROLLBACK.md](docs/ROLLBACK.md).

---

## Gameplay

### Core loop

```
Choose class → Explore floor → Fight enemies → Loot rooms
    → Level up → Unlock skills/abilities → Descend → Defeat boss
```

- **Win:** Reach the exit room and defeat the dungeon boss. The dungeon spans **8 floors**, with new bosses on floors 6 (Archlich Sovereign), 7 (Abyssal Leviathan), and 8 (Infernal Dragon).
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
| **Paladin** | +15 | +2 | +3 | — | Divine Shield & Holy Strike; bonus damage vs undead |
| **Necromancer** | −5 | — | −2 | +20 | Raise Dead; summon skeleton minions from fallen enemies |
| **Ranger** | — | +1 | — | +10 | Wolf Companion; trap synergy; Volley multi-attack |

---

## Commands

| Command | Aliases | Description |
|---------|---------|-------------|
| `go <dir>` | `north`/`n`, `south`/`s`, `east`/`e`, `west`/`w` | Move to adjacent room |
| `look` | `l` | Redescribe current room |
| `examine <target>` | `ex` | Inspect an enemy or item; for equippable inventory items, auto-shows comparison vs. currently equipped |
| `take <item>` | `get` | Pick up an item |
| `use <item>` | | Use a consumable item; omit item name to pick from an interactive menu; accepts fuzzy/typo matching |
| `inventory` | `inv`, `i` | Interactive item browser with arrow-key selection; displays details and comparison for selected equippable items |
| `compare <item>` | `comp` | Display side-by-side stat comparison for an inventory item vs. currently equipped gear; omit item name for interactive menu |
| `equipment` | `gear` | Show equipped items and their bonuses |
| `equip <item>` | | Equip a weapon or armour from inventory; omit item name to pick from an interactive menu |
| `unequip <slot>` | | Remove equipped item back to inventory |
| `stats` | `status` | View current player stats |
| `map` | `m` | Display ASCII dungeon map |
| `shop` | `buy` | Browse merchant's wares |
| `skills` | `skill` | Open interactive skill tree menu; select a skill to learn |
| `learn <skill>` | | Unlock a skill by name (text command shortcut) |
| `craft <recipe>` | | Craft an item from ingredients |
| `save <name>` | | Save current game state |
| `load <name>` | | Load a saved game |
| `list` | `saves` | List all save files |
| `descend` | `down` | Descend to next floor at a cleared exit |
| `ascend` | `up`, `u` | Return to the previous floor from the current floor entrance |
| `return` | `retreat` | Fast-travel to the entrance room of the current floor (blocked during combat) |
| `back` | `b` | Step back to the previous room |
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

**Warrior**

| Ability | Mana | CD | Unlocked | Effect |
|---------|------|----|----------|--------|
| Shield Bash | 8 | 2 | Level 1 | Strike with shield, stunning the enemy |
| Battle Cry | 10 | 4 | Level 2 | Rally, boosting attack power |
| Fortify | 12 | 3 | Level 3 | Reduce incoming damage |
| Reckless Blow | 15 | 3 | Level 5 | Massive damage, reduces DEF temporarily |
| Last Stand | 20 | 6 | Level 7 | Desperate damage surge at low HP |

**Mage**

| Ability | Mana | CD | Unlocked | Effect |
|---------|------|----|----------|--------|
| Arcane Bolt | 8 | 0 | Level 1 | Magical damage |
| Frost Nova | 14 | 3 | Level 2 | Slow enemy for 2 turns |
| Mana Shield | 0 | 5 | Level 4 | Absorb damage with mana |
| Arcane Sacrifice | 0 | 3 | Level 5 | Sacrifice HP to restore mana |
| Meteor | 35 | 5 | Level 7 | Devastating magical strike |

**Rogue**

| Ability | Mana | CD | Unlocked | Effect |
|---------|------|----|----------|--------|
| Quick Strike | 5 | 0 | Level 1 | Fast attack, combo-ready |
| Backstab | 10 | 2 | Level 2 | Bonus damage from behind |
| Evade | 12 | 4 | Level 3 | Dodge next enemy attack |
| Flurry | 15 | 3 | Level 5 | Rapid succession of attacks |
| Assassinate | 25 | 6 | Level 7 | Execute with massive damage |

**Paladin**

| Ability | Mana | CD | Unlocked | Effect |
|---------|------|----|----------|--------|
| Holy Strike | 8 | 0 | Level 1 | 150% damage vs undead |
| Lay on Hands | 15 | 4 | Level 2 | Heal 40% Max HP (50% when HP < 25%) |
| Divine Shield | 12 | 3 | Level 3 | Absorb all damage for 2 turns |
| Consecrate | 18 | 3 | Level 5 | Holy damage + Bleed; Stun vs undead |
| Judgment | 25 | 5 | Level 7 | Damage scales with missing HP; execute at ≤20% |

**Necromancer**

| Ability | Mana | CD | Unlocked | Effect |
|---------|------|----|----------|--------|
| Death Bolt | 8 | 0 | Level 1 | 120% vs poisoned/bleeding targets |
| Curse | 10 | 2 | Level 2 | Weaken enemy: −25% damage for 3 turns |
| Raise Dead | 25 | 4 | Level 3 | Raise a skeleton from the last slain enemy |
| Life Drain | 15 | 2 | Level 5 | Deal damage and heal for the full amount |
| Corpse Explosion | 30 | 5 | Level 7 | Sacrifice all minions for massive damage |

**Ranger**

| Ability | Mana | CD | Unlocked | Effect |
|---------|------|----|----------|--------|
| Precise Shot | 6 | 0 | Level 1 | 130% vs enemies with a status effect |
| Lay Trap (Poison) | 10 | 3 | Level 2 | Poison trap triggers before next enemy attack |
| Summon Companion | 15 | 5 | Level 3 | Summon a wolf companion |
| Lay Trap (Snare) | 12 | 3 | Level 5 | Snare trap slows and stuns |
| Volley | 25 | 4 | Level 7 | Multi-attack; +30% with wolf or active trap |

### Status effects

| Effect | Per-turn | Duration | Notes |
|--------|----------|----------|-------|
| Poison | −3 HP | 3 turns | Applied by Lich King, Poison Dart, certain rooms |
| Bleed | −5 HP | varies | Applied by bleed weapons |
| Stun | skip turn | 1 turn | Target cannot act |
| Regen | +4 HP | varies | Applied by shrines and items |
| Fortified | +50% DEF | 2 turns | Applied by Defensive Stance |
| Weakened | −50% ATK | varies | Applied by certain enemies |
| Slow | −25% ATK | varies | Applied by certain enemies |
| Burn | −8 HP | 3 turns | Applied by Infernal Dragon's Flame Breath |

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

Skills are passive bonuses unlocked for free once you meet the level requirement. Use the `skills` command to open an interactive arrow-key menu showing all skills for your class (locked ones shown with their level requirement). Select a skill to learn it, or use `learn <skill>` as a text command shortcut. Each skill can only be unlocked once per run.

Universal skills (available to all classes):

| Skill | Min Level | Effect |
|-------|-----------|--------|
| PowerStrike | 3 | +15% attack damage |
| IronSkin | 3 | +3 Defense (immediate) |
| Swiftness | 5 | +5% dodge chance |
| ManaFlow | 4 | +10 max mana, +5 mana/turn |
| BattleHardened | 6 | Take 5% less damage |

**Warrior** class skills:

| Skill | Min Level | Effect |
|-------|-----------|--------|
| IronConstitution | 3 | +15 MaxHP, +5% damage reduction |
| UndyingWill | 5 | Apply Regen for 3 turns when HP drops below 25% (once per combat) |
| BerserkersEdge | 6 | +10% damage per 25% HP missing |
| Unbreakable | 8 | Last Stand triggers at ≤50% HP (default 40%) |

### Prestige

Prestige data is saved to `%AppData%/Dungnz/prestige.json` and persists across all runs.

- Every **3 wins** grants +1 prestige level.
- Each prestige level adds **+1 ATK, +1 DEF, +5 Max HP** applied at the start of every future run.
- View current prestige with the `prestige` command.

---

## Equipment

### Equipment Slots

| Slot | Icon | Description |
|---|---|---|
| Weapon | ⚔ | Offensive weapon — increases Attack |
| Accessory | 💍 | Special bonuses: dodge, mana, immunities |
| Head | 🪖 | Helms, hoods, crowns |
| Shoulders | 🥋 | Pauldrons, mantles |
| Chest | 🛡 | Cuirasses, robes, tunics, chainmail |
| Hands | 🧤 | Gauntlets, gloves, bracers |
| Legs | 👖 | Greaves, leggings, chaps |
| Feet | 👟 | Boots, sabatons, shoes |
| Back | 🧥 | Cloaks, capes |
| Off-Hand | ⛨ | Shields, spell focuses |

**Armor Weight Classes:** Heavy armor (plate) is restricted to Warriors and Paladins. Cloth/Robes are restricted to Mages and Necromancers. Leather armor is restricted to Rogues and Rangers. Universal (medium) armor can be worn by any class.

### Armor Sets

Equip multiple pieces of a named set for powerful bonuses:

| Set | Class | Pieces | 2-Piece Bonus | 4-Piece Bonus |
|---|---|---|---|---|
| Ironclad Set | Warrior / Paladin | Head, Chest, Hands, Legs | +5 DEF | 10% damage reflect |
| Shadowstep Set | Rogue / Ranger | Head, Hands, Legs, Feet | +8% dodge chance | Bleed on every hit |
| Arcane Ascendant Set | Mage / Necromancer | Head, Shoulders, Chest, Legs | +20 max mana | -1 mana cost on abilities |
| Sentinel Set | Any class | Shoulders, Chest, Legs | +8 DEF, +20 HP | Stun immunity |

---

## Enemies

### Regular enemies

29 enemy types spread across 8 dungeon floors, each with a `Lore` field surfaced on inspect. Representative examples:

| Enemy | HP | ATK | DEF | Notable |
|-------|----|-----|-----|---------|
| Goblin | 20 | 8 | 2 | Fast, weak |
| Skeleton | 30 | 12 | 5 | Drops Rusty Sword; undead |
| Troll | 60 | 10 | 8 | Drops Troll Hide armour |
| Dark Knight | 45 | 18 | 12 | Drops Dark Blade + Knight's Armor |
| Goblin Shaman | 25 | 10 | 4 | Can heal nearby enemies |
| Stone Golem | 90 | 8 | 20 | Immune to status effects |
| Wraith | 35 | 18 | 2 | 30% flat dodge chance |
| Vampire Lord | 80 | 16 | 12 | 50% lifesteal on attacks |
| Mimic | 40 | 14 | 8 | Disguised as a chest |
| Chaos Knight | — | — | — | Elite floor 5–8 spawn |
| Crypt Priest | — | — | — | Undead healer; floors 5–8 |
| Frost Wyvern | — | — | — | Frost Breath; floors 6–8 |

### Boss variants (one randomly selected per run)

| Boss | Floor | HP | ATK | DEF | Special |
|------|-------|----|-----|-----|---------|
| Dungeon Boss | 5 | 100 | 22 | 15 | Enrages at ≤40% HP (+50% ATK) |
| Lich King | 5 | 120 | 18 | 5 | Applies Poison on every hit; undead |
| Stone Titan | 5 | 200 | 22 | 15 | Extreme tank |
| Shadow Wraith | 5 | 90 | 25 | 3 | 25% flat dodge chance |
| Vampire Boss | 5 | 110 | 20 | 8 | 30% lifesteal |
| **Archlich Sovereign** | **6** | 180 | 28 | 14 | Phase 2: summons undead; drops LichsCrown |
| **Abyssal Leviathan** | **7** | 220 | 32 | 12 | Aquatic horror; drops DragonScale |
| **Infernal Dragon** | **8** | 250 | 36 | 16 | Flame Breath applies Burn; drops InfernalGreatsword |

---

## Dungeon Systems

### Room types

Rooms have an environmental flavour that affects the description: **Standard**, **Dark**, **Mossy**, **Flooded**, **Scorched**, **Ancient**.

### Special rooms (Phase 6)

Three high-value special room types are generated in floors 5–8:

| Room | Description |
|------|-------------|
| **Forgotten Shrine** | Ancient altar offering blessings or curses with rare rewards |
| **Petrified Library** | Dusty repository with lore scrolls and crafting recipes |
| **Contested Armory** | Abandoned weapon cache with contested elite loot |

### Floor narration

Unique atmospheric narration plays at key floor transitions (5→6, 6→7, 7→8), reflecting the deepening danger of the dungeon's lower levels.

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

Shop rooms contain a **Merchant** (`shop` command). Floor 1–5 stock staples (Health Potions, Mana Potions, Iron Sword, Leather Armor, Elixir of Strength). Floors 6–8 carry a rotating high-tier inventory including Legendary accessories, floor-exclusive weapons and armor, and guaranteed key consumables (`dragonheart-elixir` on floor 6, `bone-flute` and `panacea` on floor 7, and `crown-of-ascension` on floor 8).

### Item tiers and affixes

119 items are available across **five tiers**: Common, Uncommon, Rare, Epic, and **Legendary**.

- **Legendaries** (14 items) carry unique passive effects — e.g. *Aegis of the Immortal* (survive at 1 HP once per combat), *Ironheart Plate* (reflect 25% damage), *Lifedrinker* (vampiric strike on hit).
- **Uncommon+** items have a ~10% chance per affix slot to roll a **Prefix** or **Suffix**, adding bonus stats (e.g. "Keen Iron Sword", "Iron Sword of the Bear").  Available affix bonuses include: ATK/DEF/HP/Mana flat bonuses, dodge/crit chance, HP-on-hit, on-hit Bleed, enemy DEF reduction, holy damage vs undead, block chance, phoenix revive extra charge, and per-turn periodic damage.
- **Equipment sets** (Shadowstalker, Ironclad, Arcanist) grant cumulative bonuses for 2- and 3-piece combinations — checked automatically on equip/unequip.
- Many items carry **Class Restrictions** (e.g. Warrior-only, Mage/Necromancer only).

### Crafting

Use `craft <recipe>` to combine ingredients. Gold is consumed along with the listed items.

| Recipe | Ingredients | Gold | Result |
|--------|------------|------|--------|
| Health Elixir | 2× Health Potion | — | Heals 75 HP |
| Reinforced Sword | 1× Iron Sword | 30g | +8 ATK weapon |
| Reinforced Armor | 1× Leather Armor | 25g | +8 DEF armour |

Armor crafting recipes are now available for all equipment slots (Head, Shoulders, Chest, Hands, Legs, Feet, Back, Off-Hand). Browse the full recipe list via the **Petrified Library** special room or `craft` command in-game.

---

## Display & Colours

Dungnz uses [Spectre.Console](https://spectreconsole.net/) for all rendering — rich panels, tables, progress bars, and arrow-key selection menus. Colour and styling are applied via Spectre markup tags.

**Avalonia Migration (In Progress):** A cross-platform GUI using Avalonia UI is being developed alongside the Spectre terminal UI. Core map rendering logic has been extracted to `MapRenderer` (in `Models/`) to enable sharing between both display implementations. The Avalonia project scaffold exists at `Dungnz.Display.Avalonia/` but is not yet functional.

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
| Item tier | Common=grey / Uncommon=green / Rare=blue / Epic=purple / Legendary=gold |

All console output is routed through `IDisplayService` / `SpectreDisplayService` — colour constants live in `Systems/ColorCodes.cs`. When equipping items, `EquipmentManager` shows stat deltas with green `+` / red `−` indicators.

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

## Phase 8: Grand Expansion — Bosses, Balance, Combat & Narrative

### Floor Bosses

Each floor (1–8) now has a unique named boss with a lore identity and a phase ability that triggers mid-fight:

| Floor | Boss | Phase Ability |
|-------|------|---------------|
| 1 | Goblin Warchief | Calls reinforcements at 50% HP |
| 2 | Plague Hound Alpha | Blood Frenzy (+5 ATK) at 40% HP |
| 3 | Iron Sentinel | Stunning Blow at 60% HP |
| 4 | Bone Archon | Weaken Aura at 50% HP |
| 5 | Crimson Vampire | Blood Drain (MP drain + self-heal) at 25% HP |
| 6 | Archlich Sovereign | Death Shroud at 40% HP |
| 7 | Abyssal Leviathan | Tentacle Barrage at 50% HP |
| 8 | Infernal Dragon | Flame Breath at 60% HP |

### Class Passives

Each class now has a unique passive trait that activates automatically in combat:

| Class | Passive | Effect |
|-------|---------|--------|
| Warrior | Battle Hardened | Gains +2 ATK per 20% HP lost (max +8) |
| Mage | Arcane Surge | Next ability costs 1 less mana after spending mana |
| Rogue | Shadow Strike | First attack each combat deals 2× damage |
| Paladin | Divine Bulwark | Gains Fortified status when HP < 25% (once per combat) |
| Necromancer | Soul Harvest | Heals 5 HP on every kill |
| Ranger | Eagle Eye | +15% dodge chance on turns 1–2 |

### New Status Effects

Three new status effects introduced:

| Effect | Per-turn | Duration | Notes |
|--------|----------|----------|-------|
| **Freeze** | — | 2 turns | Cannot act; breaks on physical damage |
| **Silence** | — | 3 turns | Cannot use abilities |
| **Curse** | — | 4 turns | ATK and DEF reduced by 25% |

### Trap Rooms

10% of dungeon rooms are now trap rooms with interactive choices:

| Trap | Choices | Risk / Reward |
|------|---------|---------------|
| **Arrow Volley** | Shield or sprint through | Sprint risks damage; shield is safe |
| **Poison Gas Vent** | Sprint or find bypass | Sprint risks Poison; bypass costs a turn |
| **Collapsing Floor** | Leap or cross carefully | Leap risks fall damage but may yield loot |

Careful routes are safer; reckless routes offer bonus loot.

### Environmental Hazards

Rooms can now have persistent environmental hazards that affect every action taken inside:

| Hazard | Floors | Spawn Rate | Effect |
|--------|--------|-----------|--------|
| 🔥 Lava Seam | 7–8 | 15% of rooms | 5 fire damage per action |
| 💀 Corrupted Ground | 5–8 | 10% of rooms | 3 HP drain per action |
| ✨ Blessed Clearing | 1–6 | 8% of rooms | +3 HP regen per action (once per visit) |

### Epic Tier Loot

Epic-tier armor items are now reachable through normal dungeon play:

| Floors | Drop Chance |
|--------|-------------|
| 5–6 | ~8% per loot roll |
| 7–8 | ~15% per loot roll |

### Narrative Systems

- **Room descriptions** vary by floor theme: stone dungeon (1–2) → armory (3–4) → undead domain (5–6) → volcanic abyss (7–8).
- **Item flavor text**: pickups, equip/unequip actions, and consumable use all display flavor lines by tier and item type.
- **Combat narration**: ambient lines for combat start, critical hits, killing blows, and near-death urgency.
- **Boss narration**: all 8 bosses have unique intro, phase-trigger, and death lines delivered by `BossNarration`.
- **Shrines**: outcome text is tailored to shrine type (Heal, Bless, Fortify, Meditate).
- **Merchants**: floor-aware greeting lines reflect the dungeon's deepening danger.

### Process Note

Phase 8 introduced **isolated git worktrees** for parallel agent work on the same repository files, eliminating merge conflicts during concurrent development.

---

## Architecture

```
Dungnz/
├── Program.cs                   # Entry point — wires dependencies and starts game
├── Engine/
│   ├── GameLoop.cs              # Main state machine; all command dispatch and win/loss logic
│   ├── CombatEngine.cs          # Turn-based combat, item use in combat, XP award, level-up flow
│   ├── ICombatEngine.cs         # Interface enabling test doubles
│   ├── StubCombatEngine.cs      # Deterministic test stub
│   ├── CommandParser.cs         # Maps raw text input → ParsedCommand (verb + argument)
│   ├── DungeonGenerator.cs      # Procedural grid generation with BFS connectivity check
│   ├── EnemyFactory.cs          # Spawns regular enemies or boss variants by floor
│   └── EnemyTypeRegistry.cs     # Runtime JSON polymorphism for all Enemy subtypes
├── Dungnz.Models/               # Class library: pure domain models, zero external deps
│   └── (Player, Room, Enemy, Item, SkillTree, GameConstants, MerchantInventoryConfig, DefaultItems, ...)
├── Data/
│   ├── enemy-stats.json         # Stats and ASCII art for all 29+ enemy types
│   ├── item-stats.json          # 119 items: stats, tiers, passiveEffectId, setId, classRestriction
│   ├── item-affixes.json        # 10 prefixes + 10 suffixes for Uncommon+ affix rolls
│   ├── crafting-recipes.json    # 15+ crafting recipes with ingredient + gold costs
│   └── merchant-inventory.json  # Per-floor merchant stock (floors 1–8)
├── Display/
│   ├── IDisplayService.cs       # Output abstraction (all console writes go through here)
│   └── DisplayService.cs        # Concrete ANSI console implementation
└── Systems/
    ├── AbilityManager.cs        # Ability registry, cooldown tracking, ability execution
    ├── AchievementSystem.cs     # Run-end achievement evaluation and persistence
    ├── AffixRegistry.cs         # Affix definitions + roll logic (10% on Uncommon+)
    ├── AmbientEvents.cs         # Random flavour events on room entry
    ├── BossNarration.cs         # Boss-specific combat flavour text
    ├── ColorCodes.cs            # ANSI colour constants and helper methods
    ├── CraftingSystem.cs        # Recipe catalogue and TryCraft logic
    ├── EquipmentManager.cs      # Equip/unequip with stat delta application (colour-coded deltas)
    ├── FloorSpawnPools.cs       # Per-floor enemy spawn tables (60/30/10 distribution)
    ├── FloorTransitionNarration.cs # Atmospheric text for floor 5→6, 6→7, 7→8 transitions
    ├── InventoryManager.cs      # Take, use, and item dispatch
    ├── NarrationService.cs      # General combat and exploration flavour text
    ├── PassiveEffectProcessor.cs   # Legendary passive effect handlers (vampiric, reflect, phoenix…)
    ├── PrestigeSystem.cs        # Cross-run win tracking and prestige bonuses
    ├── RoomDescriptions.cs      # Floor-themed room description pools (8 floors × 4 each)
    ├── SaveSystem.cs            # Full game-state serialisation/deserialisation
    ├── SetBonusManager.cs       # 3 equipment sets with 2- and 3-piece bonus detection
    ├── StatusEffectManager.cs   # Per-turn effect ticking and stat modifiers
    └── Enemies/                 # One class per enemy type + BossVariants.cs
```

---

## Testing

**1,289 passing tests** across **68 test files** in `Dungnz.Tests/`.

```bash
dotnet test Dungnz.Tests
```

Coverage includes: `CombatEngine`, `CommandParser`, `CraftingSystem`, `SkillTree`, `PrestigeSystem`, `AchievementSystem`, `StatusEffectManager`, `EquipmentSystem`, `InventoryManager`, `SaveSystem`, `GameLoop` (integration), `DungeonGenerator`, `EnemyFactory`, `LootTable`, `Player`, `DisplayService`, `NarrationService`, `CombatItemUse`, and stub combat engine. Major test suites: Combat variants, enemy factory coverage, integration tests, phase-specific testing, loot distribution, balance simulations, and regression tests.

---

## Contributing

- **XML docs required** on all public members — the project enforces this via existing doc comments.
- **Update README.md** when changing any documented system (commands, enemies, abilities, skills, crafting, saves, achievements). The `readme-check` CI workflow will fail PRs that modify `Engine/`, `Systems/`, `Models/`, or `Data/` without a matching `README.md` change.
- Run `dotnet test Dungnz.Tests` before opening a PR — all tests must pass.
- Follow existing code style: one class per file, `PascalCase` types, `camelCase` locals.
