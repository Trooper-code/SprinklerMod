# M0-research: how racks, grow lights, pot water and sprinklers work

## Read first
- `AGENTS.md` (standing rules, including "Project-specific rules" and your agent-specific section)
- `docs/HANDOFF.md`, sections "Setup" and "Tool versions" (game paths and versions)
- `docs/MILESTONES.md`, rows M0 and M1
- `docs/research/INDEX.md`, then `docs/research/game-systems.md`
- `docs/DECISIONS.md` D1–D3 (the design this research must support; don't reopen them)

## Goal
Find out, with evidence, how Schedule I attaches grow lights to the Suspension Rack, how a grow light finds the pots
under it, and how the game adds water to a pot. Then write a short design for M1: a sprinkler head that hangs on a
rack and waters every pot under it. Prove the key facts with a log-only probe mod that Josh runs.

## Branch
`m0-research`, branched from tag `start` (the initial scaffold commit).

## Known facts
| Fact | Status | Evidence |
|------|--------|----------|
| Grow lights hang on the Suspension Rack in the vanilla game | confirmed | Josh's in-game screenshot, 2026-11-19 |
| The vanilla game has a Big Sprinkler that waters up to 8 pots next to it or diagonal from it, and a Pot Sprinkler that waters one adjacent pot or grow tent | unverified | Fandom wiki Items page |
| Both vanilla sprinklers are activated by the player; Botanists can't use them | unverified | wiki descriptions; Steam forum post |
| The Mono build's `Assembly-CSharp.dll` is at `<Mono copy>\Schedule I_Data\Managed\` and decompiles to readable C# | unverified | Schedule I modding docs (s1modding.github.io) |
| IL2CPP proxy assemblies are in `<game>\MelonLoader\Il2CppAssemblies\`; class names carry an `Il2Cpp` namespace prefix (e.g. `Il2CppScheduleOne.Persistence`) | unverified | Schedule I modding wiki example code |
| S1API has `BuildableItemCreator` with `CreateBuilder()` and `CloneFrom()` for placeable items | unverified | deepwiki page for ifBars/S1API, "Item registration and builders" |
| MelonLoader 0.7.x; S1API Forked 3.0.22 is the latest on Thunderstore | unverified | Thunderstore pages, 2026-11-19 |

## Tasks
1. **Set up.** Record your tool versions. Decompile the Mono `Assembly-CSharp.dll` with `ilspycmd` into
   `_decompiled/` (it's git-ignored). If the Mono copy path in HANDOFF.md is still "TODO", stop and report.
2. **Suspension Rack.** Find the rack's class(es). How do grow lights attach to it: slots, a child grid, attach
   points, a parent reference? What makes an item allowed to attach (a type check, a flag, a component, an item
   category)? How many items fit on one rack, and where?
3. **Grow light coverage.** How does a grow light decide which pots it affects (trigger collider, grid cells,
   raycast down, a list on the rack)? When does it check (every frame, on placement, on a tick)? How do I get
   "every pot under this rack" from the rack or from something attached to it?
4. **Pot water.** The class and fields for a pot's water level and maximum, how water drains, and the method that
   adds water. Is the change networked (FishNet RPC, SyncVar or similar)? Do grow tents (and mushroom beds, if
   cheap to check) use the same water code?
5. **Vanilla sprinklers.** How the Pot Sprinkler and Big Sprinkler find their pots and add water, how activation is
   wired, and whether either has a water tank or a cooldown. Which parts could the head reuse directly?
6. **Saving.** How placed buildables and rack attachments are saved and loaded (save classes, item ID, parent
   link). What happens on load when an item ID isn't known, e.g. after the mod is removed?
7. **Time ticks.** Which game-time event (per in-game minute or similar) the mod can subscribe to for a cheap
   periodic "any pot below the threshold?" check, instead of checking every frame.
8. **S1API.** Read the S1API source (github.com/ifBars/S1API). Can `BuildableItemCreator.CloneFrom` clone a grow
   light so the clone keeps the rack-attach behaviour? Can S1API add the item to Oscar's shop? What does it wrap for
   pots or water, if anything?
9. **IL2CPP check.** For each class you name in tasks 2–7, check it exists under the same name (with the `Il2Cpp`
   prefix) in the IL2CPP proxy assemblies. List any that differ.
10. **Probe mod.** Write a small log-only MelonLoader mod in `tools/probe/` (Mono target) that, on a hotkey, logs:
    every Suspension Rack in the current property, what's attached to each, the pots under each rack (by your
    task 3 method), and each pot's current and max water. It must **not change anything** in the game. Add a
    `tools/probe/README.md` with build steps and exactly what Josh should do and send back. Build it to confirm it
    compiles. Josh runs it; the coordinator uses his log to confirm or reject your findings.
11. **M1 design.** Write `docs/research/m1-design.md` (and add it to the index): how the head should be built (clone
    of which item), how it finds its rack's pots, how it adds water, and the risks. Plain English plus class and
    method names, no code.
12. Write every finding into `docs/research/game-systems.md` in its section, labeled, with evidence (decompiled
    class and method name). Static findings are **unverified** until the probe log confirms them.

## Tools
- `ilspycmd`: decompile the Mono `Assembly-CSharp.dll` for tasks 2–8. Installing it with
  `dotnet tool install -g ilspycmd` is allowed for this task.
- superpowers `writing-plans`: for the M1 design in task 11.
- superpowers `verification-before-completion`: before you report done, check every finding has its evidence and
  label, and that the probe builds.
- REA: only for task 9 if the proxy assemblies don't answer it. Read-only.
- Not /swarm: this is one linear trace.

## Task-specific constraints
- Research only, plus the probe mod. No mod code outside `tools/probe/`.
- The probe references game DLLs through a git-ignored `Directory.Build.props.user`. Commit a
  `Directory.Build.props.user.example` showing the property names with placeholder paths.
- Name things, don't paste them: class, method and field names and signatures are fine; method bodies are not.

## Stop and report if
- The AGENTS.md stop conditions are met, or:
- The Mono copy's path or version is missing from HANDOFF.md, or the Mono and IL2CPP game versions differ.
- Rack attachment turns out to be hard-wired to the grow light class in a way a clone can't use. Report what it
  checks, and stop before designing around it (that would reopen D1).

## Acceptance checks
- `dotnet build tools/probe` succeeds (paste the summary line).
- Every section in `docs/research/game-systems.md` that this prompt lists is filled in, with labeled findings and
  evidence, and `docs/research/INDEX.md` statuses are updated.
- `python scripts/check-research.py` and `python scripts/check-safe-to-publish.py` pass.
- No decompiled code or game files in any commit.

## Report
Write `docs/reports/M0-research.md` using `docs/reports/TEMPLATE.md`, commit it on the branch (local only, no
PR), and reply with its path plus a 3-line summary.
