# Content Authoring Spec

**For:** Dungnz Content Writers and C# Developers  
**By:** Fury (Narrative) · Barton (Systems/Display)  
**Version:** 1.1  
**Last Updated:** 2025

This guide documents where your content appears in the game UI, how much space it has, and which characters will crash the display. **Read this before writing any flavor text.**

---

## 1. Panel Layout Quick Reference

The game UI consists of 6 panels arranged in a 3-row layout:

```
┌─ Map (60%) ─────────┬─ Stats (40%) ──────┐
│ (Top 20%)           │ (Top 20%)          │
├─ Content (70%) ─────┼─ Gear (30%) ───────┤
│ (Middle 50%)        │ (Middle 50%)       │
├─ Log (70%) ─────────┼─ Input (30%) ──────┤
│ (Bottom 30%)        │ (Bottom 30%)       │
└─────────────────────┴────────────────────┘
```

### Content Surfaces by Panel

| Surface | Panel | Approx Lines | Wraps? | Notes |
|---------|-------|--------------|--------|-------|
| **Room description** | Content | 10–15 | Yes | Multiple lines acceptable; scrolls naturally |
| **Room environmental hazard** | Content | 1–2 | Yes | Descriptive flavor line for room effects (lava, corruption, blessing) |
| **Combat log entry** | Content | 1–2 | Yes | Per action (hit/miss/crit/heal) |
| **Enemy intro** | Content | 1–3 | Yes | Flavor text when enemy enters room (e.g., "A snarling goblin leaps!") |
| **Enemy name** | Gear (combat) | 1 | No | Appears in "⚔ Enemy Name" header above stats |
| **Enemy lore/flavor** | Gear (combat) | 3–8 | Partially | First ~8 lines visible in Gear panel during combat; scrolls if longer |
| **Enemy crit reaction** | Content | 1–2 | Yes | Enemy personality reaction when landing critical hits |
| **Enemy idle taunt** | Content | 1–2 | Yes | Mid-combat banter (displayed periodically) |
| **Enemy desperation line** | Content | 1–2 | Yes | Final stand message when enemy HP < 25% |
| **Item description** | Content | 3–5 | Yes | Shown in shop menu or pickup context |
| **Item flavor (pickup)** | Content | 1–2 | Yes | Optional atmospheric message when picking up item |
| **Skill name** | Stats | 1 | No | Ability/spell name — keep concise (e.g., "Fireball", "Healing Touch") |
| **Skill description** | Content | 2–4 | Yes | Flavor text explaining what ability does |
| **Status effect label** | Stats / Gear | 1 | No | **MUST escape brackets** (see section 3) |
| **Merchant greeting** | Content | 1 | Yes | Personality-driven opening line (e.g., "Coin spends the same, down here.") |
| **Merchant farewell** | Content | 1 | Yes | Closing line when exiting shop |
| **Shrine description** | Content | 2–5 | Yes | Atmospheric introduction to shrine (e.g., "Ancient power hums in the air.") |
| **Shrine grant message** | Content | 1–2 | Yes | Flavor when player receives blessing |
| **Shrine deny message** | Content | 1–2 | Yes | Flavor when shrine has no blessing to grant |
| **Floor transition** | Content | 3–5 | Yes | Atmospheric text when descending to next floor |
| **Boss intro** | Content | 2–4 | Yes | Dramatic introduction to final encounter |

---

## 2. Panel Dimensions & Hard Limits

All height constants are defined in `Dungnz.Display/Spectre/LayoutConstants.cs` relative to a 40-row baseline terminal. Width is calculated from proportional ratios applied to the terminal width.

### Height constants (from `LayoutConstants.cs`)

| Constant | Value | Panel |
|----------|-------|-------|
| `LayoutConstants.BaselineTerminalHeight` | 40 rows | baseline terminal |
| `LayoutConstants.StatsPanelHeight` | 8 rows | Stats (TopRow, 40 % width) |
| `LayoutConstants.MapPanelHeight` | 8 rows | Map (TopRow, 60 % width) |
| `LayoutConstants.ContentPanelHeight` | 20 rows | Content (MiddleRow, 70 % width) |
| `LayoutConstants.GearPanelHeight` | 20 rows | Gear (MiddleRow, 30 % width) |
| `LayoutConstants.LogPanelHeight` | 8 rows | Log (BottomRow) |

### Width ratios

The layout splits columns proportionally:

| Row band | Left panel | Right panel |
|----------|------------|-------------|
| TopRow | Map — **60 %** terminal width | Stats — **40 %** terminal width |
| MiddleRow | Content — **70 %** terminal width | Gear — **30 %** terminal width |
| BottomRow | Log spans full width | — |

### Derived character widths

| Panel | 80-col terminal | 120-col terminal |
|-------|----------------|-----------------|
| Content (70 %) | ~56 chars | ~84 chars |
| Gear (30 %) | ~24 chars | ~36 chars |
| Map (60 %) | ~48 chars | ~72 chars |
| Stats (40 %) | ~32 chars | ~48 chars |

### In-memory buffer limits (from `SpectreLayoutDisplayService.cs`)

| Constant | Value | Meaning |
|----------|-------|---------|
| `MaxContentLines` | 50 | Content panel ring buffer — oldest lines dropped beyond 50 |
| `MaxLogHistory` | 50 | Log ring buffer |
| `MaxDisplayedLog` | 12 | Visible log lines rendered at any one time |

### Content Panel (Middle-Left)
- **Width:** ~70 % of terminal width (~56 chars at 80-col)
- **Height:** `ContentPanelHeight` = 20 rows (baseline)
- **Behavior:** Text wraps and scrolls naturally
- **Use for:** Room descriptions, combat log, item descriptions, dialog, floor transitions, enemy intros

### Gear Panel (Middle-Right, Combat Mode)
- **Width:** ~30 % of terminal width (~24 chars at 80-col; borders consume ~4, leaving ~20 usable)
- **Height:** `GearPanelHeight` = 20 rows (baseline)
- **Behavior:** Displays enemy name, level, stats, and lore in a fixed layout
- **Enemy Lore Line Limit:** ~8 lines visible before scrolling (if longer, only first 8 show without explicit scroll)
- **Use for:** Enemy stats + lore during combat; equipped gear outside combat

### Stats Panel (Top-Right)
- **Width:** ~40 % of terminal width (~32 chars at 80-col)
- **Height:** `StatsPanelHeight` = 8 rows (baseline)
- **Behavior:** Compact display, limited room
- **Use for:** Player HP, MP, XP bars, status effects (abbreviated)

### Log Panel (Bottom-Left)
- **Width:** ~70 % of terminal width (~56 chars at 80-col)
- **Height:** `LogPanelHeight` = 8 rows (baseline); `MaxDisplayedLog = 12` lines visible
- **Behavior:** Scrolling history of recent actions
- **Use for:** Recent combat actions, item pickups, shrine messages (optional)

### Map Panel (Top-Left)
- **Width:** ~60 % of terminal width (~48 chars at 80-col)
- **Height:** `MapPanelHeight` = 8 rows (baseline)
- **Behavior:** ASCII dungeon map
- **Use for:** Procedurally generated dungeon layout (not for written content)

### Input Panel (Bottom-Right)
- **Width:** ~30 % of terminal width
- **Height:** ~4 rows (BottomRow = 30 % of 40 = 12 rows; Input = 30 % of that ≈ 4)
- **Behavior:** Command prompt
- **Use for:** Command entry only (not content)

---

## 3. Unsafe Characters & Escaping Rules

Spectre.Console interprets `[word]` as markup tags. These are problematic:

### ⚠️ UNSAFE: ALL_CAPS bracketed words

The following will throw `InvalidOperationException` if unescaped:
```
[CHARGED]    [STUNNED]    [BURNING]    [POISONED]    [FROZEN]
[STUNNED]    [WEAKENED]   [BLEEDING]   [SILENCED]
```

**Escape rule:** Double the brackets:
```
[[CHARGED]]     renders as     [CHARGED]
[[STUNNED]]     renders as     [STUNNED]
```

### ⚠️ UNSAFE: CamelCase bracketed words

Any `[PascalCaseWord]` or `[camelCaseWord]` that isn't a valid Spectre color will crash:
```
❌ "You are [Stunned] for 2 turns"     → InvalidOperationException
❌ "The [DarkKnight] roars!"            → InvalidOperationException
```

**Fix:** Escape with double brackets:
```
✅ "You are [[Stunned]] for 2 turns"
✅ "The [[DarkKnight]] roars!"
```

### ⚠️ UNSAFE: Invalid color names

Spectre recognizes specific color tags. Random names will crash:
```
❌ "[crimson]Blood seeps from the wound[/]"     → InvalidOperationException (use [red] instead)
❌ "[darkgreen]The moss spreads[/]"              → InvalidOperationException (use [green] instead)
```

### ✅ SAFE: Valid Spectre Colors

These colors are **safe to use** in markup:
```
[red]           [green]         [blue]          [yellow]
[cyan]          [magenta]       [grey]          [white]
[black]         [gold1]         [bright_red]    [bright_green]
[bold]          [dim]           [italic]        [underline]
```

**Examples:**
```csharp
"[red]The flame consumes everything![/]"                       // ✅ Safe
"[[CHARGED]] electricity crackles around you"                 // ✅ Safe (escaped status)
"The [green]grass[/] glows with eerie light"                  // ✅ Safe
"The [[DarkKnight]] emerges from shadow"                      // ✅ Safe (escaped enemy name)
```

### Rule of Thumb

**If it's all caps or mixed case inside brackets, escape it with double brackets:**
```
[word]        → SAFE if it's a valid color or markup tag
[WORD]        → ESCAPE: [[WORD]]
[WordWord]    → ESCAPE: [[WordWord]]
[word_WORD]   → ESCAPE: [[word_WORD]]
```

---

## 3a. Dynamic Content — `Markup.Escape()` Rule (Developers)

> **This section is for C# developers** writing display code. Content writers do not call `Markup.Escape()` directly, but they should understand why it is required.

### The problem

Spectre.Console's `new Markup(string)` constructor parses **all** `[word]` sequences as colour/style tags. If a player names their character `[red]` or `[DarkKnight]`, any string that embeds it raw will either crash with `MarkupException` or render with unintended colours.

### The rule — all runtime values MUST be escaped

**Every value that comes from runtime state** (player name, enemy name, item name, room description, ability name, status effect name, any user input) **must be wrapped with `Markup.Escape()`** before being inserted into a markup string.

```csharp
// ❌ WRONG — player named "[red]" turns the rest of the panel red
$"Welcome, {player.Name}!"
$"[bold]{player.Name}[/] has arrived."
$"Enemy: {enemy.Name}"
$"[{tc}]{item.Name}[/]"

// ✅ CORRECT — safe for any player/enemy/item name
$"Welcome, {Markup.Escape(player.Name)}!"
$"[bold]{Markup.Escape(player.Name)}[/] has arrived."
$"[red]Enemy: {Markup.Escape(enemy.Name)}[/]"
$"[{tc}]{Markup.Escape(item.Name)}[/]"
```

### Fields that ALWAYS require `Markup.Escape()`

| Expression | Escaped form |
|------------|-------------|
| `player.Name` | `Markup.Escape(player.Name)` |
| `player.Class.ToString()` | `Markup.Escape(player.Class.ToString())` |
| `enemy.Name` | `Markup.Escape(enemy.Name)` |
| `item.Name` | `Markup.Escape(item.Name)` |
| `room.Description` | `Markup.Escape(room.Description)` |
| `effect.Effect.ToString()` | `Markup.Escape(effect.Effect.ToString())` |
| `killedBy` (game-over string) | `Markup.Escape(killedBy)` |
| Any narration string from `NarrationService` | `Markup.Escape(narration)` |
| Recipe / ability / set-bonus names | `Markup.Escape(name)` |

### Alternative — `new Text()` bypasses the parser entirely

When building complex layouts, use `new Text(value)` for plain values alongside `new Markup("...")` for styled labels:

```csharp
// ✅ Text() never parses markup — safe for any player name
var row = new Columns(
    new Markup("[bold red]Enemy:[/]"),
    new Text(enemy.Name)
);

// ✅ Victory banner — escape the name, markup the decoration
sb.AppendLine($"[bold gold1]✦  V I C T O R Y  ✦[/]");
sb.AppendLine($"[bold]{Markup.Escape(player.Name)}[/]  •  Level {player.Level}");
```

### `AppendLog` takes plain text — do NOT add markup

The `AppendLog(string message, string type)` method in `SpectreLayoutDisplayService` **automatically escapes** its input and wraps it with the correct icon and colour for the log type. Passing markup into it will render as literal tag text.

```csharp
// ❌ WRONG — renders as literal "[bold]Critical hit![/]" in the log
AppendLog("[bold]Critical hit![/]");

// ✅ CORRECT — the service applies icon + colour automatically
AppendLog("Critical hit!");
AppendLog($"Picked up {item.Name}", "loot");   // plain text; service escapes item.Name internally
```

---

## 3b. Narration Strings — Plain Text Only

All strings in `NarrationService.cs` pools (room entry, combat, pickup, etc.) are **plain text**. They are passed through `Markup.Escape()` by the display service before rendering. Do not add Spectre tags inside narration pool strings.

```csharp
// ❌ WRONG — markup tag inside narration pool
private static readonly string[] _openingAttackPool = [
    "You swing [bold]furiously[/] at the enemy.",   // BAD — renders as literal "[bold]..."
];

// ✅ CORRECT — plain prose, zero markup tags
private static readonly string[] _openingAttackPool = [
    "You swing furiously at the enemy.",
    "Your blade finds a gap in their guard.",
];
```

### Multi-line narration — use `\n`, not multiple calls

```csharp
// ❌ WRONG — three separate calls cause panel flicker and inconsistent timing
AppendContent("The dungeon trembles.");
AppendContent("Dust falls from the ceiling.");
AppendContent("Something has awakened.");

// ✅ CORRECT — one atomic content update
AppendContent("The dungeon trembles.\nDust falls from the ceiling.\nSomething has awakened.");
```

### Narration length limits

| Context | Max recommended length | Notes |
|---------|----------------------|-------|
| Single combat narration line | **70 chars** | Prevents wrap on 80-col terminal |
| Room entry / atmospheric line | **70 chars** | Same constraint |
| Enemy taunt / idle line | **80 chars** | Content panel has room to wrap once |
| Log message | **55 chars** | Log panel is ~70 % of bottom row width |
| Gear slot item description | **20 chars** | Gear panel ≈ 24 chars; borders eat ~4 |

---

## 4. Content Self-Validation Checklist

### For content writers

Before submitting content for code integration, verify:

- [ ] **Line limit respected?** Checked "Approx Lines" column in section 1 for my content surface
- [ ] **Panel width?** If writing long strings, tested wrapping at ~70 chars for wide panels, ~25 chars for Gear panel
- [ ] **No unsafe brackets?** All all-caps bracketed words escaped: `[[WORD]]` not `[WORD]`
- [ ] **No enemy/status names in brackets?** E.g., `[[Stunned]]` not `[Stunned]`; `[[DarkKnight]]` not `[DarkKnight]`
- [ ] **Only safe colors used?** If using Spectre markup, colors are from the SAFE list (red, green, blue, bold, etc.)
- [ ] **Tone & style consistent?** Reviewed similar content in codebase for matching voice
- [ ] **No trailing whitespace?** Cleaned up line endings
- [ ] **Plural/singular correct?** Checked grammar for placeholder text
- [ ] **No markup tags in narration strings?** Narration pool strings are plain text — no `[bold]`, `[red]`, etc.
- [ ] **Multi-line uses `\n`?** Not multiple separate `AppendContent` calls

### For developers adding display code

- [ ] All `player.Name`, `enemy.Name`, `item.Name`, `room.Description` calls wrapped in `Markup.Escape()`
- [ ] Any literal `[` or `]` in a non-markup string is doubled to `[[` / `]]`
- [ ] `AppendLog()` called with plain text (it escapes and styles internally)
- [ ] Map symbols use `[[S]]`, `[[B]]`, `[[?]]` etc. — never bare `[S]`
- [ ] New emoji use the EAW=N set **or** use the `EL()` helper for aligned rows (see section 3c)
- [ ] No tier/rarity string baked into `item.Name` — tier shown via colour only

---

## 5. Example: Enemy Entry (Correct vs Incorrect)

### ❌ INCORRECT: Unsafe Brackets & Tone Issues

```csharp
["Dark Sorcerer"] = new[]
{
    "The [DarkSorcerer] arrives, crackling with [ARCANE] power!",
    "A shadow in robes, the [DarkSorcerer] casts a [SILENCED] spell on you!",
    "The powerful [DarkSorcerer] laughs as your attacks bounce off its magic shield."
};
```

**Problems:**
1. `[DarkSorcerer]` → InvalidOperationException (not a valid color tag)
2. `[ARCANE]` → InvalidOperationException (all-caps, not a valid tag)
3. `[SILENCED]` → InvalidOperationException (status effect name, not escaped)
4. Tone is mechanical ("powerful", "laughs") — lacks enemy personality

### ✅ CORRECT: Escaped & Thematic

```csharp
["Dark Sorcerer"] = new[]
{
    "A [[DarkSorcerer]] arrives, crackling with arcane force!",
    "Shadow and void—the [[DarkSorcerer]] weaves a silent incantation at you.",
    "The [[DarkSorcerer]] gestures, and reality bends to its will."
};
```

**Fixed:**
1. `[[DarkSorcerer]]` → renders as `[DarkSorcerer]` (escaped)
2. "arcane force" → descriptive, not bracketed
3. `[[DarkSorcerer]]` → properly escaped
4. Tone: clinical, otherworldly, personality-driven ("weaves", "bends reality")

---

## 6. Content Surface Details by Feature

### Enemy Encounters

**Intro line** (when enemy enters room):
- Location: Content panel
- Line limit: 1–3 lines
- Pattern: Enemy type + dramatic action verb
- Example: `"A snarling goblin leaps from the shadows, blade glinting!"`

**Lore field** (displayed in Gear panel during combat):
- Location: Gear panel (combat mode)
- Line limit: 3–8 lines (first 8 visible)
- Pattern: 1–3 sentence flavor text describing enemy archetype
- Tone: Should match enemy personality (see Fury's learnings for archetype notes)
- Example: `"A cursed warrior, bound to eternal conflict. Regenerates wounds that would fell mortals. Its rage is hunger—hunger for blood and souls."`

**Crit reaction** (when enemy lands critical hit on player):
- Location: Content panel
- Line limit: 1–2 lines
- Pattern: Enemy-specific personality response to successful critical strike
- Tone: Arrogant, threatening, or unsettling depending on enemy type
- Example (Dark Knight): `"Pathetic. I've cleaved kingdoms apart."`

**Idle taunt** (periodic mid-combat banter):
- Location: Content panel
- Line limit: 1–2 lines
- Pattern: Enemy mocking or threatening the player
- Trigger: Every 3–4 turns when no special action is taken
- Example (Vampire Lord): `"Your terror is exquisite. It seasons your blood."`

**Desperation line** (when enemy HP < 25%):
- Location: Content panel
- Line limit: 1–2 lines
- Pattern: Enemy final stand or desperate last act
- Tone: Varies (cornered fury, eerie resolve, panicked screams)
- Example (Lich King): `"I have ruled eons. I will NOT fall to you!"`

**Death/defeat line**:
- Location: Content panel
- Line limit: 1–2 lines
- Pattern: Final moment or end state (varies by enemy type)
- Tone: Should NOT be overly comical or diminishing
- Example (Troll): `"The massive troll slumps, its regeneration finally overwhelmed."`

### Merchant Encounters

**Greeting** (when entering shop):
- Location: Content panel
- Line limit: 1 line
- Personality: Gruff, warm-but-grudging, impatient, paranoid, methodical, or short-tempered
- Example: `"Coin spends the same, even down here."`

**Farewell** (when exiting shop):
- Location: Content panel
- Line limit: 1 line
- Personality: Should echo or complement the greeting
- Example: `"Don't spend it all in one dungeon."`

### Shrine Encounters

**Description** (when entering shrine):
- Location: Content panel
- Line limit: 2–5 lines
- Tone: Awe, ancient power, divine ambiguity
- Example: `"You stand before the altar. Blessing or bargain—what's the difference anymore?"`

**Grant message** (when shrine bestows a blessing):
- Location: Content panel
- Line limit: 1–2 lines
- Tone: Dignified, patient, mysterious (NOT harsh or judgmental)
- Example: `"Ancient power flows through you. The shrine pulses softly, its gift accepted."`

**Deny message** (when shrine has nothing to grant):
- Location: Content panel
- Line limit: 1–2 lines
- Tone: Indifferent, patient (shrine shouldn't make player feel bad for asking)
- Example: `"The shrine remains still. Miracles are not infinite."`

### Room Entry Narration

**First visit** (entering a room for the first time):
- Location: Content panel
- Line limit: 1–2 lines
- Pattern: Sensory description + dread
- Example: `"You step into shadow-drenched stone. The air tastes of rust and old death."`

**Active enemies**:
- Location: Content panel
- Line limit: 1–2 lines
- Pattern: Immediate danger, movement in dark
- Example: `"Eyes gleam from the shadows. They've been waiting."`

**Cleared room**:
- Location: Content panel
- Line limit: 1–2 lines
- Pattern: Victory, but caution remains
- Example: `"Silence settles over broken bodies. You've earned a breath."`

### Floor Transitions

**Floor descent** (when moving to next floor):
- Location: Content panel
- Line limit: 3–5 lines
- Pattern: Builds dread, atmospheric, ends with floor number
- Tone: Danger increases as player descends
- Example (Floor 1):
  ```
  "You push deeper into shadow. Stone walls close in.
  The air grows colder.
  Something vast stirs in the darkness.
  Floor 1. The dungeon begins here."
  ```

---

## 7. Safe Formatting Examples

### Status Effects

Status effect names **must** be escaped when displayed as labels:

```csharp
// ❌ WRONG
"[CHARGED] electricity crackles around you"     → CRASH

// ✅ CORRECT
"[[CHARGED]] electricity crackles around you"   → displays: [CHARGED] electricity...
```

### Enemy/NPC Names in Flavor Text

Enemy and NPC names **must** be escaped inside brackets:

```csharp
// ❌ WRONG
"The [DarkKnight] raises its sword"     → CRASH

// ✅ CORRECT
"The [[DarkKnight]] raises its sword"   → displays: The [DarkKnight] raises...
```

### Color Markup

Valid colors are safe; invalid ones will crash:

```csharp
// ❌ WRONG
"[crimson]Blood flows[/]"     → CRASH (crimson not valid)

// ✅ CORRECT
"[red]Blood flows[/]"         → displays in red
"[bold red]ROAR![/]"          → displays bold red
```

---

## 7b. Emoji Width Rules (Developers)

Spectre.Console uses terminal column widths for alignment. Emoji have two widths depending on their Unicode East Asian Width (EAW) property:

| EAW class | Width | Examples | Trailing padding needed |
|-----------|-------|----------|------------------------|
| EAW=N (Narrow) | 1 column | `⚔ ⚗ ☠ ★ ↩ •` | 2 spaces |
| EAW=W (Wide) | 2 columns | `🔥 💧 🐉 📜 🎒 🦺 🪖` | 1 space |

Mixing EAW=W and EAW=N in aligned text (e.g., gear slot list) causes column drift. The codebase uses the `EL(emoji, text)` helper to compensate:

```csharp
// EL adds correct spacing based on whether emoji is in NarrowEmoji set
// NarrowEmoji set: { "⚔", "⚗", "☠", "★", "↩", "•" }
// Wide emoji → 1 trailing space; Narrow emoji → 2 trailing spaces
EL("⚔",  "Weapon")    // "⚔  Weapon" — 2 spaces for narrow
EL("🦺", "Chest")     // "🦺 Chest"  — 1 space for wide
```

**Rule:** Use the `EL()` helper for any new aligned list rows that mix emoji and text labels.

## 7c. Item Tier — Display via Colour, Not Name

Item tier/rarity is shown via Spectre colour markup around the item name. **Do not embed tier text in `item.Name`.**

| Tier | Colour in markup |
|------|----------------|
| Common | `white` |
| Uncommon | `green` |
| Rare | `blue` |
| Epic | `purple` |
| Legendary | `gold1` |

```csharp
// ❌ Wrong — tier baked into name string
item.Name = "Mystic Blade [Epic]";
$"{item.Name}"   // pollutes save files, breaks ItemNames constants

// ✅ Correct — clean name; colour signals tier
item.Name = "Mystic Blade";
$"[purple]{Markup.Escape(item.Name)}[/]"   // Epic
$"[gold1]{Markup.Escape(item.Name)}[/]"    // Legendary
```

---

## 8. Integration Points for Developers

Content is referenced from these locations in code:

| Feature | Code Location | Notes |
|---------|---------------|-------|
| Room entry narration | `Dungnz.Systems/NarrationService.cs` | `GetRoomEntryNarration(RoomNarrationState)` |
| Combat phase narration | `Dungnz.Systems/NarrationService.cs` | `GetPhaseAwareAttackNarration(turn, playerPct, enemyPct)` |
| Legendary/Epic pickup flavour | `Dungnz.Systems/NarrationService.cs` | `_legendaryPickupPool`, `_epicPickupPool` |
| Enemy crit / taunt / desperation | `Dungnz.Systems/NarrationService.cs` | `GetEnemyCritReaction`, `GetEnemyIdleTaunt`, `GetEnemyDesperationLine` |
| Enemy lore (data-driven) | `Dungnz.Data/enemy-stats.json` | `.Lore` field per enemy entry |
| Item descriptions (data-driven) | `Dungnz.Data/item-stats.json` | `.Description` field per item |
| Status effects (data-driven) | `Dungnz.Data/status-effects.json` | Effect display names and descriptions |
| UI panel rendering | `Dungnz.Display/Spectre/SpectreLayoutDisplayService.cs` | All markup construction, emoji icons, `Markup.Escape()` sites |
| Layout constants | `Dungnz.Display/Spectre/LayoutConstants.cs` | `StatsPanelHeight`, `ContentPanelHeight`, etc. |

> **Note:** `EnemyNarration.cs`, `MerchantNarration.cs`, `ShrineNarration.cs`, and `FloorTransitionNarration.cs` are planned but not yet present. Those strings currently live in `NarrationService.cs` or inline in the display service.

---

## 9. Common Pitfalls

| Pitfall | Example | Fix |
|---------|---------|-----|
| Forgetting to escape brackets | `"[STUNNED] poison takes hold"` | `"[[STUNNED]] poison takes hold"` |
| Using invalid color names | `"[crimson]Blood!"` | Use valid colors: `[red]`, `[green]`, etc. |
| Mixing enemy name in brackets | `"The [Goblin] shrieks!"` | `"The [[Goblin]] shrieks!"` |
| Text too long for panel | 25 lines in Gear panel | Keep Gear lore to ~8 lines |
| Tone inconsistent with enemy archetype | Goblins being sophisticated | Review archetype learnings (see Fury history) |
| Trailing whitespace | Extra spaces at line end | Clean before submission |
| Forgetting closing markup tag | `"[red]Blood flows"` (no `[/]`) | Always close: `"[red]Blood flows[/]"` |
| Raw player/enemy/item name in markup | `$"Welcome {player.Name}!"` | `$"Welcome {Markup.Escape(player.Name)}!"` |
| Markup tags inside narration pool strings | `"You strike [bold]hard[/]."` | Plain text only: `"You strike hard."` |
| Multiple `AppendContent` calls for one beat | Three separate calls with flicker | One call with `\n` separators |
| Tier text baked into item name | `item.Name = "Blade [Epic]"` | Clean name; use colour markup for tier |
| `AppendLog` with markup | `AppendLog("[bold]Crit![/]")` | `AppendLog("Crit!")` — plain text |

---

## 10. Questions?

Refer back to **Section 1** for panel sizes, **Section 3** for bracket escaping rules, **Section 3a** for `Markup.Escape()` rules, **Section 3b** for narration plain-text rules, and **Section 6** for content surface details. If adding a new content surface, coordinate with Coulson (lead architect) and Hill (display layer owner) to determine panel allocation and line limits.

---

**End of Content Authoring Spec**
