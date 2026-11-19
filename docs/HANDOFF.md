# Overhead Sprinkler (Schedule I mod) handoff

## Setup
| What | Where |
|------|-------|
| Repo (local only, no remote) | `E:\Sprinkler` |
| Coding agent | Antigravity, Gemini 4 Hydrogen at Medium (rules: `AGENTS.md`) |
| Game install, normal IL2CPP branch (never committed, read-only) | TODO Josh, default `C:\Program Files (x86)\Steam\steamapps\common\Schedule I` |
| Mono copy of the game, for decompiling (never committed, read-only) | TODO Josh (see "Getting a Mono copy" below) |
| MelonLoader logs | `<game folder>\MelonLoader\Latest.log` |

**Getting a Mono copy:** in Steam, right-click Schedule I → Properties → Betas → pick `alternate`, and let it
update. Copy the whole game folder somewhere else (e.g. `E:\S1-modding\Schedule I Mono`), then switch back to
"None" if you want to keep playing on the normal branch. The agent reads
`<Mono copy>\Schedule I_Data\Managed\Assembly-CSharp.dll`.

## Tool versions
| Tool | Version |
|------|---------|
| Schedule I (normal branch) | TODO Josh (main menu corner; v0.4.7f12 was the latest seen publicly in Oct 2026) |
| Schedule I (Mono copy) | TODO Josh |
| MelonLoader | 0.7.x (exact: TODO Josh) |
| S1API Forked (ifBars) | 3.0.22 or newer (exact: TODO) |
| .NET SDK | TODO (agent records it in the M0 report) |

## How it works now
No code yet. The design is settled: an Overhead Sprinkler Head hangs on a Suspension Rack like a grow light, covers
every pot under that one rack, and waters them automatically below a threshold (`docs/DECISIONS.md` D1–D3). The
plan is to clone a grow light with S1API and reuse the vanilla sprinklers' water-adding code. Whether that works
is what M0 finds out. Research so far: `docs/research/INDEX.md`.

## Where it stops
Nothing to test yet.

## In progress
- Antigravity: `docs/prompts/M0-research.md` on branch `m0-research`.
  Report (when done): `docs/reports/M0-research.md` on that branch.

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
