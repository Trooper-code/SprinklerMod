# Overhead Sprinkler Design

**Date:** 2026-11-25  
**Status:** Approved  
**Target game version:** Schedule I v0.4.7f12 (Mono `alternate` first in M1–M3, IL2CPP added in M4)

## 1. Intent and Why

Players want an overhead watering system for Schedule I that keeps floors clear, attaches to the existing Suspension Rack (`FloorRack`) like a grow light, and waters every pot in a property with a single press—without having to walk up to and interact with each rack or pot individually. Later, the same system should also deliver fertilization/additives (`Fertilizer`, `PGR`, `Speed Grow`) from property storage.

## 2. Approaches Considered

### Trigger mechanism
1. **Phone App only via S1API `PhoneApp` (Picked):** Adds a "Sprinklers" app to the in-game phone, listing every owned property with at least one head installed and letting the player trigger watering (and later additives) from anywhere with one tap. Requires only one placeable item, and S1API handles phone registration and UI on both Mono and IL2CPP.
2. **Wall-Mounted Button only (`ToggleableSurfaceItem`):** Clones a wall-mountable `SurfaceItem` (`Surface.ESurfaceType.Wall`) that triggers all heads in that property when interacted with. Feels diegetic, but adds a second buyable item, shop entry, stand-in model, and surface save/load overhead.
3. **Phone App first, Wall Button later:** Ship Phone App in v1, keep `SprinklerController` decoupled so an optional Wall Button can call the same controller later. (If a physical switch is wanted post-v1, `SprinklerController` stays ready for it.)

### Chaining
1. **Automatic per property (Picked):** Every Overhead Sprinkler Head placed in the same `Property` is grouped automatically—no linking UI or wiring items required.
2. **Manual linking / zones:** Player explicitly links heads to a specific button or group. Rejected for v1 due to extra UI and persistence complexity.

### Fertilization / Additives supply
1. **Consume from property storage shelves (Picked):** Scans `PlaceableStorageEntity` / `StorageEntity` containers in the same property for the chosen additive (`Fertilizer`, `PGR`, `Speed Grow`), consumes 1 bottle per eligible `GrowContainer`, and applies it via `GrowContainer.ApplyAdditive_Server`. Matches the game's restocking loop without hand-pouring.
2. **Consume from player inventory:** Simpler, but cramped for large grows (Barn, Warehouse).
3. **Charge bank/cash automatically (or free):** Skips buying and stocking supplies.

## 3. Architecture

- **Item (`Overhead Sprinkler Head`):** Cloned from a vanilla grow light via `S1API.Items.Buildable.BuildableItemCreator.CloneFrom(...)`, inheriting `ProceduralGridItem`, `EProceduralTileType.Rack`, and `BuildStart_ProceduralGrid` / `BuildUpdate_ProceduralGrid` attachment to `FloorRack`.
  - To avoid mutating vanilla grow lights (since `CloneFrom` shares the source `BuiltItem` prefab), custom behavior is gated by `ItemInstance.ID == "overhead_sprinkler_head"` on spawned instances (M1), and moved to an independent cloned prefab in M2/M5.
- **Rack-to-Pot Lookup (`SprinklerController`):** Decoupled from any UI or hotkey trigger:
  1. Find all non-ghost `ProceduralGridItem`s in a `Property` whose `ItemInstance?.ID == "overhead_sprinkler_head"`.
  2. Resolve each head's parent `FloorRack` from `SyncAccessor_footprintTileMatches[0].tileParent`.
  3. Collect all non-ghost `GrowContainer`s (`Pot`, grow tents, `MushroomBed`) from `rack.ProceduralTiles` → `MatchedFootprintTile.MatchedStandardTile.BuildableOccupants` into a deduplicated `HashSet<GrowContainer>`.
  4. Water each unique `GrowContainer` using `ChangeMoistureAmount(...)` + `SyncMoistureData()` (guarded by `InstanceFinder.IsServer` in M1; networked via RPCs in M4).
- **Phone App (M2+):** S1API `PhoneApp` accessible from anywhere, showing every owned property that has ≥ 1 head (head count + pot count), with **Water** and **Water All** in M2, and **Fert / PGR / Speed Grow** buttons + status feedback in M3.

## 4. Milestones

1. **M1 — Proof of Concept (Mono):** Cloned head hangs on a Suspension Rack; `F7` spawns a head in inventory, `F8` waters every pot under all heads in the current property.
2. **M2 — Real Item + Phone App (Watering):** Own item ID, name, description, price, Oscar's Warehouse shop listing, save/load verified, and "Sprinklers" `PhoneApp` with per-property **Water** and **Water All**.
3. **M3 — Fertilization / Additives:** Property storage shelf consumption (`PlaceableStorageEntity` / `StorageEntity`) + **Fert / PGR / Speed Grow** controls and status feedback in the Phone App.
4. **M4 — Polish, Effects & IL2CPP:** 5-segment spray animation/particles/sound, `MelonPreferences` config, co-op multiplayer sync, and IL2CPP build support.
5. **M5 — Model & Release (`v1.0.0`):** Original overhead pipe/nozzle model, Mono + IL2CPP builds, complete README, packaged release zip.
- **Post-v1 (later):** Dedicated **Additive Reservoir** placeable item (stock additive stacks directly in it), optional **Wall-Mounted Button**, and **Automatic watering below a threshold**.
