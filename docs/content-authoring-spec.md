# Content Authoring Spec

**For:** Dungnz Content Writers  
**By:** Fury (Narrative Specialist)  
**Version:** 1.0  
**Last Updated:** 2026-03-11

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

### Content Panel (Middle-Left)
- **Width:** ~70 characters (depending on terminal width)
- **Height:** ~20 lines  
- **Behavior:** Text wraps and scrolls naturally
- **Use for:** Room descriptions, combat log, item descriptions, dialog, floor transitions, enemy intros

### Gear Panel (Middle-Right, Combat Mode)
- **Width:** ~25–30 characters  
- **Height:** ~20 lines  
- **Behavior:** Displays enemy name, level, stats, and lore in a fixed layout
- **Enemy Lore Line Limit:** ~8 lines visible before scrolling (if longer, only first 8 show without explicit scroll)
- **Use for:** Enemy stats + lore during combat; equipped gear outside combat

### Stats Panel (Top-Right)
- **Width:** ~25–30 characters  
- **Height:** ~8 lines  
- **Behavior:** Compact display, limited room
- **Use for:** Player HP, MP, XP bars, status effects (abbreviated)

### Log Panel (Bottom-Left)
- **Width:** ~70 characters  
- **Height:** ~8 lines  
- **Behavior:** Scrolling history of recent actions
- **Use for:** Recent combat actions, item pickups, shrine messages (optional)

### Map Panel (Top-Left)
- **Width:** ~70 characters  
- **Height:** ~5 lines  
- **Behavior:** ASCII dungeon map
- **Use for:** Procedurally generated dungeon layout (not for written content)

### Input Panel (Bottom-Right)
- **Width:** ~25–30 characters  
- **Height:** ~4 lines  
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

## 4. Content Self-Validation Checklist

Before submitting content for code integration, verify:

- [ ] **Line limit respected?** Checked "Approx Lines" column in section 1 for my content surface
- [ ] **Panel width?** If writing long strings, tested wrapping at ~70 chars for wide panels, ~25 chars for Gear panel
- [ ] **No unsafe brackets?** All all-caps bracketed words escaped: `[[WORD]]` not `[WORD]`
- [ ] **No enemy/status names in brackets?** E.g., `[[Stunned]]` not `[Stunned]`; `[[DarkKnight]]` not `[DarkKnight]`
- [ ] **Only safe colors used?** If using Spectre markup, colors are from the SAFE list (red, green, blue, bold, etc.)
- [ ] **Tone & style consistent?** Reviewed similar content in codebase for matching voice
- [ ] **No trailing whitespace?** Cleaned up line endings
- [ ] **Plural/singular correct?** Checked grammar for placeholder text

---

## 5. Example: Enemy Entry (Correct vs Incorrect)

### ❌ INCORRECT: Unsafe Brackets & Tone Issues

\`\`\`csharp
["Dark Sorcerer"] = new[]
{
    "The [DarkSorcerer] arrives, crackling with [ARCANE] power!",
    "A shadow in robes, the [DarkSorcerer] casts a [SILENCED] spell on you!",
    "The powerful [DarkSorcerer] laughs as your attacks bounce off its magic shield."
};
\`\`\`

**Problems:**
1. `[DarkSorcerer]` → InvalidOperationException (not a valid color tag)
2. `[ARCANE]` → InvalidOperationException (all-caps, not a valid tag)
3. `[SILENCED]` → InvalidOperationException (status effect name, not escaped)
4. Tone is mechanical ("powerful", "laughs") — lacks enemy personality

### ✅ CORRECT: Escaped & Thematic

\`\`\`csharp
["Dark Sorcerer"] = new[]
{
    "A [[DarkSorcerer]] arrives, crackling with arcane force!",
    "Shadow and void—the [[DarkSorcerer]] weaves a silent incantation at you.",
    "The [[DarkSorcerer]] gestures, and reality bends to its will."
};
\`\`\`

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
  You push deeper into shadow. Stone walls close in.
  The air grows colder.
  Something vast stirs in the darkness.
  Floor 1. The dungeon begins here.
  ```

---

## 7. Safe Formatting Examples

### Status Effects

Status effect names **must** be escaped when displayed as labels:

\`\`\`csharp
// ❌ WRONG
"[CHARGED] electricity crackles around you"     → CRASH

// ✅ CORRECT
"[[CHARGED]] electricity crackles around you"   → displays: [CHARGED] electricity...
\`\`\`

### Enemy/NPC Names in Flavor Text

Enemy and NPC names **must** be escaped inside brackets:

\`\`\`csharp
// ❌ WRONG
"The [DarkKnight] raises its sword"     → CRASH

// ✅ CORRECT
"The [[DarkKnight]] raises its sword"   → displays: The [DarkKnight] raises...
\`\`\`

### Color Markup

Valid colors are safe; invalid ones will crash:

\`\`\`csharp
// ❌ WRONG
"[crimson]Blood flows[/]"     → CRASH (crimson not valid)

// ✅ CORRECT
"[red]Blood flows[/]"         → displays in red
"[bold red]ROAR![/]"          → displays bold red
\`\`\`

---

## 8. Integration Points for Coders

Content is referenced from these locations in code:

| Feature | Code Location | Access Pattern |
|---------|---------------|-----------------|
| Enemy intros | `Dungnz.Systems/EnemyNarration.cs` | `EnemyNarration.GetIntro(enemyName)` |
| Enemy lore | `Data/enemy-stats.json` | `.Lore` field per enemy |
| Enemy crits | `Dungnz.Systems/EnemyNarration.cs` | `EnemyNarration.GetCritReactions(enemyName)` |
| Enemy idle taunts | `Dungnz.Systems/EnemyNarration.cs` | `EnemyNarration.GetIdleTaunts(enemyName)` |
| Enemy desperation | `Dungnz.Systems/EnemyNarration.cs` | `EnemyNarration.GetDesperationLines(enemyName)` |
| Enemy death | `Dungnz.Systems/EnemyNarration.cs` | `EnemyNarration.GetDeath(enemyName)` |
| Room entry | `Dungnz.Systems/NarrationService.cs` | `GetRoomEntryNarration(RoomNarrationState)` |
| Combat narration | `Dungnz.Systems/NarrationService.cs` | `GetPhaseAwareAttackNarration(...)` |
| Merchant | `Dungnz.Systems/MerchantNarration.cs` | Merchant greeting/farewell pools |
| Shrine | `Dungnz.Systems/ShrineNarration.cs` | Shrine description/grant/deny pools |
| Floor transitions | `Dungnz.Systems/FloorTransitionNarration.cs` | Floor-specific pools |
| Items | `Data/item-stats.json` | `.Description` field per item |

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

---

## 10. Questions?

Refer back to **Section 1** for panel sizes, **Section 3** for bracket escaping rules, and **Section 6** for content surface details. If adding a new content surface, coordinate with Coulson (lead architect) and Hill (display layer owner) to determine panel allocation and line limits.

---

**End of Content Authoring Spec**
