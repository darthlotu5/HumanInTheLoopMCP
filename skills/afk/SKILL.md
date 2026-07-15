---
name: afk
description: Tells the agent I've stepped away from my keyboard. It should not block on the terminal waiting for me — I won't see it. If it needs anything from me (a question, clarification, choice, confirmation, decision, or approval) it should reach me through the human-in-the-loop MCP server's ask_user tool, which delivers the message to my phone and returns my reply so it can continue. This skill describes that MCP server and the tools it exposes and how to call them. Invoke `/afk` or `/afk start` when I step away and `/afk stop` when I'm back.
argument-hint: "start | stop"
license: MIT
metadata:
  author: Pius Dan
  version: "3.2.0"
---

# afk — reach me on my phone while I'm away from the keyboard

This skill doesn't change how you work or what you decide. It only changes **how you reach me**
when you need something: my phone instead of the terminal.

## Modes

### `/afk` or `/afk start`  →  I've stepped away from the keyboard
I'm no longer at the terminal, so a terminal prompt will just sit there — I won't see it.
Until I run `/afk stop`: if you need anything from me — a question, a clarification, a choice, a
confirmation, a decision, or **permission/approval to do something** — reach me by calling the
**`ask_user`** tool on the **`human-in-the-loop`** MCP server (described below). It delivers the
message to my phone and returns my reply, so you can carry on.

That includes permission: whenever you'd otherwise stop and ask me to approve an action (running a
command, editing or deleting files, deploying, pushing, spending), ask for that approval **through
the loop** with `ask_user` — e.g. `choices: ["Approve", "Reject"]` — rather than waiting on a local
terminal prompt I can't see. Proceed only if I approve.

### `/afk stop`  →  I'm back
I'm back at the keyboard. Ask me directly in the terminal again; no need to route through the MCP.

## The `human-in-the-loop` MCP server

A remote MCP server that relays messages between you and me while I'm away. You reach me by calling
its tools; each call blocks until I respond and returns what I said.

### Allow-list this MCP so reaching me never stops for a prompt

The **installer sets this up for you** — `npx github:darthlotu5/HumanInTheLoopMCP --ai copilot`
(or `--ai claude`) trusts this MCP for the current repo; add `--global` to trust it everywhere.
If you'd rather configure it by hand, here's what it does — it trusts **only this MCP**, nothing
else about how you run changes.

**GitHub Copilot CLI** — two separate things control this:

1. *Enable* the tools — in `~/.copilot/mcp-config.json`, put `"tools": ["*"]` on the server entry:
   ```json
   {
     "mcpServers": {
       "human-in-the-loop": {
         "type": "http",
         "url": "https://humanintheloop-mcp.azurewebsites.net/mcp",
         "headers": { "Authorization": "Bearer YOUR_TOKEN" },
         "tools": ["*"]
       }
     }
   }
   ```
2. *Pre-approve* the tool so it runs without the "Do you want to use this tool?" prompt.
   `tools: ["*"]` only *enables* the tool — approval is tracked separately, **per directory**, in
   `~/.copilot/permissions-config.json`. Either choose **"Yes, and don't ask again for tool
   'ask_user' from 'human-in-the-loop' in this directory"** once, or add the approval yourself under
   the directory you work from (a parent directory covers everything beneath it). Restart Copilot
   CLI after editing so it reloads:
   ```json
   {
     "locations": {
       "C:\\Users\\me": {
         "tool_approvals": [
           { "kind": "mcp", "serverName": "human-in-the-loop", "toolName": "ask_user" }
         ]
       }
     }
   }
   ```

**Claude Code** — in `.claude/settings.json` (project) or `~/.claude/settings.json` (global), add the
server to `permissions.allow`; the name with no tool suffix trusts every tool it exposes:
```json
{ "permissions": { "allow": ["mcp__human-in-the-loop"] } }
```

### Tool: `ask_user`

Ask me something and wait for my answer. The server sends the message to my phone (Telegram), waits
for my reply, and returns it to you — the choice I tapped, or the text I typed.

**Parameters**

| Parameter       | Type       | Required | Description |
| --------------- | ---------- | -------- | ----------- |
| `question`      | string     | yes      | The question or request to put to me. |
| `choices`       | string[]   | no       | Options for me to pick from (I tap one). Omit for a free-form answer. |
| `allowFreeform` | boolean    | no       | Whether I may type my own answer instead of picking a choice. Defaults to `true`. |
| `repository`    | string     | no       | Repo name for context, e.g. `Voxra.API` (from `git remote get-url origin` basename, or the folder). |
| `branch`        | string     | no       | Current git branch, e.g. `feature/billing` (from `git branch --show-current`). |
| `conversation`  | string     | no       | Short title of what we're working on in this session. |
| `context`       | string     | no       | One extra line about why you're asking or what happens next. |

**Returns:** my answer as a string — the choice I selected, or the free-form text I typed. (If I
don't reply before the server's timeout, it returns a message saying so.)

**Calling it well**

- Put the actual ask in `question`. For a decision between options, pass `choices` (e.g.
  `["Deploy", "Cancel"]`); for an open question, omit `choices`.
- Fill `repository`, `branch`, `conversation`, and `context` when you can — they make the message
  easy for me to answer at a glance on my phone.
- Then act on what I return, and carry on.
