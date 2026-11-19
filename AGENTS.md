# Rules for coding agents

You are the **worker** on this project. A coordinator (Claude) writes your prompts in `docs/prompts/`, reviews your
branch, and merges it after the user has tested it. Follow these rules on every task, plus whatever your prompt adds.

## Always
- Read the files your prompt lists under "Read first" before doing anything else.
- Work only on the branch your prompt names. Create it from the tag it names.
- **Never merge, rebase onto main, push to main, or create tags.** Leave the branch for review.
- Look things up in `docs/research/INDEX.md` before searching the docs or the code, and add a line there for every
  new research topic or section you create.
- Write every finding into the research docs (`docs/research/` or `RESEARCH.md`), in the section it belongs to,
  labeled **confirmed** (proven by a real run, test, capture or emulation), **unverified** (read from code or
  docs only) or **wrong** (an old claim that turned out false: fix it where it was written).
- Stamp every **confirmed** finding with what it was proven on, so it can be re-checked when tools change:
  `[confirmed @ <your branch> | <tool>=<version>, <tool>=<version>]`, using the tool names from the
  "Tool versions" table in `docs/HANDOFF.md`.
- Don't reopen a settled choice. Check `docs/DECISIONS.md` first; if new facts change the answer, say so in your
  report instead of changing course.
- Every claim cites its evidence: file, address/offset, log, capture or test name.
- Keep each file's existing line endings (CRLF stays CRLF, LF stays LF).
- Commit with the project's git identity. Small commits with clear messages.

## Never
- Commit copyrighted or third-party files: games, ROMs, saves, assets, proprietary binaries or code. Reference them
  by path; the user supplies their own.
- Touch real user data, real logs, real save files or real network ports in tests. Use copies and fixtures.
- Change files outside the repo, or install system-wide software, without the prompt saying so.

## Tool versions
Before you start, record the versions you're using (compiler, SDK, emulator, loaders, mods: whatever the project
lists under "Tool versions" in `docs/HANDOFF.md`). If any differ from that table, say so at the top of your report.
Version mismatches are a common hidden cause of bugs.

## Stop and report
Stop working and write your report (below) when any of these happen. A clear "here's what I know and where I'm
stuck" is more useful than more attempts.
- The same fix or approach has failed **3 times**.
- You need a file, tool, permission or piece of information you don't have.
- What you find **contradicts a fact marked confirmed** in the docs or your prompt.
- The task turns out much bigger than the prompt expected (e.g. it needs changes in areas the prompt didn't mention).
- Your prompt's own stop conditions are met.

## Your report
When you finish or stop, write `docs/reports/<MILESTONE>-<slug>.md` using the format in
`docs/reports/TEMPLATE.md`, check it with `python scripts/check-report.py <your report>`, commit it on your
branch, and reply with its path plus a 3-line summary. The coordinator reads
the report from your branch, so it must stand on its own.

## Project-specific rules: Schedule I mods
- **Never commit game files or decompiled game code.** That includes `Assembly-CSharp.dll`, anything from
  `Schedule I_Data/` or `MelonLoader/Il2CppAssemblies/`, extracted models and textures, and pasted method bodies
  from a decompiler. In the research docs, you may write class, method and field **names**, signatures, and a
  plain-English description of what the code does. Quote at most a few lines when the exact logic matters.
- Reference game DLLs from the user's install through a path in a local, git-ignored `Directory.Build.props.user`
  (see `docs/HANDOFF.md` → Setup). Never hard-code the user's paths in committed files.
- The game install is **read-only** for you. Never write into it, never launch it with your tools, and never
  install mods into it. The user runs the game and sends you the logs.
- Research against the **Mono** build (Steam beta branch `alternate`). Its `Assembly-CSharp.dll` decompiles to
  readable C#. IL2CPP proxy assemblies have no method bodies, so they're only for checking that names match.

## Agent-specific constraints: Antigravity
**How it loads rules.** Antigravity reads `AGENTS.md` from the workspace folder and its parents automatically
(confirmed against Antigravity's rules docs). If a `GEMINI.md` ever gets added, it must not contradict this file.

**Tools.** Use these when they fit, and say which one you used in your report:
- superpowers process skills: `systematic-debugging` before any fix, `test-driven-development` for new code,
  `writing-plans` when a prompt asks for a design, `verification-before-completion` before reporting done.
- `ilspycmd` (ILSpy command line): decompiles the Mono `Assembly-CSharp.dll` so you can read it. Write the output
  to a git-ignored folder (`_decompiled/`), never into the repo's tracked files.
- REA (Reverse Engineer Anything): only if a prompt needs IL2CPP-side checks. Read-only on game files; never use its
  debugger or process tools on the running game. Its output is **unverified** until proven in a real run.
- /swarm: not for this project unless a prompt says so explicitly. Its tasks are linear and small.

**Permission limits.** No network calls to live game services. Read-only on the game folder (see above).

**Quirks.**
- Long runs can drift from the prompt. Re-read your prompt's Goal and Stop conditions before writing the report.
- This repo is **local only**: no remote, no pushing, no PRs. Commit the report on your local branch. Don't only
  paste it in chat: the coordinator reads it from the branch.
