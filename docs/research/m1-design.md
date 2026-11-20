# M1 Design: Overhead Sprinkler Head (Proof of Concept)

Design for M1 based on M0 static research (mono `Assembly-CSharp.dll` v0.4.7f12 and `ifBars/S1API` `stable`). Class and method names are cited; no code is included.

## 1. Item creation & rack attachment
- Use `S1API.Items.Buildable.BuildableItemCreator.CloneFrom(...)` targeting a vanilla grow light item ID (e.g. `fullspectrumgrowlight` / `ledgrowlight` / `halogengrowlight` — exact ID confirmed from `RackProbe` output).
- Because `BuildableItemDefinitionBuilder` copies `BuildableDefinition.BuiltItem`, the cloned item inherits the grow light's `ProceduralGridItem` component, `BuildStart_ProceduralGrid` / `BuildUpdate_ProceduralGrid` build handler, `CoordinateFootprintTilePairs`, and `ProceduralTileType = EProceduralTileType.Rack`.
- **Important detail:** `CloneFrom` shares the source `BuiltItem` prefab reference by default. To avoid mutating vanilla grow lights, M1 will attach our sprinkler behavior dynamically when an instance is initialized whose `ItemInstance.ID` matches our mod's item ID (e.g. via a Harmony postfix on `ProceduralGridItem.InitializeProceduralGridItem` or `GrowLight.InitializeProceduralGridItem`), rather than modifying the shared `BuiltItem` prefab in place. (In M2/M5, we can clone the `BuiltItem` GameObject into a separate inactive prefab if we swap the model/remove `UsableLightSource`.)

## 2. Finding the rack's pots
- When our head is placed on a `FloorRack`, `ProceduralGridItem.SyncAccessor_footprintTileMatches[0].tileParent` holds the `FloorRack`'s `NetworkObject`.
- Calling `tileParent.GetComponent<FloorRack>()` gives the parent rack.
- To find every pot (and grow tent / mushroom bed) under that rack:
  1. Iterate `rack.ProceduralTiles`.
  2. For each `ProceduralTile`, read `procTile.MatchedFootprintTile?.MatchedStandardTile`.
  3. Collect every `GridItem` in `standardTile.BuildableOccupants` that is a `GrowContainer` (covers `Pot`, grow tents, and `MushroomBed`), deduplicated in a `HashSet<GrowContainer>` (since a single pot or bed can span multiple tiles).
- No range/radius or collider checks are needed; this matches D2 (one head covers every pot under its single `FloorRack`).

## 3. Activation (M1 vs M3)
- **M1 (Proof of concept):** A debug hotkey (e.g. `F8`) triggers watering on all placed Overhead Sprinkler Heads in the player's current property (or the nearest one), verifying placement, rack-to-pot lookup, and watering end-to-end.
- **M3 (Player interaction):** Add an `InteractableObject` (`ScheduleOne.Interaction.InteractableObject`) to the head's placed GameObject with `MaxInteractionRange = 4f`, `onHovered` setting `"Activate sprinkler"` when not currently sprinkling, and `onInteractStart` triggering watering—matching `Sprinkler.Hovered()` / `Sprinkler.Interacted()`.

## 4. Adding water
- Follow `Sprinkler.AddWater(float normalizedAmount)`'s pattern on each `GrowContainer` found under the rack:
  - Call `container.ChangeMoistureAmount(container.MoistureCapacity * normalizedAmount)` (in M1, `normalizedAmount = 1f` for instant full watering; in M3, 5 segments of `0.2f` over `ApplyWaterDelay` with particles/sound).
  - Call `container.SyncMoistureData()` so moisture updates replicate to clients via `SetMoistureData_Server` / `SetMoistureData_Client`.
- Guard with `InstanceFinder.IsServer` for multiplayer safety (expanded in M4 with `SendWater` / `Water` RPCs).

## 5. Risks & mitigations
- **Shared `BuiltItem` prefab:** `S1API`'s `CloneFrom` reuses `buildableSource.BuiltItem`. *Mitigation:* never mutate the shared prefab; gate custom behavior by `ItemInstance.ID` on spawned instances (or instantiate a separate prefab copy in M2+).
- **Grow light emission on the clone:** A cloned `GrowLight` still has `ToggleableLight` and `UsableLightSource`, so in M1 it will emit light onto the tile it's attached to. *Mitigation:* acceptable for M1 PoC; in M2/M5, disable/remove `GrowLight`'s light emission on our custom item.
- **4-meter interact reach in M3:** `InteractionManager.MaxInteractionRange` is `4f`. *Mitigation:* if a Suspension Rack is hung high enough that looking up at the head exceeds 4 m, extend a downward nozzle/pull-cord collider on the M5 model or fall back to interacting with the `FloorRack`.
- **Single vs multi-tile head footprint:** A grow light occupies specific `ProceduralTile`s on the rack, not necessarily all of them. *Mitigation:* we always look up `rack.ProceduralTiles` (the whole rack), not just `SyncAccessor_footprintTileMatches` (the head's own tiles), so one head waters the entire rack regardless of which slot it's snapped to.
