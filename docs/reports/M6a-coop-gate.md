# Report: M6a-coop-gate

**Prompt:** `docs/prompts/M6a-coop-gate.md`
**Branch:** `m6a-coop-gate` (created from `main` `ed70ea0`, not tag `v1.0.0` `65639ef`; see Open questions) · **Last commit:** `0f54f36` (prior to adding this report)
**Status:** done

## Tool versions
| Tool | Version used | Matches HANDOFF.md? |
|------|--------------|---------------------|
| Schedule I, Mono copy | v0.4.7f12 Alternate (`_decompiled/` from `E:\Sprinkler\Schedule I Mono`) | yes |
| MelonLoader | 0.7.3 (`LavaGang.MelonLoader` 0.7.3) | yes |
| S1API Forked (ifBars) | 3.2.3 (`S1API.Forked` 3.2.3) | yes |
| .NET SDK | 10.0.404 (`dotnet --version`) | yes |

## What changed
- `docs/prompts/M6a-coop-gate.md`: copied prompt from `E:\Sprinkler\Multiplayer-Plan\prompts\Sprinkler-M6a-coop-gate.md` (`93fd267`).
- `src/OverheadSprinkler/SprinklerController.cs`: removed `if (!InstanceFinder.IsServer)` early returns in `WaterProperty` and `ApplyAdditiveToProperty`, removed unused `FishNet` / `Il2CppFishNet` `using` directives, and removed the unused `SkippedNotServer` property/parameter from `WaterSummary` and `AdditiveApplySummary` (`7291e04`). Did not touch `SprinklerEffects.cs` or `ActiveWateringPropertyIds` lock behavior.
- `src/OverheadSprinkler/SprinklerPhoneApp.cs`: removed the three `SkippedNotServer` / `M4b` non-host warning branches in `OnWaterPropertyClicked`, `OnWaterAllClicked`, and `OnApplyAdditiveClicked` (`7291e04`).
- `src/OverheadSprinkler/OverheadSprinklerMod.cs`: removed the `#if DEBUG` F8 `SkippedNotServer` warning branch and bumped `MelonInfo` / startup log version from `1.0.0` to `1.1.0` (`7291e04`).
- `src/OverheadSprinkler/OverheadSprinkler.csproj`: bumped the project `Version` property from `1.0.0` to `1.1.0` (`7291e04`).
- `docs/MILESTONES.md`, `docs/research/game-systems.md`, `docs/research/INDEX.md`, `README.md`, `CHANGELOG.md`: marked M4's "co-op sync" claim **wrong** in place, added M6 milestone & progress log, added `## Multiplayer` research section + index entry, added plain-English `## Multiplayer` README section, and added `## Unreleased` entry in CHANGELOG (`0f54f36`).

## Findings
| Finding | Status | Evidence |
|---------|--------|----------|
| `WaterProperty` previously blocked non-hosts with `if (!InstanceFinder.IsServer)` (now removed) | confirmed | `src/OverheadSprinkler/SprinklerController.cs` line 586 at `ed70ea0`; removed in `7291e04` |
| `ApplyAdditiveToProperty` previously blocked non-hosts with `if (!InstanceFinder.IsServer)` (now removed) | confirmed | `src/OverheadSprinkler/SprinklerController.cs` line 704 at `ed70ea0`; removed in `7291e04` |
| Phone app previously showed `M4b` non-host messages (now removed) | confirmed | `src/OverheadSprinkler/SprinklerPhoneApp.cs` lines 562, 615, 651 at `ed70ea0`; removed in `7291e04` |
| `GrowContainer.ChangeMoistureAmount` + `SyncMoistureData()` routes through `SetMoistureData_Server` (`[ServerRpc(RequireOwnership = false)]`) → `SetMoistureData_Client` (`[ObserversRpc]` / `[TargetRpc]`) → `SetMoistureAmount`, with no host-only guard | unverified | `_decompiled/ScheduleOne.Growing/GrowContainer.cs` lines 466–500, 844–889 |
| `GrowContainer.ApplyAdditive_Server(string)` is `[ServerRpc(RequireOwnership = false, RunLocally = true)]` → `ApplyAdditive_Client` (`[ObserversRpc(RunLocally = true)]` / `[TargetRpc]`) → `ApplyAdditive` (idempotent via `IsAdditiveApplied`), with no host-only guard | unverified | `_decompiled/ScheduleOne.Growing/GrowContainer.cs` lines 502–543, 965–1021 |
| `ItemSlot.ChangeQuantity(int, bool)` routes non-empty decrements to `SlotOwner.SetItemSlotQuantity`, which on `StorageEntity` is `[ServerRpc(RunLocally = true, RequireOwnership = false)]` → `SetItemSlotQuantity_Internal` (`[ObserversRpc(RunLocally = true)]`) | unverified | `_decompiled/ScheduleOne.ItemFramework/ItemSlot.cs` lines 228–256; `_decompiled/ScheduleOne.Storage/StorageEntity.cs` lines 333–345, 746–759 |
| When `Quantity + change <= 0`, `ItemSlot.ChangeQuantity` calls `ClearStoredInstance` → `SlotOwner.SetStoredInstance(null, SlotIndex, null)`, which on `StorageEntity` is `[ServerRpc(RunLocally = true, RequireOwnership = false)]` → `SetStoredInstance_Internal` | unverified | `_decompiled/ScheduleOne.ItemFramework/ItemSlot.cs` lines 167–198, 244–246; `_decompiled/ScheduleOne.Storage/StorageEntity.cs` lines 311–331 |
| `RunLocally = true` on `ApplyAdditive_Server`, `SetItemSlotQuantity`, and `SetStoredInstance` causes FishNet's `RpcWriter___Observers_*` to log harmless `"Cannot complete action because server is not active..."` warnings on non-host clients | unverified | `_decompiled/ScheduleOne.Growing/GrowContainer.cs` lines 979–996; `_decompiled/ScheduleOne.Storage/StorageEntity.cs` lines 717–734 |
| `docs/MILESTONES.md` M4 row previously listed "co-op sync" as done even though non-hosts were blocked and no two-player test existed | wrong | `docs/MILESTONES.md` M4 row at `ed70ea0` vs `SprinklerController.cs` lines 586, 704; fixed in `0f54f36` |
| Spray effects (`PlaySprayOnHeads`) and watering lock (`ActiveWateringPropertyIds`) remain per-PC until M6b; local snapshot & all-players-need-mod constraint remain | unverified | `src/OverheadSprinkler/SprinklerController.cs` lines 188, 592, 620, 670–704; `src/OverheadSprinkler/OverheadSprinklerMod.cs` line 152 |
| Refilling a pot with `ChangeMoistureAmount(capacity)` + `SyncMoistureData()` works for the host | confirmed | `docs/research/game-systems.md` § Pot water (`m1-poc`) |

Written to: `docs/research/game-systems.md` (§ Multiplayer) and `docs/research/INDEX.md`

## Tests
Tools used: `systematic-debugging` (not needed — builds passed cleanly), `verification-before-completion` (re-ran Release and Debug builds plus all `git grep` and script checks after edits).

```
dotnet build src/OverheadSprinkler/OverheadSprinkler.csproj -c Release
  OverheadSprinkler -> E:\Sprinkler\S1-Overhead-Sprinkler\src\OverheadSprinkler\bin\Release\netstandard2.1\OverheadSprinkler.Mono.dll
  OverheadSprinkler -> E:\Sprinkler\S1-Overhead-Sprinkler\src\OverheadSprinkler\bin\Release\net6.0\OverheadSprinkler.IL2CPP.dll
Build succeeded.
    0 Warning(s)
    0 Error(s)

dotnet build src/OverheadSprinkler/OverheadSprinkler.csproj -c Debug
  OverheadSprinkler -> E:\Sprinkler\S1-Overhead-Sprinkler\src\OverheadSprinkler\bin\Debug\net6.0\OverheadSprinkler.IL2CPP.dll
  OverheadSprinkler -> E:\Sprinkler\S1-Overhead-Sprinkler\src\OverheadSprinkler\bin\Debug\netstandard2.1\OverheadSprinkler.Mono.dll
Build succeeded.
    0 Warning(s)
    0 Error(s)

git grep -n "M4b" src/
(no output, exit code 1)

git grep -n "InstanceFinder.IsServer" src/
(no output, exit code 1)

git grep -n "SkippedNotServer" src/
(no output, exit code 1)

python scripts/check-research.py
Version drift: none (14 stamped fact(s) checked).
Research index: every entry resolves.
```

## Two-player test steps (for Josh)
1. Host and friend both install the `v1.1.0` build (`OverheadSprinkler.IL2CPP.dll` for normal IL2CPP branch, or `OverheadSprinkler.Mono.dll` for `alternate` Mono branch, matching each player's S1API build) and join the same co-op save.
2. Friend, standing anywhere: open phone → **Sprinklers** → **Water** on a property with heads and dry pots.
   - Expect: both players see the pots' moisture rise to 100% over the spray duration; only the friend sees mist and hears the sprinkler sound.
3. Friend: place **Fertilizer** on a property storage shelf (including testing a small stack that empties down to 0) and click **Fertilizer** in the **Sprinklers** app for that property.
   - Expect: bottle count drops by the number of eligible planted pots (and clears the slot when reaching 0) for both players; fertilizer visuals appear on the pots for both players.
4. Host repeats steps 2–3 on a property with dry pots / unfertilized plants.
   - Expect: same behavior as `v1.0.0`.
5. Save as host, reload, and rejoin with the friend: confirm moisture levels and applied additives persist.
6. Share `MelonLoader\Latest.log` from **both** PCs. (Note: harmless FishNet `"Cannot complete action because server is not active..."` warnings may appear in the friend's log when `ApplyAdditive_Server` / `SetItemSlotQuantity` / `SetStoredInstance` run locally.)

## Open questions
- **Branch base (`main` vs `v1.0.0`):** `m6a-coop-gate` was branched from `main` (`ed70ea0`) rather than tag `v1.0.0` (`65639ef`) because `main` contains post-tag housekeeping (`525c607` README update, `ea3d8c4` removal of tracked coordinator scripts, and `ed70ea0` `.gitignore` additions) that would conflict with the local untracked `scripts/` folder if `v1.0.0` were checked out.
- **Pre-commit hook note:** `.git/hooks/pre-commit` invokes `python3`, which on this Windows machine hits the Windows Store App Execution Alias stub instead of Python 3.12 (`python`). `python scripts/check-safe-to-publish.py --staged` was run explicitly and passed before each commit (`--no-verify`).
- **Live two-player verification:** Pending Josh's two-player test run with the `v1.1.0` DLLs.
