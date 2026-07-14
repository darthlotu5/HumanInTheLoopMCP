---
name: afk
description: Toggle "away from keyboard" mode so Copilot routes every approval, confirmation, sign-off, decision, or clarifying question to me on Telegram via the human-in-the-loop MCP server instead of the local terminal prompt. Invoke `/afk` or `/afk start` when stepping away and `/afk stop` when back. Also use automatically for any approval before an irreversible or destructive action (deploys, git pushes, deletes, spending).
argument-hint: "start | stop"
license: MIT
metadata:
  author: Pius Dan
  version: "2.0.0"
---

# afk — route questions to Telegram while I'm away from the keyboard

Use this to switch WHERE Copilot asks me things: my phone (Telegram) vs. the terminal.

## Modes

### `/afk` or `/afk start`  →  I'M AWAY FROM THE KEYBOARD
I am now away from the terminal. For the rest of this session (until I run `/afk stop`):
- Send EVERY question, approval, confirmation, sign-off, choice, or decision to me by calling
  the **`ask_user` tool on the `human-in-the-loop` MCP server** (it reaches my phone on Telegram).
- NEVER use the built-in `ask_user` terminal prompt — I won't see it.
- Keep working autonomously between questions; only stop to ask when you genuinely need my input,
  and always before any irreversible or destructive action (production deploys, `git push`,
  deleting files/resources, spending money).
- Acknowledge in the terminal that AFK mode is ON.

### `/afk stop`  →  I'M BACK
Resume using the normal built-in prompt for questions and stop routing to Telegram. Acknowledge in
the terminal that AFK mode is OFF.

## How to ask while I'm away

Call the `human-in-the-loop` `ask_user` tool with rich context so the message is easy to answer at
a glance:
- `question`: the actual question or approval.
- `choices`: the options as an array. For a yes/no approval use `["✅ Approve", "❌ Reject"]`; if I
  gave options after a `|`, use exactly those; omit for an open-ended question.
- `allowFreeform`: `true`.
- `repository`: current repo name — from `git remote get-url origin` (basename) or the folder name.
- `branch`: current branch — from `git branch --show-current`.
- `conversation`: this session's title (a short description of what we're working on).
- `context`: one extra line of why you're asking / what happens next, when useful.

Then act on my reply: Approve/yes → proceed; Reject/no → stop and tell me what you were about to
do; free-form → follow my instruction. Report my decision back in the terminal.

## Rules
- While I'm away, ALL of my input goes through Telegram — never the terminal.
- Never take an irreversible or destructive action without approval obtained this way.
- If the `human-in-the-loop` server is genuinely unavailable, say so and fall back to the terminal.
