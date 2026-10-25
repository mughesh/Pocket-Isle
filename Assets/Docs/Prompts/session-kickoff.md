# Session kickoff (use at the start of any new Claude Code session or account)

You're joining Pocket Isle mid-project. Another session built steps 1–7c. Don't change anything in this pass.

1. Read: `CLAUDE.md`, `.claude-handoff.md`, `Assets/Docs/BuildPlan.md`, `Assets/Docs/GameDesign.md`,
   `Assets/Docs/PolishBacklog.md`, and the latest prompt in `Assets/Docs/Prompts/` (highest step number).
2. Run `git status`, `git log --oneline -15`, `git tag`.
3. Check the tools work:
   - Unity bridge: with the Editor open, send `refresh` through `Temp/ClaudeBridge/cmd.txt` and read the log.
   - Meta tooling: confirm the Meta VR CLI / meta-quest agentic skills are available (list what you have).
   - `adb devices` (headset may be off; just report).
4. Reply in **at most 10 bullets**: current step and state, uncommitted files, the `pre-goa` tag position, which
   tools work or are missing, and anything in the docs that looks contradictory.
5. Then wait for the next prompt.

Rules that matter most (details in CLAUDE.md and the handoff):
- Small steps; one compile batch per section; never commit a broken compile.
- Commit locally after each working section. **No AI attribution lines** (no Co-Authored-By, no session links, no
  footers). **Never push** unless Mugesh says "push".
- Ask before installing packages, changing project settings, or deleting files.
- Short answers: tables and bullets, exact Unity menu paths, one-line Git Bash commands (forward-slash paths).
