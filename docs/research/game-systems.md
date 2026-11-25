# Schedule I game systems

How the parts of Schedule I this mod touches work under the hood. Every claim has a label (**confirmed**,
**unverified**, **wrong**) and its evidence. Class, method and field names only, no pasted game code.

## Public facts (starting point)
Gathered from public pages on 2026-11-19. These are player-facing facts, not code facts. All of them are
**unverified** until they're checked in game or in the code.

| Fact | Status | Source |
|------|--------|--------|
| The Big Sprinkler was added to the game in v0.4.5 (30 March 2026) | unverified | Steam patch notes v0.4.5, via SteamDB |
| Big Sprinkler: $1,000, Bagman III, 2×2 when placed; "will water up to 8 surrounding plants", which must be directly next to it or diagonal from it | unverified | Schedule I Fandom wiki, Items page |
| Pot Sprinkler: $200, Street Rat V, 1×2 when placed; waters the adjacent pot or grow tent | unverified | Schedule I Fandom wiki, Items page |
| Both sprinklers are activated by the player interacting with them | **confirmed** (code) / unverified in-game | `ScheduleOne.ObjectScripts.Sprinkler` (`IntObj`, `Hovered()`, `Interacted()`) |
| Botanists can't use the Pot Sprinkler or the Big Sprinkler | unverified | Steam forum post, ~April 2026 (not official) |
| Grow lights hang on the Suspension Rack, sold by Oscar at the Warehouse | confirmed (in-game observation) | Josh's screenshot, 2026-11-19; Dot Esports guide |
| BigSprinklerLogic (k0Mods) gave the Big Sprinkler logic before the game did; it's now deprecated | unverified | Thunderstore page |

## Suspension Rack attachment
- **Rack class:** `ScheduleOne.ObjectScripts.FloorRack` (inherits `GridItem`, implements `IProceduralTileContainer`) (**unverified** static detail; attachment confirmed live).
- **Attachment slots:** `FloorRack` exposes `public List<ProceduralTile> procTiles` via `IProceduralTileContainer.ProceduralTiles`. Each `ProceduralTile` (`ScheduleOne.Tiles.ProceduralTile`) has `TileType` (`EProceduralTileType.Rack`), `ParentBuildableItem` (pointing back to the `FloorRack`), `MatchedFootprintTile` (the rack's `FootprintTile` under that slot, whose `MatchedStandardTile` is the floor `Tile`), and `Occupants` (`List<ProceduralGridItem>`).
- **Attachment eligibility (not class-locked):** **confirmed** — cloning `fullspectrumgrowlight` with a new item ID (`overhead_sprinkler_head`) snaps onto Suspension Racks (`FloorRack`) in-game without mutating `GrowLight` checks [confirmed @ m1-poc | Schedule I, main install=v0.4.7f12 Alternate, MelonLoader=0.7.3, S1API Forked (ifBars)=3.2.3] (`MelonLoader/Latest.log` 2026-11-25). Statically, `BuildUpdate_ProceduralGrid.IsMatchValid(FootprintTile, ProceduralTile)` checks only:
  1. Distance between the item's `FootprintTile` and the rack's `ProceduralTile` is `< 0.01f`
  2. `matchedTile.Occupants.Count == 0`
  3. `matchedTile.TileType == ItemClass.ProceduralTileType` (both `EProceduralTileType.Rack`)
  There is **no** type check for `GrowLight`.
- **Rack destruction guard:** `FloorRack.CanBeDestroyed(out string reason)` blocks destruction if any `procTile` in `procTiles` has `Occupants.Count > 0 || OccupantTiles.Count > 0` (**unverified**, static decompilation).

## Grow light pot coverage
- **How `GrowLight` finds pots:** `GrowLight` does **not** use a trigger collider, raycast, or per-frame check (**unverified**, static decompilation). On placement (`GrowLight.InitializeProceduralGridItem`), it iterates `SyncAccessor_footprintTileMatches` (`List<CoordinateProceduralTilePair>`), resolves each `item.tile`, and calls `item.tile.MatchedFootprintTile.MatchedStandardTile.LightExposureNode.AddSource(usableLightSource, 1f)` (and `RemoveSource` in `GrowLight.Destroy()`).
- **Getting every pot under a rack:** **confirmed** — from any attached `ProceduralGridItem`, `sync___get_value_footprintTileMatches()[0].tileParent.GetComponent<FloorRack>()` resolves the parent rack, and iterating `rack.ProceduralTiles` → `procTile.MatchedFootprintTile.MatchedStandardTile.BuildableOccupants` (plus `rack.OwnerGrid` / `rack.CoordinatePairs` fallback) finds every `GrowContainer` under every rack with a head in the property (verified across 2 racks and 2 Moisture-Preserving Pots in the Barn) [confirmed @ m1-poc | Schedule I, main install=v0.4.7f12 Alternate, MelonLoader=0.7.3, S1API Forked (ifBars)=3.2.3] (`MelonLoader/Latest.log` 2026-11-25).

## Pot water
- **Base class:** Both `ScheduleOne.ObjectScripts.Pot` and `ScheduleOne.ObjectScripts.MushroomBed` inherit from `ScheduleOne.Growing.GrowContainer` (`: GridItem, IUsable, ITransitEntity`). Grow tents are `Pot` prefabs using `GrowContainer._lightSourceOverride` (`GetAverageLightExposure`) (**unverified**, static decompilation).
- **Fields and properties:**
  - `GrowContainer.MoistureCapacity` (`float`, **confirmed** `5.0` on Moisture-Preserving Pot in-game) [confirmed @ m1-poc | Schedule I, main install=v0.4.7f12 Alternate, MelonLoader=0.7.3, S1API Forked (ifBars)=3.2.3]
  - `GrowContainer._currentMoistureAmount` (`protected float`)
  - `GrowContainer.NormalizedMoistureAmount` (`public float`, `_currentMoistureAmount / MoistureCapacity`, range `0..1`, **confirmed**)
  - `GrowContainer._moistureDrainPerHour` (`private float`, default `1f`, **unverified**)
- **Draining:** `GrowContainer` subscribes `OnMinPass` to `NetworkSingleton<ScheduleOne.GameTime.TimeManager>.Instance.onMinutePass` (and `OnTimeSkipped` to sleep/time-skip). `DrainMoisture(int minutes)` calls `ChangeMoistureAmount((-_moistureDrainPerHour / 60f) * minutes)` (**unverified**, static decompilation).
- **Adding water & networking:** **confirmed** — calling `GrowContainer.ChangeMoistureAmount(container.MoistureCapacity)` + `GrowContainer.SyncMoistureData()` refills pots cleanly (`0% -> 100%` and `97% -> 100%` on Moisture-Preserving Pots in the Barn) [confirmed @ m1-poc | Schedule I, main install=v0.4.7f12 Alternate, MelonLoader=0.7.3, S1API Forked (ifBars)=3.2.3] (`MelonLoader/Latest.log` 2026-11-25). `SyncMoistureData()` sends `SetMoistureData_Server` → `SetMoistureData_Client` (**unverified** in multiplayer until M4).

## Vanilla sprinklers
All findings below are **unverified** (static decompilation of Mono `Assembly-CSharp.dll` v0.4.7f12).

- **Single class for both sprinklers:** Both Pot Sprinkler and Big Sprinkler use `ScheduleOne.ObjectScripts.Sprinkler` (`: GridItem`), differing by prefab settings (`TilesToWater`, `MinTilesToWater`, visuals).
- **Finding pots:** `Sprinkler.GetPots()` rotates each `Coordinate` in `TilesToWater` by `_rotation` around `_originCoordinate` on `OwnerGrid`, inspects `tile.BuildableOccupants` for `Pot`, counts how many target tiles each `Pot` occupies, and returns pots with `count >= MinTilesToWater`.
- **Activation & networking:** Uses `InteractableObject IntObj` (`Hovered()` sets `"Activate sprinkler"` when `!IsSprinkling`; `Interacted()` calls `SendWater()`). `SendWater()` (`[ServerRpc(RequireOwnership = false, RunLocally = true)]`) calls `Water()` (`[ObserversRpc(RunLocally = true)]`).
- **Watering routine, tank, and cooldown:** No water tank or supply limit exists. `Water()` sets `IsSprinkling = true`, plays `ClickSound`, `WaterSound`, and `WaterParticles`, then runs 5 segments over `ApplyWaterDelay` (default `6f`, so 1.2s per segment). On the server (`InstanceFinder.IsServer`), each segment calls `AddWater(1f / 5f)`, which calls `pot.ChangeMoistureAmount(pot.MoistureCapacity * 0.2f)` and `pot.SyncMoistureData()` on every pot from `GetPots()`. Afterward it waits `ParticleStopDelay` (`2.5f`), stops particles/sound, waits `Cooldown` (`float`), and sets `IsSprinkling = false`.
- **Reusable parts:** The `SendWater` / `Water` RPC pattern, 5-segment `AddWater` coroutine, and `InteractableObject` wiring can be reused directly on the overhead head (replacing `GetPots()` with our rack-tile lookup).

## Saving placed items
All findings below are **unverified** (static decompilation of Mono `Assembly-CSharp.dll` v0.4.7f12).

- **Save format:** `ProceduralGridItem.GetBaseData()` returns `ProceduralGridItemData(GUID, ItemInstance, 50, SyncAccessor_Rotation, FootprintMatchData[])`. Each `FootprintMatchData` stores `TileOwnerGUID` (the `FloorRack`'s `GUID`), `TileIndex` (index in `FloorRack.ProceduralTiles`), and `FootprintCoordinate`.
- **Load order & parent link:** `ProceduralGridItemLoader` (`LoadOrder => 100`) deserializes `ProceduralGridItemData`, loads `ItemInstance` via `ItemDeserializer.LoadItem(data.ItemString)`, resolves each `TileOwnerGUID` via `GUIDManager.GetObject<BuildableItem>(...)`, and calls `BuildManager.Instance.CreateProceduralGridItem(...)`.
- **Unknown item ID (mod removed):** `ItemDeserializer.LoadItem` delegates to `ItemLoader.LoadItem`, which calls `Registry.GetItem(itemData.ID)`. If `null`, it logs a warning (`"Failed to find item definition for ..."`) and returns `null`. `ProceduralGridItemLoader.LoadAndCreate` checks `if (itemInstance == null) return null;` and skips spawning the item without crashing or blocking the `FloorRack` from loading.

## Activating rack items
All findings below are **unverified** (static decompilation of Mono `Assembly-CSharp.dll` v0.4.7f12).

- **Vanilla state:** Neither `GrowLight` nor `FloorRack` has an `InteractableObject` component.
- **Interaction system:** `InteractionManager` (`ScheduleOne.Interaction`) runs `Physics.RaycastAll` and `Physics.SphereCastAll` (`RayRadius = 0.075f`, `MaxInteractionRange = 4f`) against `interaction_SearchMask` (`QueryTriggerInteraction.Collide`) from `PlayerCamera.Instance.transform`. `CheckHover()` looks up `GetComponentInParent<InteractableObject>()` on hit colliders and checks `Vector3.Distance(camera, hit.point) <= MaxInteractionRange` (`4f`), `CheckAngleLimit`, and `enabled`.
- **Conclusion:** Because a `GrowLight` already has colliders on the rack (used for right-click pickup/destroy at `rightClickRange = 5f`), adding an `InteractableObject` component to our cloned head prefab (with `MaxInteractionRange = 4f` and `onHovered`/`onInteractStart` wired) allows the player to look up at the head on the rack and press the interact key directly, provided the player is within 4 meters of the head's collider. If testing in-game shows the rack is mounted too high (> 4 m) or awkward to aim at, the fallback is attaching an `InteractableObject` to a downward-hanging pull/nozzle collider on the head prefab or adding an interaction to the `FloorRack`.

## S1API support
All findings below are **unverified** (inspected `github.com/ifBars/S1API` `stable` branch on 2026-11-19).

- **Cloning a grow light:** `S1API.Items.Buildable.BuildableItemCreator.CloneFrom(string sourceItemId)` looks up `Registry.GetItem(sourceItemId)` as `BuildableItemDefinition` and constructs `BuildableItemDefinitionBuilder(buildableDef)`, which copies `BuildableDefinition.BuiltItem = buildableSource.BuiltItem`. Because `BuiltItem` is the `GrowLight` (`ProceduralGridItem`) prefab with `BuildHandler = BuildStart_ProceduralGrid`, `ProceduralTileType = Rack`, and `CoordinateFootprintTilePairs`, a cloned item keeps the exact Suspension Rack attachment behavior.
- **Adding to Oscar's shop:** `S1API.Shops.ShopManager` provides `GetAllShops()`, `GetShopByName(string shopName)` (e.g. Oscar's Warehouse shop), and `Shop.AddItem(...)` (via `S1API.Shops.Shop` / `ShopIntegration`).
- **Pot / water wrappers:** `S1API.Growing` wraps `MushroomBedInstance`, `PlantInstance`, `SeedCreator`, `SeedDefinition`, `SeedInstance`, `ShroomColonyInstance`, and `GrowContainerAdditives`, but has **no** wrapper for `Pot` or `GrowContainer` moisture/watering. Our mod will call `GrowContainer.ChangeMoistureAmount` and `GrowContainer.SyncMoistureData` directly (with `#if MONOMELON` / `#if IL2CPPMELON` namespace aliases in M2+).
