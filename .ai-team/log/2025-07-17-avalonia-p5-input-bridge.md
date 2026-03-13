# Session: 2025-07-17 — Avalonia P5 Input Bridge

**Requested by:** Boss  
**Team:** Hill, Scribe  

---

## What They Did

### Hill — Avalonia P5 Implementation

Implemented the input bridge for Avalonia Phase 5, enabling the game thread to receive typed player commands from the Avalonia UI. Key changes:

- **`AvaloniaDisplayService.cs`** — Added `ReadCommandInput()` using `TaskCompletionSource<string?>` with `RunContinuationsAsynchronously` to bridge the synchronous game thread to the async Avalonia UI thread. Subscribes/unsubscribes to `InputSubmitted` per-call.
- **`AvaloniaInputReader.cs`** (new) — Created a new `IInputReader` implementation for Avalonia that uses the same TCS pattern in `ReadLine()`. Subscribes to `InputSubmitted` persistently in the constructor.
- **`InputPanelViewModel.cs`** — Added `InputSubmitted` event and `Submit()` command that fires it, plus `IsInputEnabled` property for toggling the TextBox.
- **`InputPanel.axaml` / `InputPanel.axaml.cs`** — Added TextBox with Enter key binding and auto-focus behavior via code-behind.
- **`App.axaml.cs`** — Minor wiring adjustments for the input reader.

Commits:
- `aba100b` — feat(avalonia): implement ReadCommandInput with TCS pattern (P5)
- `9e43525` — docs: add P5 decision doc and update Hill history

### Scribe — Decision Merge

Merged the inbox decision file `hill-avalonia-p5-input.md` into `.ai-team/decisions/decisions.md`. Decision documents the TCS-based input bridge architecture, rationale for avoiding async/await engine rewrite, and safety of dual-consumer single-event pattern.

---

## Key Technical Decisions

- **TCS over Channel/AutoResetEvent:** `TaskCompletionSource<string?>` chosen as the minimal-overhead pattern for one-shot sync-to-async bridging. `Channel<string>` rejected as overengineered; `AutoResetEvent` rejected due to shared mutable state.
- **`RunContinuationsAsynchronously` flag:** Required to prevent continuations from executing on the Avalonia UI thread, which would deadlock.
- **Two consumers, one event:** `AvaloniaInputReader` (persistent subscriber) and `AvaloniaDisplayService` (scoped subscriber) safely share `InputSubmitted` because the game thread is single-threaded — only one blocks at a time.
- **`IsInteractive = false`:** Deferred to P6 (menus). For now, the game uses numbered text prompts instead of arrow-key navigation.

---

## Related PRs

- PR #1405: Avalonia P5 — Implement input panel with TCS-based thread bridge
