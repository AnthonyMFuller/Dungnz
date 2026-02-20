# Dungnz

A classic dungeon-crawling roguelike written in C# — explore procedurally generated dungeons, fight enemies, collect loot, level up, and defeat the boss to escape.

## Gameplay

You navigate a randomised dungeon grid, room by room. Each room may contain enemies to fight, items to loot, or both. Reach the exit room and defeat the Dungeon Boss to win.

- **Win:** Reach the exit room and defeat the Dungeon Boss
- **Lose:** Your HP drops to 0 in combat

**Core loop:** Explore → Fight → Loot → Level Up → Defeat Boss

## Commands

| Command | Aliases | Description |
|---------|---------|-------------|
| `go <direction>` | `north`, `south`, `east`, `west` / `n s e w` | Move to an adjacent room |
| `look` | `l` | Redescribe the current room |
| `examine <target>` | | Inspect an enemy or item |
| `take <item>` | | Pick up an item |
| `use <item>` | | Use a consumable or equip a weapon/armour |
| `inventory` | `inv`, `i` | List carried items |
| `stats` | `status` | View player stats |
| `help` | `?` | Show command list |
| `quit` | `exit`, `q` | Exit the game |

## Architecture

```
Dungnz/
├── Program.cs              # Entry point — bootstraps the game
├── Engine/
│   ├── GameLoop.cs         # Main state machine; command handling and win/loss logic
│   ├── CombatEngine.cs     # Turn-based combat, XP, and levelling
│   ├── ICombatEngine.cs    # Interface for testability
│   ├── StubCombatEngine.cs # Test stub
│   ├── CommandParser.cs    # Parses raw input into ParsedCommand
│   ├── DungeonGenerator.cs # Procedural 5×4 room grid with BFS connectivity check
│   └── EnemyFactory.cs     # Spawns random enemies or the Dungeon Boss
├── Models/                 # Pure data classes (Player, Room, Enemy, Item, enums…)
├── Display/
│   └── DisplayService.cs   # All console output — no raw Console.Write elsewhere
└── Systems/
    ├── InventoryManager.cs # Take / use / equip logic
    └── Enemies/            # Goblin, Skeleton, Troll, DarkKnight, DungeonBoss
```

### Key mechanics

| Mechanic | Detail |
|----------|--------|
| Damage | `Math.Max(1, attacker.Attack - defender.Defense)` |
| Levelling | Every 100 XP = +1 level; each level grants +2 Attack, +1 Defense, +10 Max HP |
| Dungeon size | 5×4 rooms; ~60 % have enemies, ~30 % have items |
| Loot | First-match-wins drop table per enemy type |

## Getting Started

**Prerequisites:** [.NET 9.0 SDK](https://dotnet.microsoft.com/download)

```bash
# Clone
git clone https://github.com/AnthonyMFuller/Dungnz.git
cd Dungnz

# Build & run
dotnet run
```

## Enemies

| Enemy | HP | Attack | Defense | XP |
|-------|----|--------|---------|-----|
| Goblin | 20 | 8 | 2 | 15 |
| Skeleton | 30 | 10 | 4 | 25 |
| Troll | 50 | 14 | 6 | 45 |
| Dark Knight | 65 | 18 | 10 | 70 |
| Dungeon Boss | 100 | 22 | 15 | 100 |

## Known Limitations / Roadmap

- No save/load system — each run starts fresh
- No unit test suite yet (planned for v2)
- `IDisplayService` interface not yet extracted (blocks headless testing)
- `Player` model uses public setters (encapsulation refactor planned for v2)
