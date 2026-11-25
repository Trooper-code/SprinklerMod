# Schedule I Modding Lessons (Mono + IL2CPP)

Practical playbook of confirmed technical lessons learned while building **Overhead Sprinkler** (`v1.0.0`) on **Schedule I v0.4.7f12**, **MelonLoader 0.7.3**, and **S1API.Forked 3.2.3**. Every pattern below was verified live in-game.

---

## 1. Dual-Target Mono + IL2CPP `.csproj` Setup

Schedule I ships on two Steam branches:
- **Normal branch (`None`):** IL2CPP (`net6.0`, `Il2CppScheduleOne.*` proxies in `MelonLoader\Il2CppAssemblies\`)
- **`alternate` beta branch:** Mono (`netstandard2.1`, `ScheduleOne.*` in `Schedule I_Data\Managed\`)

You can compile both DLLs from **one shared C# project** using `<TargetFrameworks>netstandard2.1;net6.0</TargetFrameworks>` and local git-ignored paths in `Directory.Build.props.user`:
- `ScheduleIManagedDir` → `<Schedule I Mono>\Schedule I_Data\Managed`
- `ScheduleIIl2CppDir` → `<Schedule I>\MelonLoader\Il2CppAssemblies` (generated after running MelonLoader once to the main menu)
- `ScheduleIMelonNet6Dir` → `<Schedule I>\MelonLoader\net6` (`Il2CppInterop.Runtime.dll`, `Il2CppInterop.Common.dll`)

Key `.csproj` settings:
- Define `MONO` on `netstandard2.1` and `IL2CPP` on `net6.0`, with separate `<AssemblyName>` (`<Mod>.Mono` vs `<Mod>.IL2CPP`).
- Set `<CopyLocalLockFileAssemblies>false</CopyLocalLockFileAssemblies>`.
- Set `<SuppressTfmSupportBuildWarnings>true</SuppressTfmSupportBuildWarnings>` and `<NoWarn>NU1603;NU1605;NU1701</NoWarn>` so `LavaGang.MelonLoader 0.7.3` + `S1API.Forked 3.2.3` restore cleanly across both `netstandard2.1` and `net6.0` with **0 warnings**.
- **Important:** The NuGet `S1API.Forked 3.2.3` package ships the `MonoMelon` build of `S1API.dll` (`lib\netstandard2.1\S1API.dll`). It works as a compile reference for both targets, **but at runtime on IL2CPP players must install `S1API.Il2Cpp.MelonLoader.dll`** from `ifBars/S1API` releases (never drop Mono `S1API.dll` into an IL2CPP `Mods/` folder).

---

## 2. IL2CPP Pitfalls and Verified Fixes

### 2.1 Wrapper Identity Bug (`HashSet<T>` / `Dictionary<T, ...>` on Unity Objects)
- **Trap:** Under `Il2CppInterop`, calling `TryCast<T>()` or reading a tile/list property allocates a **new managed wrapper** around the same native Unity object. Using `HashSet<GrowContainer>` or `HashSet<FloorRack>` fails to deduplicate—for example, a 2×2 `Pot` spanning 4 tiles was counted 8× (`Applied Fertilizer to 8 pots` for 1 pot).
- **Fix:** Always deduplicate Unity objects (`GameObject`, `Component`, `Property`, `FloorRack`, `GrowContainer`, `StorageEntity`) by **`obj.GetInstanceID()`** (`HashSet<int>` or `Dictionary<int, T>`).

### 2.2 Casting Game / IL2CPP Objects (`TryCast<T>`)
- **Trap:** Standard C# `is` / `as` operators only check managed inheritance, which can miss or fail across `Il2CppObjectBase` proxies in collections like `Tile.BuildableOccupants` (`GridItem`) or `Registry.GetAllItems()` (`ItemDefinition`).
- **Fix:** Use a small `GameCompat.TryCast<T>(object? obj)` helper:
  - Under `#if IL2CPP`: `where T : Il2CppObjectBase`, call `il2cppObj.TryCast<T>()`.
  - Under `#else`: `where T : class`, return `obj as T`.
- Avoid generic `Registry.GetItem<T>(id)` across runtimes; call `Registry.GetItem(id)` and pass the result to `GameCompat.TryCast<BuildableItemDefinition>(...)`.

### 2.3 Unity IL2CPP Bytecode Stripping (`ParticleSystem`)
- **Trap:** IL2CPP strips Unity property setters that the base game never calls directly. On `UnityEngine.ParticleSystemModule.dll` in Schedule I, `MainModule.loop`, `MainModule.playOnAwake`, `MainModule.duration`, `MainModule.gravityModifier`, `MainModule.simulationSpace`, `EmissionModule.enabled`, `ShapeModule.enabled`, `ColorOverLifetimeModule`, and 2-arg `MinMaxGradient(Color, Color)` are stripped or read-only.
- **Fix:** Keep the shared cross-runtime properties (`startLifetime`, `startSpeed`, `startSize`, `maxParticles`, 1-arg `new ParticleSystem.MinMaxGradient(color)`, `emission.rateOverTime`, `shape.shapeType`, `shape.angle`, `shape.radius`, `renderer.renderMode`, `renderer.sharedMaterial`) outside `#if`, and wrap the extra setters in `#if !IL2CPP`.

### 2.4 Custom `MonoBehaviour` Components on IL2CPP
- **Trap:** On IL2CPP, Unity won't instantiate or invoke lifecycle hooks (`OnEnable`, `Update`) on custom `MonoBehaviour` classes unless they are registered, have the `IntPtr` constructor, and expose lifecycle methods publicly—and managed reference fields (like `Action?`) require extra care.
- **Fix:**
  1. Annotate with `#if IL2CPP [MelonLoader.RegisterTypeInIl2Cpp] #endif`.
  2. Add `#if IL2CPP public MyHook(IntPtr ptr) : base(ptr) { } #endif`.
  3. Make `OnEnable()` / `Update()` **`public`** (not `private`).
  4. Avoid managed reference instance fields by delegating to a static active instance or static state.

### 2.5 Native Events & `List<T>` Types
- **Trap:** `ScheduleOne` exposes `Il2CppSystem.Collections.Generic.List<T>` on IL2CPP vs `System.Collections.Generic.List<T>` on Mono, and events like `Property.onBuildableItemAdded` use `Il2CppSystem.Action<BuildableItem>`.
- **Fix:**
  - Use `var` for local list variables returned by game properties/methods (`Property.OwnedProperties`, `property.GetBuildablesOfType<T>()`, `storage.ItemSlots`, `Registry.GetAllItems()`).
  - Subscribe native events with `#if IL2CPP prop.add_onBuildableItemAdded((Il2CppSystem.Action<BuildableItem>)Handler); #else prop.onBuildableItemAdded += Handler; #endif`.
  - For `UnityEngine.UI.Button` clicks, always use `S1API.Utils.EventHelper.AddListener(callback, button.onClick)`.

### 2.6 FishNet `SyncVar` Fields
- **Trap:** FishNet's IL post-processor turns `[SyncVar] public List<CoordinateProceduralTilePair> footprintTileMatches` into a broken C# property `SyncAccessor_footprintTileMatches` (`CS1545` error in Mono because its setter has 2 parameters).
- **Fix:** Read `head.sync___get_value_footprintTileMatches()` on Mono (`#else`) and `head.footprintTileMatches` on IL2CPP (`#if IL2CPP`).

---

## 3. S1API `PhoneApp` & `UnityEngine.UI` Layout Rules

- **Auto-discovery:** Any `public` non-abstract subclass of `S1API.PhoneApp.PhoneApp` with a public parameterless constructor is automatically instantiated and registered on the phone by S1API (`PhoneAppRegistry`).
- **Why `UIFactory` layouts clip off the screen:**
  - `UIFactory.TopBar` and `UIFactory.HorizontalLayoutOnGO` leave `HorizontalLayoutGroup.childControlWidth = false`, so Unity falls back to default `100px` child widths and overflows off the screen edges.
  - `UIFactory.ScrollableVerticalList` sets `Content` anchors `(0, 1)` to `(1, 1)` without zeroing `sizeDelta` / `offsetMin` / `offsetMax`, leaving a `100px` horizontal overflow (`50px` clipped off both left and right by the viewport `Mask`).
- **Fix:**
  1. Add a safe-area child under `container` (`offsetMin = (24, 18)`, `offsetMax = (-24, -18)`).
  2. Configure every `HorizontalLayoutGroup` / `VerticalLayoutGroup` explicitly with `childControlWidth = true`, `childControlHeight = true`, and explicit `LayoutElement` widths/heights (including `flexibleHeight = 0f` on fixed-height header/footer bars so they don't stretch vertically).
  3. Zero `_listContent.offsetMin`, `offsetMax`, `sizeDelta`, and `anchoredPosition` immediately after calling `UIFactory.ScrollableVerticalList(...)`.
- **No external PNG files needed:** Generate `IconSprite` (`64×64` `Texture2D` + `Sprite.Create`) and a 9-sliced `32×32` rounded-rectangle `Sprite` (`Sprite.Create(..., new Vector4(8, 8, 8, 8))`) in code so the mod distributes as a single `.dll`.

---

## 4. Cloning Items, Visuals, Shops, Audio, and Storage

- **Cloning without mutating vanilla items:** `S1API.Items.Buildable.BuildableItemCreator.CloneFrom("fullspectrumgrowlight")` copies `BuiltItem` by reference, so **never modify `BuiltItem` directly** (or every vanilla grow light changes too). Instead, attach per-instance visuals/components when `ItemInstance.ID == yourItemId` via `GameLifecycle.OnLoadComplete` + `Property.onBuildableItemAdded`.
- **URP-safe procedural attachments:** Clone `head.GetComponentInChildren<MeshRenderer>()?.sharedMaterial` (fallback `Shader.Find("Universal Render Pipeline/Lit")`) and set **both** `mat.color` and `mat.SetColor("_BaseColor", color)`. Destroy the default `Collider` on `GameObject.CreatePrimitive(...)` parts (requires `UnityEngine.PhysicsModule.dll`) so they never interfere with pickup or player movement.
- **Dynamic shop discovery:** `ShopManager.FindShopsByItem("fullspectrumgrowlight")` finds `"Oscar's Store"` reliably without hardcoding fragile scene paths; call `shop.AddItem(itemDef, price)` inside `GameLifecycle.OnLoadComplete` guarded by `!shop.HasItem(itemId)`.
- **Equalized spatial audio:** Extract `AudioClip`s from vanilla `Sprinkler.ClickSound` / `WaterSound`, route `AudioSource.outputAudioMixerGroup = Singleton<AudioManager>.Instance.MainGameMixer`, scale by `Singleton<AudioManager>.Instance.GetVolume(EAudioType.FX, scaled: true)`, cap simultaneous sources (`2` per cycle), and divide by `Mathf.Sqrt(activeSources)` so multi-head rooms don't stack loudly.
- **Property storage consumption:** `property.GetBuildablesOfType<PlaceableStorageEntity>()` → `StorageEntity.ItemSlots` → check `!slot.IsRemovalLocked && slot.Quantity > 0` → `slot.ChangeQuantity(-1)`.
- **Plant additive lifecycle:** Vanilla `Pot.OnPlantFullyHarvested()` calls `ClearAdditives()`. Always check `container.ContainsGrowable() && container.GetGrowthProgressNormalized() < 1f` and `container.CanApplyAdditive(additiveDef, out _)` before consuming an item and calling `container.ApplyAdditive_Server(additiveDef.ID)`.
