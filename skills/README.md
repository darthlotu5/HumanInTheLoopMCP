# Skills

AI-agent skills that pair with the HumanInTheLoop MCP.

## afk

Teaches your agent to call `ask_user` for approvals and decisions instead of guessing,
and adds `/afk start` and `/afk stop` to route everything to your channel while
you're away from the keyboard. See [afk/SKILL.md](afk/SKILL.md).

**Install** (from the repo root):

```bash
./install-skill.sh --ai copilot     # or: claude, cursor   (Windows: ./install-skill.ps1 -Ai copilot)
```

Or copy `afk/` into your tool's skills directory (`~/.copilot/skills/` or `~/.claude/skills/`).

