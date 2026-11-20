# RackProbe (M0 Diagnostic Mod)

Read-only MelonLoader mod for Schedule I (`alternate` Mono branch, v0.4.7f12). On pressing **F9**, it logs:
- Every Suspension Rack (`FloorRack`) in your current property
- Every item attached to each rack's `ProceduralTile` slots (e.g. `GrowLight`)
- Every pot / grow container (`Pot`, `MushroomBed`) underneath each rack, with its current and max water

It does **not** modify anything in the game.

## Build from source
1. Copy `Directory.Build.props.user.example` in the repo root to `Directory.Build.props.user` and set `<ScheduleIManagedDir>` to your Mono copy's `Schedule I_Data\Managed` folder.
2. Build:
   ```powershell
   dotnet build tools/probe -c Release
   ```
3. Output DLL: `tools/probe/bin/Release/net472/RackProbe.dll`

## How to run and what to send back
1. Make sure **MelonLoader 0.7.x** is installed in your main Schedule I folder (`E:\SteamLibrary\steamapps\common\Schedule I`, currently on the `alternate` Mono branch).
2. Copy `RackProbe.dll` into `<Schedule I>\Mods\`.
3. Launch Schedule I and load a save with at least one Suspension Rack, a grow light hanging on it, and pots (or grow tents / mushroom beds) underneath it—ideally with varying water levels.
4. Walk into that property and press **F9**.
5. Close the game (or alt-tab) and send back `<Schedule I>\MelonLoader\Latest.log` (or the `=== RackProbe Snapshot ===` section from it).
