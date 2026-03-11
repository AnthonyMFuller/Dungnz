# Retrospective — 2026-03-11
**Facilitator:** Coulson
**Participants:** Hill, Barton, Romanoff, Fury, Fitz
**Context:** Post-retro-cycle retrospective — user requested improvement recommendations
**Test suite at time of ceremony:** 1,913 passing, 0 failing

---

## What Went Well

### 1. CHARGED crash fix was handled with the right scope
All five participants acknowledged the `[CHARGED]` → `[[CHARGED]]` fix was more than a one-line patch. The team traced the root cause to unescaped user-facing strings hitting Spectre's parser, swept the entire display service, and delivered adversarial smoke tests alongside the fix. That's a bug fix that prevents a class of future bugs, not just the instance.

### 2. `internal static` extraction pattern proven and reusable
`BuildPlayerStatsPanelMarkup` as an `internal static` method — callable from `PanelHeightRegressionTests` without a live renderer — is now established practice. Hill, Barton, and Romanoff all called it out as the correct architectural answer for display testing. The pattern is proven. It now needs to be applied to the remaining untestable panels.

### 3. PanelHeightRegressionTests is the right model
The 151-line test class gates real line counts in CI. Before it existed, panel overflow was invisible until a player hit it. Multiple members named it as the infrastructure investment of the cycle. It should expand to cover all panels, not just Stats.

### 4. LayoutConstants.cs centralization
Coulson's centralization of panel height constants into a shared file means a layout change is now a one-edit propagation with immediate test signal. Hill noted that the magic number drift is what *caused* the cooldown overflow to go undetected. That root cause is now addressed structurally.

### 5. Content Authoring Spec exists and is substantive
Fury's `docs/content-authoring-spec.md` (416 lines, 9 sections) documents unsafe bracket patterns, valid Spectre color names, panel surfaces with line limits, and a self-validation checklist. It turns tribal knowledge into a spec. Romanoff noted it still needs a gate — but it was the right thing to create.

### 6. CI pipeline held firm
NuGet caching worked silently. Coverage sat at 85.57% (above the 70% floor). The squad-CI and smoke-test.yml both held through the refactor cycles. Zero regressions leaked to master.

---

## What Could Be Improved

### 1. GearPanel is the last untestable major rendering path
All five members flagged this independently. `RenderGearPanel` is ~55 lines of inline markup construction with 10 equipment slots, conditional set bonus text, and tier coloring — the most visually dense panel we have, with zero unit test surface. The `BuildGearPanelMarkup` extraction TODO has been deferred for two cycles. Barton: *"If there's a gear panel overflow bug right now, we would not know until a player hits it."*

### 2. The cooldown overflow is a documented bug, not a deferred feature
`BuildPlayerStatsPanelMarkup` can emit up to 9+ lines against `StatsPanelHeight=8`. The regression tests explicitly exclude the cooldown path to avoid failing. Romanoff: *"A player with an active cooldown sees a broken layout, and CI is green. This is the wrong side of the tradeoff."* Hill: *"Documenting a bug in a constant file is not the same as fixing it."*

### 3. Narration content has no markup safety gate
Fury's Content Authoring Spec warns authors about unescaped brackets. But warnings in documentation don't run. The adversarial display tests catch bad markup fed *to the display layer* — they don't enumerate the 1,775+ lines of narration string content for embedded `[STATUS]`-style patterns. A single `[CHARGED]` in `EnemyNarration.cs` would be a dormant runtime crash waiting for a player to encounter it.

### 4. Architecture enforcement has a disabled rule
Romanoff: The `NotCallMethod` ArchUnitNET check that would catch bare `Console.Write` calls outside `IDisplayService` is commented out with a TODO. 1,913 tests, zero automated enforcement of the architecture rule "no Console I/O in game logic."

### 5. Coverage floor is stale
Fitz: We're running at 85.57% but enforcing 70%. That 15-point buffer means hundreds of lines of untested code can ship before CI objects. The floor is a false safety net at current levels.

### 6. CI double-run overhead
`smoke-test.yml` triggers on both `pull_request` and `push: master` — the project is built three times per PR merge (squad-ci, smoke-test on PR, smoke-test on push). No shared artifact between smoke-test and squad-ci.

### 7. Open bug living in a test comment
Romanoff: `SoulHarvestIntegrationTests` contains an explicit `// THIS IS THE BUG:` comment about a double-heal. That's either an open defect tracked nowhere, or a false alarm never triaged.

---

## 🎯 Top Recommendations (One Per Member)

| Member | Role | Recommendation |
|--------|------|----------------|
| Hill | C# Dev | Extract `BuildGearPanelMarkup` as `internal static` + add per-panel line-count CI assertions for **all** panels in `PanelHeightRegressionTests`, covering all class/state combinations. Every panel gets a named height constant, extracted markup builder, and CI assertion. |
| Barton | Systems/Display | Extract `BuildGearPanelMarkup` as `internal static` mirroring `BuildPlayerStatsPanelMarkup` exactly — then add gear panel line-count assertion to `PanelHeightRegressionTests`. Fix the stats panel line budget (count actual worst-case lines across all classes, not just the happy path). |
| Romanoff | QA Engineer | Add **Verify.Xunit snapshot baselines** for every extractable panel markup method. Any change to panel output produces a reviewable diff a human must approve. This catches the CHARGED class, the overflow class, and every future layout regression — and makes `BuildGearPanelMarkup` extraction a merge gate by enforcement rather than by courtesy. |
| Fury | Content Writer | Add `NarrationMarkupSafetyTests` using reflection to enumerate every string in every narration static class (`EnemyNarration`, `MerchantNarration`, `ShrineNarration`, etc.) and parse each through `new Markup(s)`. Any throw is a test failure with the offending string reported. Turns the Content Authoring Spec from advice into a gate. |
| Fitz | DevOps | Raise the coverage floor from 70% → **80%** in `squad-ci.yml` (single-character change). We're at 85.57% — this doesn't fail any current code — but it eliminates the 15-point buffer that lets large untested features (like the pending GearPanel work) ship without CI objection. |

**Pattern:** Hill and Barton converged independently on the same structural fix (GearPanel extraction). Romanoff proposes making it a merge gate mechanism. Fury targets the content layer equivalent (narration markup safety). Fitz targets the CI enforcement layer. All five recommendations reinforce each other rather than competing.

---

## Action Items

| Owner | Action | Priority |
|-------|--------|----------|
| Hill | Extract `BuildGearPanelMarkup` as `internal static` in `SpectreLayoutDisplayService.cs` | P0 |
| Hill | Fix cooldown panel overflow — raise `StatsPanelHeight` or cap rendering with overflow indicator | P0 |
| Fitz | Raise coverage floor: `Threshold=70` → `Threshold=80` in `squad-ci.yml` | P0 |
| Fitz | Change closes-issue check from `::warning::` to `exit 1` in `squad-ci.yml` | P0 |
| Romanoff | Add Verify.Xunit snapshot baseline for `BuildPlayerStatsPanelMarkup` (already extractable) | P1 |
| Romanoff | Add Verify.Xunit snapshot baseline for `BuildGearPanelMarkup` once extracted | P1 |
| Romanoff | Expand `PanelHeightRegressionTests` to all 5 panels × all class/state combinations | P1 |
| Romanoff | Write `NarrationMarkupSafetyTests` (reflection over all narration static classes, parse each string through `new Markup(s)`) | P1 |
| Romanoff | Triage `SoulHarvestIntegrationTests` double-heal comment — file issue or delete | P1 |
| Fury | Write floor-themed room narration (5 variants per floor tier for cleared + revisited states) | P2 |
| Fury | Write item-specific pickup lines for Legendary/Epic tier items in `ItemInteractionNarration.cs` | P2 |
| Fitz | Fix smoke-test double-run: remove `push: master` trigger or deduplicate with `workflow_run` | P1 |
| Fitz | Add job summary step to `squad-ci.yml` emitting build duration + test count + coverage % | P2 |
| Coulson/Romanoff | Replace disabled `NotCallMethod` TODO with custom xUnit fact scanning assembly for bare `Console.Write/WriteLine` call sites outside approved types | P2 |

---

## Notes

### Unanimous structural diagnosis
All five participants named the same structural gap without prompting: the GearPanel rendering path has no unit test coverage, and it's the most complex panel in the layout. Hill and Barton recommended the same specific fix independently. Romanoff wants to make it a gate. Fury's concern is that untestable rendering means *her* content gets silently eaten by layout bugs with no CI signal. This is convergence, not a majority — the fix is overdue.

### Verify.Xunit as a forcing mechanism
Romanoff's snapshot recommendation is notable because it's orthogonal to the panel height tests already in place. Snapshot tests catch *any* rendering change — not just line count. A content author embedding an unsafe character, a developer changing a color constant, a layout tweak that reflows cooldown display — all produce reviewable diffs rather than silent failures. The forcing mechanism angle (can't snapshot a private method → extraction becomes enforced) is the strongest argument for adopting it.

### Fury's narration markup gap
The distinction Fury drew is important: the adversarial display tests verify that the *renderer* escapes brackets correctly. They don't verify that *content strings themselves* are bracket-safe. With 1,775+ lines of narration content across seven files, the Content Authoring Spec is necessary but not sufficient. The `NarrationMarkupSafetyTests` class closes that gap for approximately 60 lines of test code.

### Fitz's CI efficiency wins
The P0 items (coverage floor + closes-issue gate) are sub-5-minute changes that can ship in a single PR with no coordination. The double-run fix is P1 but addresses real wasted compute on every merge. These are easy wins that compound.

### Coverage floor vs. actual coverage
85.57% actual, 70% enforced. Fitz's point is that the gap creates false confidence. The next large feature (or the GearPanel extraction + tests that need to land together) should not be able to ship with a 20% drop going unnoticed. Floor at 80% catches that with no current false positives.
