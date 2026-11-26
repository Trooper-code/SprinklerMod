# M6a-coop-gate: let non-host players water and apply additives

## Read first
- `AGENTS.md` (standing rules: project-specific rules and your Antigravity section)
- `docs/HANDOFF.md`, sections "Setup" and "Tool versions"
- `docs/MILESTONES.md`, row M4 and "Later"
- `docs/research/INDEX.md`, then `docs/research/game-systems.md` ("Adding water & networking") and
  `docs/research/modding-lessons.md`
- `E:\Sprinkler\Multiplayer-Plan\FINDINGS-Sprinkler.md` (the coordinator's findings for this task; S1–S11 below
  refer to its rows)

## Goal
Let any player in a co-op game, not just the host, use the Sprinklers phone app to water and to apply additives,
by removing the mod's own host check and relying on the game's existing server RPCs. Fix the docs that wrongly say
co-op sync is done. Spray effects and a shared watering lock are **not** part of this task (that's M6b).

## Branch
`m6a-coop-gate`, from tag `v1.0.0`.
Before branching: the working copy has 26 files changed only in line endings (CRLF vs committed LF). Run
`git diff --ignore-cr-at-eol --stat`; if it prints nothing, run `git checkout -- .` to discard them. If it prints
anything, stop and report (those are real changes Josh may want).

## Known facts
| Fact | Status | Evidence |
|------|--------|----------|
| `WaterProperty` blocks non-hosts with `if (!InstanceFinder.IsServer)` | unverified | code read: `src/OverheadSprinkler/SprinklerController.cs` line 586 at `ed70ea0` |
| `ApplyAdditiveToProperty` blocks non-hosts the same way | unverified | code read: same file, line 704 |
| Phone app shows "co-op sync comes in M4b" messages | unverified | code read: `src/OverheadSprinkler/SprinklerPhoneApp.cs` lines 562, 615, 651 |
| `GrowContainer.SetMoistureData_Server` and `ApplyAdditive_Server` are `[ServerRpc(RequireOwnership = false)]` | unverified | decompiled Mono `ScheduleOne.Growing/GrowContainer.cs` ~lines 482, 502 (`_decompiled/`, git-ignored) |
| `StorageEntity.SetItemSlotQuantity` is `[ServerRpc(RunLocally = true, RequireOwnership = false)]`, and `ItemSlot.ChangeQuantity` routes through it when the slot has an owner | unverified | `_decompiled/ScheduleOne.Storage/StorageEntity.cs` line 333; `ItemSlot.ChangeQuantity` |
| Refilling a pot with `ChangeMoistureAmount(capacity)` + `SyncMoistureData()` works for the host | confirmed | `docs/research/game-systems.md`, stamped `m1-poc` |
| `docs/MILESTONES.md` M4 lists "co-op sync" as done, but the code still blocks non-hosts and no two-player test exists (so that claim is wrong) | confirmed | coordinator compared the M4 row with the lines above, 2026-11-25 (FINDINGS-Sprinkler S4) |
| Spray effects and the "already watering" lock are per-PC | unverified | code read: FINDINGS-Sprinkler S8, S9 |

## Tasks
1. Copy this prompt to `docs/prompts/M6a-coop-gate.md` and commit it on your branch.
2. Read the decompiled methods in the table above yourself and confirm or correct each "unverified" row in your
   report (names, attributes, what they call). Don't paste method bodies into the repo.
3. In `SprinklerController.WaterProperty` and `ApplyAdditiveToProperty`, remove the non-host early return so a
   non-host runs the same path as the host. Keep the host path byte-for-byte the same in behavior. Keep the
   `SkippedNotServer` fields on the summary types but stop setting them (or remove them everywhere if nothing else
   uses them; say which).
4. In `SprinklerPhoneApp.cs`, remove the "M4b" non-host messages and any branch that only existed for them.
5. In the `#if DEBUG` F8 handler in `OverheadSprinklerMod.cs`, remove the "not the host/server" warning path to match.
6. Docs:
   - `docs/MILESTONES.md`: mark the M4 "co-op sync" claim **wrong** in place (say what was actually delivered), add
     an M6 row ("Co-op: non-hosts can water and apply additives; effects and lock synced"), and a progress-log line.
   - `docs/research/game-systems.md`: add a "Multiplayer" section with the RPC facts from task 2 and gaps S8–S11,
     each labeled. Add it to `docs/research/INDEX.md`.
   - `README.md`: add a short "Multiplayer" section in plain English: every player needs the mod (same version);
     anyone can water; for now the spray effect only shows for the person who pressed the button.
   - `CHANGELOG.md`: a line under `## Unreleased`.
7. Build both targets (Mono `netstandard2.1` and IL2CPP `net6.0`) in Release and Debug.
8. Write the test steps Josh will run (below) into your report, adjusted to anything you learned.

## Tools
- superpowers `systematic-debugging`: if a build breaks or the IL2CPP target behaves differently, find the cause
  before changing code.
- superpowers `verification-before-completion`: before reporting done, re-run both builds and re-read the Goal.
- `ilspycmd`: only if `_decompiled/` is missing or stale; write output to `_decompiled/` (git-ignored).
- Not needed: REA, /swarm.

## Task-specific constraints
- No new networking code, no new dependencies, no new RPCs. If you find the game calls above don't work for a
  non-host without custom networking, stop (see below) rather than adding a message channel.
- Don't touch `SprinklerEffects.cs` or the watering lock behavior; those are M6b.
- Bump the mod version to `1.1.0` in `MelonInfo` and the csproj only if both builds pass.

## Stop and report if
- Any condition in `AGENTS.md` "Stop and report".
- `git diff --ignore-cr-at-eol --stat` shows real changes before you branch.
- A decompiled method in the table is **not** client-callable (no `RequireOwnership = false`), or routes to a
  host-only check: report which one and what it would take.

## Acceptance checks
- Both targets build in Release with no new warnings; paste the summary lines.
- `git grep -n "M4b"` in `src/` returns nothing.
- `git grep -n "InstanceFinder.IsServer" src/` shows no remaining early return in `WaterProperty` or
  `ApplyAdditiveToProperty` (list any other hits and why they stay).
- Every finding in the report is labeled and cites a file and line or a decompiled method.
- Josh's two-player test (he runs it; you only write the steps):
  1. Host and friend both install the new build (same zip) and join the same co-op save.
  2. Friend, standing anywhere: phone → Sprinklers → **Water** on a property with heads and dry pots.
     Expect: both players see the pots' moisture rise to 100% within the spray time; only the friend sees mist.
  3. Friend: apply **Fertilizer** with bottles on a shelf in that property. Expect: bottle count drops by the number
     of eligible pots for both players; fertilizer shows on the pots for both.
  4. Host does steps 2–3 too. Expect: same as v1.0.0.
  5. Save as host, reload, rejoin: moisture and additives are still there.
  6. Send `MelonLoader\Latest.log` from **both** PCs.

## Report
`docs/reports/M6a-coop-gate.md`, using `docs/reports/TEMPLATE.md`. Check it with
`python scripts/check-report.py docs/reports/M6a-coop-gate.md` if `scripts/` exists locally (it's git-ignored;
skip and say so if it's missing). Commit on `m6a-coop-gate`, then reply with the path and a 3-line summary.
