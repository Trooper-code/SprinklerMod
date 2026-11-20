# Report: M0-research

**Prompt:** `docs/prompts/M0-research.md`
**Branch:** `m0-research` (from tag `m0-base`) · **Last commit:** `f542b87`
**Status:** done

## Tool versions
| Tool | Version used | Matches HANDOFF.md? |
|------|--------------|---------------------|
| Schedule I, main install | v0.4.7f12 Alternate (on disk) | yes |
| Schedule I, Mono copy | v0.4.7f12 Alternate (`E:\Sprinkler\Schedule I Mono`) | yes |
| MelonLoader | Not yet installed in `E:\SteamLibrary\steamapps\common\Schedule I`; built `RackProbe` against `LavaGang.MelonLoader` `0.7.3` NuGet | **no: TODO in HANDOFF.md (Josh installs 0.7.x before running probe)** |
| S1API Forked (ifBars) | Inspected `github.com/ifBars/S1API` `stable` branch | yes (TODO in HANDOFF.md) |
| .NET SDK | `10.0.404` | yes (TODO in HANDOFF.md) |
| ilspycmd | `11.2.0.10050` | n/a (installed for M0) |

## What changed
- `docs/research/game-systems.md`: Filled in Suspension Rack attachment, Grow light pot coverage, Pot water, Vanilla sprinklers, Saving placed items, Activating rack items, and S1API support (`f542b87`)
- `docs/research/m1-design.md`: Wrote M1 Overhead Sprinkler Head design (`f542b87`)
- `docs/research/INDEX.md`: Updated M0 topic statuses and added `m1-design.md` (`f542b87`)
- `tools/probe/RackProbe.csproj`, `tools/probe/ProbeMod.cs`, `tools/probe/README.md`: Added read-only F9 diagnostic MelonLoader mod and run instructions (`f542b87`)
- `Directory.Build.props.user.example`: Added template for local `ScheduleIManagedDir` property (`f542b87`)

## Findings
| Finding | Status | Evidence |
|---------|--------|----------|
| Suspension Rack is `FloorRack : GridItem, IProceduralTileContainer` with `List<ProceduralTile> procTiles` (`TileType = Rack`) | unverified | `_decompiled/ScheduleOne.ObjectScripts/FloorRack.cs`, `ProceduralTile.cs` |
| Rack attachment is not class-locked to `GrowLight`; `BuildUpdate_ProceduralGrid.IsMatchValid` checks only distance `< 0.01f`, `Occupants.Count == 0`, and `ProceduralTileType == Rack` | unverified | `_decompiled/ScheduleOne.Building/BuildUpdate_ProceduralGrid.cs` (`IsMatchValid`) |
| `GrowLight` maps itself to floor tiles via `SyncAccessor_footprintTileMatches` → `ProceduralTile.MatchedFootprintTile.MatchedStandardTile.LightExposureNode` on placement; no colliders/raycasts/ticks | unverified | `_decompiled/ScheduleOne.ObjectScripts/GrowLight.cs` (`InitializeProceduralGridItem`, `Destroy`) |
| Every pot/bed under a rack can be collected via `rack.ProceduralTiles` → `MatchedFootprintTile.MatchedStandardTile.BuildableOccupants` (filtering `GrowContainer`) | unverified | `_decompiled/ScheduleOne.ObjectScripts/FloorRack.cs`, `ProceduralTile.cs`, `GrowContainer.cs` |
| `Pot` and `MushroomBed` both inherit `GrowContainer`; water uses `MoistureCapacity` (default `5f`), `NormalizedMoistureAmount`, `ChangeMoistureAmount`, and `SyncMoistureData` (`SetMoistureData_Server` / `SetMoistureData_Client`). Drain ticks on `TimeManager.Instance.onMinutePass` | unverified | `_decompiled/ScheduleOne.Growing/GrowContainer.cs`, `Pot.cs`, `MushroomBed.cs` |
| Both Pot Sprinkler and Big Sprinkler use `ScheduleOne.ObjectScripts.Sprinkler`, have no water tank, and run a 5-segment `AddWater(0.2f)` coroutine via `SendWater` (`ServerRpc`) → `Water` (`ObserversRpc`) | unverified | `_decompiled/ScheduleOne.ObjectScripts/Sprinkler.cs` |
| Removing a mod with a `ProceduralGridItem` does not break saves: `ItemLoader.LoadItem` returns `null` when `Registry.GetItem` misses, and `ProceduralGridItemLoader` skips spawning | unverified | `_decompiled/ScheduleOne.Persistence.Loaders/ProceduralGridItemLoader.cs`, `ItemDeserializer.cs`, `ItemLoader.cs` |
| `GrowLight` has no `InteractableObject`, but `InteractionManager` casts 4 m (`MaxInteractionRange = 4f`) against colliders; adding `InteractableObject` to the cloned head allows direct E-key activation within 4 m | unverified | `_decompiled/ScheduleOne.Interaction/InteractionManager.cs`, `InteractableObject.cs` |
| `S1API.Items.Buildable.BuildableItemCreator.CloneFrom` copies `BuiltItem` (keeping `ProceduralGridItem` rack-attach); `ShopManager` supports `GetShopByName` / `Shop.AddItem`; `S1API.Growing` has no `Pot`/`GrowContainer` water wrapper | unverified | `github.com/ifBars/S1API` (`stable`: `BuildableItemCreator.cs`, `BuildableItemDefinitionBuilder.cs`, `ShopManager.cs`) |

Written to: `docs/research/game-systems.md` and `docs/research/m1-design.md` (index updated in `docs/research/INDEX.md`). All static findings remain `unverified` until Josh runs `RackProbe.dll` and sends `Latest.log`.

## Tests
```
dotnet build tools/probe
  RackProbe -> E:\Sprinkler\S1-Overhead-Sprinkler\tools\probe\bin\Debug\net472\RackProbe.dll
Build succeeded.
    0 Warning(s)
    0 Error(s)
```
```
python scripts/check-research.py
Version drift: none (0 stamped fact(s) checked).
Research index: every entry resolves.
```
```
python scripts/check-safe-to-publish.py
Safe to publish: checked 19 file(s).
```

## Open questions
- Josh needs to install **MelonLoader 0.7.x** in `E:\SteamLibrary\steamapps\common\Schedule I` (currently clean, no `MelonLoader/` or `Mods/` folder), copy `tools/probe/bin/Debug/net472/RackProbe.dll` (or build `-c Release`) into `Mods/`, press **F9** in-game near a Suspension Rack with a grow light and pots, and share `MelonLoader\Latest.log` so static findings can be marked **confirmed**.
