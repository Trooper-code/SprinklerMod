# Milestones

## Next
**Antigravity:** M3 property-storage fertilization & additives on branch `m3-additives`.
**Josh:** Test M3 on Windows (stock Fertilizer/PGR/Speed Grow on a storage shelf, apply via Sprinklers Phone App).

## Milestones
| # | Milestone | Done when | Status | Tag |
|---|-----------|-----------|--------|-----|
| M0 | Research | Rack attachment, grow-light pot coverage, pot water API, how a rack item is activated, and save/load are documented with evidence; a log-only probe mod confirms the key ones in a real run; an M1 design is written | done (probe optional; M1 confirmed live) | |
| M1 | Proof of concept | A head clones a grow light via S1API, hangs on a Suspension Rack, and `F8` waters every pot under all heads in the current property (`F7` spawns head) | done | |
| M2 | Real item + Phone App | Own name, description, price and Oscar's shop listing; works after save and reload; "Sprinklers" S1API `PhoneApp` with per-property **Water** and **Water All** | done | |
| M3 | Fertilization / Additives | Property storage shelf consumption + **Fert / PGR / Speed Grow** buttons and status feedback in the Phone App | in progress | |
| M4 | Polish, effects & IL2CPP | 5-segment spray effect and sound; `MelonPreferences`; co-op sync; main install back on normal branch with every game class checked against IL2CPP proxies and an IL2CPP build that loads | planned | |
| M5 | Model and release | Original sprinkler model (pipe and nozzle); IL2CPP and Mono builds; README complete; packaged zip; `v1.0.0` (publishing later is optional, see D4) | planned | |

## Later (not planned yet)
- Dedicated **Additive Reservoir** placeable item to hold additive stacks instead of pulling from general property shelves.
- Optional **Wall-Mounted Button** (`ToggleableSurfaceItem`) calling `SprinklerController`.
- Automatic watering below a threshold (D5). Needs a game-time tick, a setting, and multiplayer and performance
  testing.

## Progress log
- 2026-11-19: Project started. Design settled (D1–D4 in `docs/DECISIONS.md`). M0 research prompt written.
- 2026-11-19: Switched from automatic to manual watering for v1 (D5). M0 prompt and M3 updated.
- 2026-11-19: Game versions confirmed (v0.4.7f12, both copies). Main install stays on the Mono branch for M0; the IL2CPP name check moves from M0 to M2. Tagged `m0-base`.
- 2026-11-25: Design updated (D6): automatic per-property chaining, S1API `PhoneApp` trigger, and property-storage fertilization planned for M3 (`docs/superpowers/specs/2026-11-25-overhead-sprinkler-design.md`). Steam install switched to Mono and MelonLoader 0.7.3 installed; started M1 on `m1-poc`.
- 2026-11-25: M1 (`m1-poc`) and M2 (`m2-phone-app`) verified live on Mono: $1,500 Oscar's Warehouse listing and redesigned Sprinklers Phone App work as expected. Started M3 on `m3-additives`.
