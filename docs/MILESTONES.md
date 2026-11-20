# Milestones

## Next
**Antigravity:** `docs/prompts/M0-research.md` on branch `m0-research`.
**Josh:** paste the prompt line into Antigravity (workspace `E:\Sprinkler\S1-Overhead-Sprinkler`).

## Milestones
| # | Milestone | Done when | Status | Tag |
|---|-----------|-----------|--------|-----|
| M0 | Research | Rack attachment, grow-light pot coverage, pot water API, how a rack item is activated, and save/load are documented with evidence; a log-only probe mod confirms the key ones in a real run; an M1 design is written | in progress | |
| M1 | Proof of concept | A head clones a grow light, hangs on a Suspension Rack, and waters every pot under that rack on a hotkey | planned | |
| M2 | Real item | Own name, description, price and Oscar's shop listing; stand-in model; works after save and reload; main install back on the normal branch, every game class we use checked against the IL2CPP proxies, and an IL2CPP build that loads | planned | |
| M3 | Activation and polish | Player activates the head the way the game's sprinklers work; spray effect and sound; any settings in MelonPreferences | planned | |
| M4 | Multiplayer and save safety | Watering syncs to co-op clients; removing the mod doesn't break saves | planned | |
| M5 | Model and release | Original sprinkler model (pipe and nozzle); IL2CPP and Mono builds; README complete; packaged zip; `v1.0.0` (publishing later is optional, see D4) | planned | |

## Later (not planned yet)
- Automatic watering below a threshold (D5). Needs a game-time tick, a setting, and multiplayer and performance
  testing.

## Progress log
- 2026-11-19: Project started. Design settled (D1–D4 in `docs/DECISIONS.md`). M0 research prompt written.
- 2026-11-19: Switched from automatic to manual watering for v1 (D5). M0 prompt and M3 updated.
- 2026-11-19: Game versions confirmed (v0.4.7f12, both copies). Main install stays on the Mono branch for M0; the IL2CPP name check moves from M0 to M2. Tagged `m0-base`.
