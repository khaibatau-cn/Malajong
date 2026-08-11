# Dead Hand & the Lucky Cat Redraw

> Mechanic change — adds a second loss condition and its reprieve.
> Status: implemented. Requires a scene rebuild (`Malajong → Setup Playable Scene Placeholder`).

---

## Why this exists

The smallest legal play in Malajong is a **Pair** — two tiles. There is no way to play a single
tile. So a hand containing no Pair, no Chow, no Pong and no Kong cannot be played at all.

Before this change the game had exactly one loss condition: `HandsRemaining <= 0` with the quota
unmet. A player holding an unplayable hand with no discards left was simply **stuck** — the PLAY
button did nothing, the DISCARD button did nothing, and the game gave no indication that anything
was wrong. The only way out was abandoning the run manually.

A dead 14-tile hand is uncommon but genuinely reachable. Example: `1/3/5/7/9` Bamboo, `1/3/5/7/9`
Characters, `1/3/5` Dots — no two tiles match, and no three are consecutive within a suit. It
becomes more likely after several discards have thinned the hand's useful tiles out.

---

## The rule

A hand is **dead** when `ScoreEngine.FindPlayableCombos()` returns nothing.

| State | Outcome |
|---|---|
| Dead hand, discards remaining | Survivable — the player must discard. Warned in the HUD. |
| Dead hand, **zero discards** | **Deadlock.** Triggers the redraw prompt. |
| Redraw produces a playable hand | Run continues, **+1 discard** awarded. |
| Redraw produces another dead hand | **Run over.** |

The redraw is a single-shot reprieve per deadlock, not a resource the player can lean on.

---

## Flow

```
play / discard
      │
      ▼
CheckExitConditions
      │
      ├── score >= target ────────────► round cleared → Shop
      ├── hands exhausted ────────────► GAME OVER ("Out of hands")
      └── dead hand AND discards == 0
                │
                ▼
        AwaitingRedraw = true
        OnRedrawRequired fired
                │
                ▼
        ┌───────────────────┐
        │   REDRAW MODAL    │  "DEAD HAND — the lucky cat is
        │   [ASK THE CAT]   │   now your entire strategy."
        └───────────────────┘
                │
                ▼
          RedrawHand()
       hand binned, 14 drawn
                │
        ┌───────┴────────┐
        ▼                ▼
   has a combo       still dead
        │                │
   +1 discard       GAME OVER
   run continues    ("The cat looked away.")
```

Check order matters: quota and hands-exhausted are both evaluated **before** the deadlock branch,
so clearing the round or running out of turns always takes precedence over a hand that merely
happens to be unplayable.

---

## Opening hands are never dead

`StartRound` now deals through `DealOpeningHand()`, which reshuffles the wall and redeals until the
hand holds at least one playable combo. A round can never open already in trouble.

Capped at 50 attempts so an incomplete tile set cannot hang the editor; with a full 144-tile wall
this effectively always succeeds on the first or second deal.

---

## Telling the player it's coming

Three escalating levels, so a deadlock is never a surprise:

1. **Discards at zero** — the discard counter turns vermilion instead of gold. Losing the safety
   net is visible before it costs anything.
2. **Dead hand, discards remaining** — the PLAYABLE IN HAND box changes from a grey "no complete
   combo" aside to a **DEAD HAND** warning naming how many discards are left. At that point
   discarding is the only legal move, so it reads as an instruction, not a note.
3. **Dead hand, zero discards** — the redraw modal. One button, no dismiss control: a redraw is
   the only action left on the board, so a close button would only strand the player.

The game over screen also names the cause now — a dead hand and a missed quota are completely
different failures and shouldn't produce the same text.

---

## Run stat: cat saves

`GameManager.CatSaves` counts how many times a redraw pulled the player out of a deadlock. It's a
**run** stat, not a round one, and resets in `InitializeRun()` — surviving the cat twice in one run
is the story worth telling at the end.

Surfaced in four places:

| Where | Shown as |
|---|---|
| Floating badge on survival | `THE CAT SMILES! +1 DISCARD`, becoming `CAT SAVE #2! +1 DISCARD` on repeats |
| The redraw modal itself | *"The cat has already bailed you out twice this run. It is keeping count."* |
| Run Info panel | `Rescued by the Lucky Cat: 2x` |
| Game Over / Victory screens | *"The lucky cat saved you 3 times this run."* |

All four suppress themselves at zero — a run that never deadlocked is never told it was rescued no
times.

---

## Design decisions

| Decision | Rationale |
|---|---|
| Redraw costs **no Hand** | It's mercy, not a turn. Charging a hand would punish bad luck twice. |
| Redrawn tiles are **discarded**, not returned to the wall | Consistent with how `DiscardSelectedTiles` already works. |
| Surviving grants **+1 discard** | Gives the player an actual out instead of returning them to the same cliff edge. |
| The extra discard is **uncapped** | Matches `RestlessWind`, which already grants discards without a ceiling. Artifacts are expected to push this number around. |
| A thin wall is **not** a special case | If fewer than 14 tiles remain, the player draws what's left and the same rule judges the shorter hand. |

### Is the redraw loop exploitable?

No. Surviving a deadlock grants a discard, which can be spent into another deadlock, which grants
another redraw — but each cycle burns roughly 14 tiles off a 144-tile wall, so the loop is bounded
by the wall running dry. It cannot be farmed, and it never advances the score on its own.

---

## Implementation

### `GameManager`

| Member | Purpose |
|---|---|
| `HasPlayableCombo` | Reads through `ScoreEngine.FindPlayableCombos` — the same call that feeds the PLAYABLE IN HAND panel, so what the player is told and what ends the run can never disagree. |
| `IsDeadlocked` | `Playing && DiscardsRemaining <= 0 && !HasPlayableCombo` |
| `AwaitingRedraw` | Set on deadlock; the round is frozen until `RedrawHand()` resolves it. |
| `GameOverReason` | Why the run ended, for the game over screen. |
| `CatSaves` | Successful redraws this run. Resets in `InitializeRun()`. |
| `OnRedrawRequired` | Event the UI answers with the modal. |
| `DealOpeningHand()` | The redeal loop. |
| `RedrawHand()` | Bins the hand, redraws, awards or ends the run. |
| `EndRun(reason)` | Single place that sets `GameOverReason` and the state together. |

### `UIManager`

`ShowRedrawPrompt()` (subscribed to `OnRedrawRequired`), `TakeRedraw()` (the button), plus the
discard-counter and combo-list warning states and the game-over reason text.

Both have a defensive fallback: if `RedrawModal` is unassigned in a scene, the redraw is taken
automatically rather than freezing the board.

### `SceneSetupTool`

Builds `RedrawModal` — malachite card, the maneki-neko from the title art, body text, and a single
`ASK THE CAT` button.

### Not touched

`ScoreEngine`, `Combo`, `SuitAffinity`, and every `SpiritData` subclass. This change is entirely in
the run/turn layer.

---

## Testing it

A deadlock is rare by design, which makes it exactly the sort of path that ships untested. There is
a shortcut:

> **Malajong → Debug → Force Dead Hand** (`Ctrl+Shift+D`), during Play mode.

It replaces the hand with every other rank across three suits — `1/3/5/7/9` Bamboo, `1/3/5/7/9`
Characters, `1/3/5/7` Dots — and zeroes discards. No two tiles match and no three are consecutive
within a suit, so nothing can be formed from it. The redraw prompt fires immediately.

Press it again after surviving a redraw to test the second save, the `CAT SAVE #2` badge, and the
"it is keeping count" line. Press it and then redraw into another dead hand to see the loss path.

The menu item is greyed out outside Play mode, and `DebugForceDeadHand` is wrapped in
`#if UNITY_EDITOR`, so none of it reaches a build.

---

## Open items

- `DiscardsRemaining` has **no upper bound**. `RestlessWind` and now the redraw both increment it
  freely. Fine today, worth a decision once artifacts start stacking discard effects.
- `CatSaves` is per-run only — nothing persists across runs. If the demo wants a lifetime tally or
  an achievement hook, that needs somewhere to save to, which the project does not have yet.
