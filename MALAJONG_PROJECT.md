# Malajong — Project Handoff Summary

> Paste this into a new Claude conversation to continue where we left off.

---

## What is Malajong?

A single-player roguelike deckbuilder game inspired by **Balatro**, but using **Hong Kong Mahjong tiles** instead of poker cards. Players draw 13-tile hands, play Mahjong sets (Pong, Chow, Kong) to score cumulatively against escalating point quotas called **Wind Blinds**, and collect passive modifiers called **Artifacts** between rounds — similar to Balatro's Jokers.

**Working title:** Malajong  
**Team name:** SanRokuNana  
**Club:** CodeCatalyst — Swinburne Technology Club  
**Last Updated:** 5/7/2026

---

## Team

| Role | Name |
|---|---|
| Project Manager | Nguyễn Lâm Khải |
| Lead Developer + Designer | Lê Thanh Hoàng Minh |
| Designer + Developer | Nguyễn Minh Khang |
| Mentor (Technical Advisor) | Nguyễn Minh Nhật |

---

## Tech stack

| Tool | Purpose |
|---|---|
| Unity 6 (2D URP) | Game engine |
| C# | All game logic, scoring, UI |
| Aseprite | 2D pixel art (tiles, UI) |
| Git + GitHub | Version control |
| Jira | Task + sprint tracking |

**Architecture pattern:** MVC + ScriptableObjects (so designers can add content without touching code)

---

## Core game design

### Scoring model (Aotenjo-inspired, NOT standard Mahjong fan/han)
- Each set played contributes **cumulative score**: `base tile points × combo multiplier × Artifact multiplier`
- Score builds across up to **4 turns per round** — not scored once at the end like real Mahjong
- No fan/han cap — multipliers stack and scale like Balatro's `chips × mult` formula
- Standard Mahjong yaku tables, seating wind bonuses, dealer repeats = **out of scope**

### Combo types
| Combo | Description | Example |
|---|---|---|
| Chow | 3 sequential tiles of same suit | 1-2-3 bamboo |
| Pong | 3 identical tiles | 6-6-6 characters |
| Kong | 4 identical tiles | 9-9-9-9 dots |
| Pair | 2 identical tiles | Required to complete a hand |
| Full hand (Mahjong!) | All 13 tiles form valid sets + pair | Highest scoring |

### Run structure
- **8 antes** total per run
- Each ante has a **Wind Blind** (escalating point quota)
- Every **3rd blind** is a **Boss Wind** — adds a special constraint rule (e.g. "no Chows this round")
- Between blinds: **Shop phase** — spend coins on Artifacts and tile upgrades
- **~10 Artifacts** at demo stage, each producing a distinct scoring strategy
- **Target run length:** 15–30 minutes

### Fail / win condition
- Miss the blind's quota before turns run out → **run over**
- Clear all 8 antes → **run complete**

---

## Unique Selling Point (USP)

**Problem it solves:** "Balatro but with Mahjong tiles" describes the aesthetic, not a USP. A reviewer's obvious objection is that the theme is a skin over an existing formula, since the scoring model (chip × mult, escalating blinds, shop between rounds) is currently a close structural copy. The USP needs to come from a mechanic that only exists because the deck is Mahjong tiles, not playing cards.

### Primary — Suit-Locked Synergy Tension
Mahjong has three fixed suits (Bamboo / Characters / Dots) plus Honor tiles, and its two core combos have suit-dependent shapes: a Chow requires three sequential tiles of the *same suit*; a Pong/Kong requires identical tiles regardless of suit. A 52-card deck has no equivalent constraint — any suit can complete any hand shape. This asymmetry is the one lever a card-based deckbuilder structurally cannot copy.

- Introduce a per-run **Suit Affinity** meter. Playing Chows/Pongs of a committed suit raises that suit's affinity and its scoring multiplier; playing off-suit tiles decays it.
- Artifacts amplify or subvert this (reward mono-suit purity, or reward deliberately breaking affinity for a burst).
- Turns "which suit am I committing to this run" into the central deckbuilding decision — an axis Balatro has no equivalent of, because standard card suits don't gate combo shapes the way Mahjong suits do.

### Secondary — Honor Tiles as a Third Resource Layer
Winds (East/South/West/North) and Dragons (Red/Green/White) have no scoring role in real Mahjong outside seat/round-wind matching. Repurposing them as the game's "consumable/tarot" layer is thematically native rather than bolted on:
- Dragon Pongs trigger one-off effects (peek the wall, freeze a tile, forced discard-and-redraw).
- Wind tiles determine which Boss Blind constraint is active that round.
- Gives Malajong a built-in third resource category most Balatro-likes have to invent from scratch.

### Tertiary / stretch — Tenpai Risk-Reward
Real Mahjong has *tenpai*: being one tile from completing a hand. Stretch goal: reward score based on how close discarded tiles were to finishing a bigger set, incentivizing holding out for a Kong instead of settling for a Pong. Genuine decision-space cards can't replicate, since cards have no native "waiting" state. **Recommended to scope after the demo milestone (14/8)** — flag as a v2 feature in the pitch so it reads as vision, not scope creep.

### One-line pitch for the supervisor
> "Unlike card-based deckbuilders, Malajong's scoring is built around suit-commitment tension that only exists because Mahjong has fixed suits and sequential Chows — with Honor tiles repurposed as a native consumable layer instead of a bolted-on mechanic."

---

## Core gameplay loop

The loop is **not** standard Mahjong turn order (draw-discard around a table). It's closer to Balatro's "play a hand" structure:

1. **Ante starts** — Wind Blind quota is revealed.
2. **Draw hand** — 13 tiles from the tile bag.
3. **Turn loop** (repeats up to 4 times per round):
   - Select tiles that form a valid Chow, Pong, or Kong and play it.
   - **Every combo played costs one turn** — this applies to all combo types equally, not just Full Hand.
   - Score accumulates cumulatively (`base chip × combo mult × Artifact mult`), hand refills back to 13 tiles.
4. **After each turn, check three exit conditions:**
   - **Full hand achieved** (all 13 tiles form valid sets + a pair in one arrangement) → round ends **instantly**, even if turns remain, with a large bonus. This is the one exception to "play a combo, keep going" — Full Hand consumes the entire hand at once, so there's nothing left to refill toward.
   - **Quota reached** (cumulative score ≥ Wind Blind target) → round clears, proceed to shop phase. Doesn't require using all 4 turns.
   - **Turns exhausted and quota not met** → run over.

**Design note flagged for the team:** consider letting a quota-met-early clear (via normal combos, not Full Hand) bank leftover turns as bonus coins, similar to Balatro rewarding early blind clears — cheap addition, high value for making early clears feel rewarded rather than wasteful.

---

## Hand scoring baseline (chip × mult)

Formula: `base chip × combo mult × Artifact mult`. Proposed starting values — needs playtesting to tune, but progression scales with rarity/difficulty:

| Hand | Condition | Base Chip | Base Mult | Notes |
|---|---|---|---|---|
| Pair | 2 identical tiles | 5 | ×1 | Required to complete a hand, not counted as a "main" combo |
| Chow | 3 sequential tiles, same suit | 15 | ×2 | Only Bamboo/Characters/Dots can Chow; Honor tiles can't |
| Pong | 3 identical tiles | 20 | ×2 | Same tier as Chow but slightly higher chip — harder to land |
| Kong | 4 identical tiles | 40 | ×3 | Noticeably rarer than Pong |
| Concealed Kong | Self-drawn Kong, unrevealed beforehand | 55 | ×4 | Risk/reward variant — rewards not exposing the tile early |
| Pure Hand bonus | All 13 tiles from a single suit (excl. Honors) | 150 | ×10 | The hand that embodies the "suit-locked" USP |
| All Honors bonus | Entire hand completed from Honor tiles | 180 | ×12 | Very hard to build (only 4 Winds + 3 Dragons exist) |
| Full Hand (Mahjong!) | All 13 tiles form valid sets + pair, ends the round | 100 | ×8 | Ends the round instantly (see Core gameplay loop); stacks on top of mult already accumulated that round |

**Implementation notes:**
- Pure Hand / All Honors bonuses should be a **post-check modifier** run after `Hand.GetCombos()` returns its valid list — they're properties of the whole hand, not of one 3–4 tile set, so they shouldn't be a separate `Combo` subclass.
- Full Hand's mult should **add onto** the total already accumulated from combos played earlier that round (cumulative, Aotenjo-inspired model), not recalculated from scratch.
- If Suit Affinity (below) ships, Pure Hand will interact strongly with it — cap the total multiplier early (e.g. 2.0× ceiling on affinity) to avoid score explosion. Reserve full balance pass for the QA milestone (18/9).

---

## C# class architecture (planned)

```
Tile              — Value, Suit (enum), Type, IsTerminal, IsHonor, BasePoints
TileBag           — List<Tile>, Draw(), Shuffle(), Remaining
Hand              — List<Tile>, MaxSize=13, AddTile(), RemoveTile(), GetCombos()
Combo (abstract)  — Name, Tiles, BasePoints, Multiplier, abstract IsValid(), virtual Score()
  └─ Pung, Chow, Kong, MahjongHand  (each override IsValid + Score)
Spirit (abstract) — Name, Rarity, Description, abstract OnComboPlayed(), virtual OnRoundEnd()
  └─ One subclass per Artifact (e.g. GreenDragonSpirit)
Blind             — Name, TargetScore, IsBoss, BossRule (Action), CoinReward
GameRun           — TileBag, Hand, List<Spirit>, CurrentBlind, Coins, Ante
ScoreEngine       — static: Calculate(combo, spirits), ApplySpirits(), GetChips(), GetMult()
```

### USP-related additions (additive only — no rework of the above)

```
SuitAffinity        — Dictionary<TileSuit, float> level (0..1 or uncapped)
                        Decay(suit), Boost(suit, amount), GetMultiplier(suit)

Combo (abstract)    — + AffinityDeltas : Dictionary<TileSuit, float>  (new field)
                        existing: Name, Tiles, BasePoints, Multiplier, IsValid(), Score()

ScoreEngine         — Calculate(combo, spirits) now also calls
                        GameRun.SuitAffinity.Boost(...) per combo.AffinityDeltas
                        and folds GetMultiplier(suit) into GetMult()

HonorEffectResolver — static: Resolve(Tile honorTile, GameRun run)
                        reads Tile.HonorEffect, mutates run state accordingly

GameRun             — + SuitAffinity SuitAffinity (new field, same lifecycle as Coins/Ante)
```

**ScriptableObject additions** (extend existing folders, don't create new top-level ones):
- `TileData` — add `HonorEffect` field (enum, default `None`) so designers can wire Dragon/Wind behaviour without code changes.
- `ArtifactData` (Spirit backing data) — add optional `AffinityRule` fields (`TargetSuit`, `BonusType`, `BonusValue`) so "mono-suit" Artifacts are data-only and don't need a new C# subclass each time.
- `BossRuleData` can reference Wind tiles directly, tying the Boss Wind constraint to which Wind tile is "in play" that round — reuses the existing `Blind` class.

**Example Artifacts demonstrating the USP** (build these first when filling out the ~10 Artifact list):

| Artifact | Effect | Demonstrates |
|---|---|---|
| Bamboo Vow | +0.5× mult per Chow/Pong played in Bamboo this round; resets on any off-suit combo | Suit commitment risk/reward |
| Broken Compass | Playing 2 different suits in one turn grants a flat +20 chip burst, but zeroes affinity | Anti-synergy / burst alternative to purity |
| Green Dragon Spirit | Pong of Green Dragon: freeze one tile in hand for the rest of the round | Honor tile consumable layer |
| Compass Rose | Whichever suit currently has highest affinity scores +1 chip per tile in that suit's combos | Rewards reading run-state, not a static bonus |

**Milestone slotting** (fits inside existing schedule, no new milestone needed):
- Mahjong scoring engine (10/7/2026): implement `SuitAffinity` + `AffinityDeltas` alongside the base chip × mult calculator — same PR, not a follow-up.
- Artifact system (14/8/2026): build the 4 example Artifacts above first, before the remaining ~6 generic Artifacts.
- Tenpai risk-reward: explicitly parked as post-demo / v2, mentioned in the presentation deck (20/9/2026) as roadmap vision.

**OOP concepts covered** (good for upcoming OOP course):
- **Inheritance** — Pung/Chow/Kong extend abstract `Combo`; each Spirit subclass extends `Spirit`
- **Polymorphism** — `ScoreEngine` calls `combo.Score()` without caring which subclass
- **Encapsulation** — `TileBag` hides shuffle logic; `Hand` owns its tile list
- **Abstraction** — `Spirit.OnComboPlayed()` is a contract; subclasses decide the effect

---

## Unity project folder structure

```
Assets/
├── Scripts/
│   ├── Core/          ← Tile, Hand, TileBag
│   ├── Scoring/       ← ScoreEngine, Combos
│   ├── Artifacts/     ← Spirit base + subclasses
│   ├── Roguelike/     ← GameRun, Blind, Shop
│   └── UI/            ← View scripts (MVC)
├── ScriptableObjects/
│   ├── Tiles/
│   ├── Artifacts/
│   └── Blinds/
├── Scenes/            ← Game, Shop, MainMenu
├── Sprites/           ← Aseprite exports
└── Audio/
```

---

## Dev environment setup (done / to do)

- [ ] Unity Hub installed → Unity 6 (2D URP) project created named `Malajong`
- [ ] IDE: JetBrains Rider (recommended) or Visual Studio Community
- [ ] Git installed, repo on GitHub with Unity `.gitignore`
- [ ] Unity Editor settings: Version Control → **Visible Meta Files**, Asset Serialization → **Force Text**
- [ ] Aseprite installed for tile art
- [ ] Jira board set up (milestone: 15/6/2026)

---

## Project milestones

| Milestone | Deliverable | Date |
|---|---|---|
| Finalize proposal | Approved proposal & charter v1.1 | 12/6/2026 |
| Jira + GitHub setup | Project board, repo scaffold, MVC skeleton | 15/6/2026 |
| Mahjong scoring engine | Pong/Chow/pair validator + chip × mult calculator | 10/7/2026 |
| Roguelike loop + shop | 8-ante run, shop UI, currency, Boss Wind rules | 31/7/2026 |
| Artifact system | ~10 Artifacts with distinct synergies | 14/8/2026 |
| Aseprite art + UI | Final tile art, UI overlays, SFX integrated | 28/8/2026 |
| QA + balancing | Playtesting across 3 seeds, bug triage, balance | 18/9/2026 |
| Demo freeze + presentation | Final build + presentation deck | 20/9/2026 |

---

## Documents produced

| Document | Status |
|---|---|
| `Malajong_Proposal_v1.1.docx` | ✅ Done — addresses all 5 mentor feedback points |
| `Malajong_Supervisor_Checkin_Agenda.docx` | ✅ Done — 45–60 min ways-of-working agenda |

### What changed in proposal v1.1 (vs v1.0)
1. **Scoring model clarified** — new subsection explicitly distinguishing Aotenjo-inspired scoring from standard Mahjong rules
2. **Goals split** — technical goals and gameplay goals are now separate with measurable success criteria
3. **Demo scope made concrete** — 8 antes, ~10 Artifacts, 15–30 min run length stated explicitly
4. **Gameplay loop diagram added** — draw → play → score → shop → advance ante flowchart
5. **Milestone dates filled** — all blank dates populated, Deliverable column added

---

## What's next (suggested)

- Write the first C# classes: `Tile`, `TileSuit` enum, `TileBag`
- Set up ScriptableObjects for tile data in Unity
- Build the combo detection algorithm (`Hand.GetCombos()`)
- Implement `ScoreEngine` with chip × multiplier stacking logic **and** `SuitAffinity` from the start (same PR — see USP section), using the chip/mult baseline table above
- Implement `HonorEffectResolver` stub (can start with just `PeekWall` and `ForceRedraw`, add the rest later)
- Design the remaining Artifact list (~6 more items with synergies, on top of the 4 USP-demonstrating ones already specced)

**Next session starts here:** coding the actual `ScoreEngine` / combo scoring logic in C#, using the class architecture and chip/mult baseline already locked in above.

---

## Changelog

- **v1.1 → this doc:** added Unique Selling Point section (Suit-Locked Synergy Tension as primary USP, Honor Tiles as secondary, Tenpai as stretch), clarified the core gameplay loop (every combo costs a turn; Full Hand ends the round instantly, not just the turn), added the hand scoring baseline table (chip/mult per hand type), and extended the C# architecture with additive `SuitAffinity` / `HonorEffectResolver` classes plus 4 example Artifacts that demonstrate the USP mechanically.
- **This doc → current:** Scoring engine milestone (10/7/2026) implemented and reviewed across several passes. See "Scoring Engine — Implementation Status" below for full detail and open items.

---

## Scoring Engine — Implementation Status (as of this session)

Files: `Tile.cs`/`TileData.cs`, `Combo.cs`, `ScoreEngine.cs`, `ScoreTesting.cs`. Built with Gemini, reviewed/debugged with Claude across multiple passes.

### Done

**Tile.cs**
- `Tile` is a plain class (not yet a ScriptableObject — still pending per architecture doc).
- `IsHonor` is a computed property (`Suit == TileSuit.Honor`), not a separate settable field — prevents Suit/IsHonor desync.
- `IsSelfDrawn` field added (defaults `false`) to support Concealed Kong validation.

**Combo.cs**
- Abstract `Combo` base: `Tiles` (defensively copied in constructor), `Name`, `BaseChips`, `BaseMult`, `AffinityBonus` (abstract, per-subclass), `IsValid()`.
- `AffinityDeltas` computed lazily as a property — only populates if combo `IsValid()`, avoids leaking affinity data for invalid attempts.
- Subclasses implemented: `Pong` (20/×2.0), `Chow` (15/×2.0, sorts a *copy* of tiles to check sequence, doesn't mutate `Tiles`), `Kong` (40/×3.0), `Pair` (5/×1.0), `ConcealedKong : Kong` (55/×4.0, overrides `IsValid()` to additionally require all tiles `IsSelfDrawn`).
- All base chip/mult values match the baseline table in this doc.
- All combo types now have `AffinityBonus` values (Pong 0.1, Chow 0.1, Kong 0.2, ConcealedKong 0.4, Pair 0.0) — these are placeholder numbers, not yet spec'd/playtested.

**ScoreEngine.cs**
- `Calculate(combo, fullHand)`: rejects invalid combos (returns 0,0), otherwise returns `BaseChips`/`BaseMult` run through `ApplyPostCheckBonuses`.
- `ApplyPostCheckBonuses`: implements Pure Hand (+150 chips, ×10) and All Honors (+180 chips, ×12) as post-check modifiers on the full 13-tile hand, per the design doc's instruction that these are whole-hand properties, not separate `Combo` subclasses. Guarded to only run on exactly 13 tiles.
- `EvaluateFullHand(fullHand)`: separate method (not part of `Calculate`) since Full Hand ends the round instantly rather than being a normal turn-costing combo. Requires exactly 14 tiles (4 sets + 1 pair = 14 — resolved an earlier ambiguity about whether hands are 13 or 14 tiles at win-check time). Returns 100 chips / ×8 mult per baseline table.
- `IsWinningMahjongHand` / `CanFormSets`: recursive backtracking solver that partitions a 14-tile hand into a pair + valid Chows/Pongs/Kongs. This was the one piece originally stubbed as "next sprint" — it's now implemented and working in tests.
- Artifact hook and Suit Affinity hook are both left as commented stubs in `Calculate`, ready to wire in once `SuitAffinity`/`Spirit` classes exist.

**ScoreTesting.cs**
- `MonoBehaviour` test harness (`ScoringTester`), runs on `Start()`, logs to Unity console. No UI/sprites needed.
- Covers: valid Pong, valid Chow, Concealed Kong (with one non-self-drawn tile to confirm it correctly invalidates), Pure Hand, All Honors, Full 14-tile Mahjong hand.
- Tests are eyeball/log-based (`Debug.Log`), not asserts — someone has to manually confirm the printed numbers match expected values.

### Known open items / not yet done

1. **Suit Affinity system itself** (`SuitAffinity` class, `Boost`/`Decay`/`GetMultiplier`) doesn't exist yet — `AffinityDeltas` on `Combo` is ready to feed it, but nothing consumes it yet. This was supposed to ship "same PR" as the base calculator per the milestone note; it slipped to a follow-up.
2. **Affinity bonus values are placeholders** — 0.1/0.1/0.2/0.4/0.0 across Chow/Pong/Kong/ConcealedKong/Pair were picked ad hoc during review, not from a balance pass. Needs a real number pass once `SuitAffinity` exists and is playtestable.
3. **`Tile` is not yet a ScriptableObject** — architecture doc calls for `TileData` as a ScriptableObject so designers can add content without touching code. Currently a plain C# class.
4. **No `HonorEffect` field on `Tile`** yet — needed for Dragon/Wind honor-tile behavior (per architecture doc's `HonorEffectResolver` section).
5. **Test harness has no assertions** — logs values for manual comparison only. Fine for now, but worth converting to actual pass/fail once more combos are added.
6. **Game Manager integration not started** — nothing yet calls `ScoreEngine.Calculate` / `EvaluateFullHand` from real gameplay (turn loop, hand refill, quota check). All testing so far is via the standalone `ScoringTester` harness.
7. **`ConcealedKong`/`Kong` share validation via inheritance** (`ConcealedKong : Kong`) — resolved an earlier duplication concern, but worth double-checking this relationship makes sense once Artifacts start interacting with combo types (an Artifact keyed off `is Kong` will also match `ConcealedKong`, which may or may not be intended).

### Suggested next steps
- Build `SuitAffinity` class and wire the `AffinityDeltas` stub into `Calculate` (item 1) — this was the main thing that slipped from the original milestone scope.
- Convert `Tile`/`TileData` to a ScriptableObject-backed setup per the architecture doc.
- Start Game Manager / turn loop integration so the scoring engine is actually driven by gameplay instead of the test harness.
- Revisit affinity bonus values once there's something to playtest against.

---

*Summary generated from a full design session — game concept, tech stack, architecture, proposal v1.1, USP design, core loop, scoring baseline, and dev setup all covered.*