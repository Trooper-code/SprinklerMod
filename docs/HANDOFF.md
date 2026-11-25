# Overhead Sprinkler (Schedule I mod) handoff

## Setup
| What | Where |
|------|-------|
| Repo (local only, no remote) | `E:\Sprinkler\S1-Overhead-Sprinkler` (open this folder in Antigravity) |
| Coding agent | Antigravity, Gemini 4 Hydrogen at Medium (rules: `AGENTS.md`) |
| Main game install (never committed, read-only for the agent) | `E:\SteamLibrary\steamapps\common\Schedule I` (normal IL2CPP branch; `Il2CppAssemblies` generated 2026-11-25) |
| Mono copy of the game, for decompiling (never committed, read-only) | `E:\Sprinkler\Schedule I Mono` (copied 2026-11-19) |
| MelonLoader logs | `<game folder>\MelonLoader\Latest.log` |

**Getting a Mono copy:** in Steam, right-click Schedule I → Properties → Betas → pick `alternate`, and let it
update. Copy the whole game folder somewhere else (e.g. `E:\S1-modding\Schedule I Mono`), then switch back to
"None" if you want to keep playing on the normal branch. The agent reads
`<Mono copy>\Schedule I_Data\Managed\Assembly-CSharp.dll`.

## Tool versions
| Tool | Version |
|------|---------|
| Schedule I, main install | v0.4.7f12 Alternate (confirmed: main menu screenshot, 2026-11-19; switched to Mono branch 2026-11-25) |
| Schedule I, Mono copy | v0.4.7f12 Alternate (confirmed: main menu screenshot, 2026-11-19); Unity 2022.3.62f2 |
| MelonLoader | 0.7.3 |
| S1API Forked (ifBars) | 3.2.3 |
| .NET SDK | 10.0.404 |

## How it works now
M1–M3 and M4a (particles, equalized vanilla audio, 5-segment watering, `MelonPreferences`) are confirmed live on Mono. `OverheadSprinkler.csproj` now dual-targets both Mono (`netstandard2.1` → `OverheadSprinkler.Mono.dll`) and IL2CPP (`net6.0` → `OverheadSprinkler.IL2CPP.dll`) on branch `m4-polish`. Research so far: `docs/research/INDEX.md`.

## Where it stops
M4b IL2CPP build ready for live Windows testing on the normal IL2CPP Steam branch.

## In progress
- Antigravity: M4b dual Mono + IL2CPP build on branch `m4-polish`.

## Rules
- Claude coordinates, reviews, merges and tags. Antigravity researches and codes, and never merges.
- Josh tests on Windows. Merge and tag only after he confirms it works.
- The repo stays local (D4). Still never commit game files, decompiled code or extracted assets.
- Every finding goes into `docs/research/` labeled confirmed / unverified / wrong, with evidence.

## First thing to do
1. Read `docs/MILESTONES.md` and `docs/research/INDEX.md`.
2. Read the agent's report from its local branch: `git show m0-research:docs/reports/M0-research.md`.
3. Run `python scripts/check-report.py` on it, then check its tool versions against the table above.
4. Spot-check its class and field names against the decompiled Mono build, and check the probe log Josh sends.
5. List bugs, then gaps, then nitpicks.
6. If M0 is good: merge, tag `m0`, and write the M1 prompt from its design. If not: a follow-up prompt.

## Lessons
- The game updates often and breaks mods. Record the exact game version with every finding.

[Paste the coding agent's latest summary here, or leave this line if its report is in docs/reports/]

---
