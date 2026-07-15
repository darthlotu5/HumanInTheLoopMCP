# HumanInTheLoop MCP

> **Stop babysitting AI agents. Let them work in the background and only interrupt you when they need a human decision.**

HumanInTheLoop MCP is a remote MCP server that allows AI agents to pause, ask you a question, wait for your response, and continue automatically.

Instead of sitting in front of your terminal waiting for the next prompt, you can leave your agent running while you work, sleep, commute, or grab lunch. When the agent reaches a decision it cannot safely make, you'll receive a message on your phone and the agent will resume once you respond.

> 🔗 **Open source & self-hosted** — run your own server from GitHub: **[github.com/darthlotu5/HumanInTheLoopMCP](https://github.com/darthlotu5/HumanInTheLoopMCP)**

---

## Why?

AI coding agents are becoming capable of completing long-running tasks, but they still get stuck when they need clarification.

Examples:

* Should I deploy to production?
* Which implementation should I choose?
* Is it safe to delete these files?
* Which branch should I target?
* Should I refactor this code?

Normally the agent stops and waits for you.

With HumanInTheLoop MCP, it asks you wherever you are.

---

## How it works

```text
You start an AI task
        │
        ▼
Agent works for 20 minutes
        │
        ▼
Needs clarification
        │
        ▼
📱 Telegram

"Should I deploy to Production?"

        │
     You reply
        │
        ▼
Agent continues automatically
```

---

## Features

* ✅ Works with any MCP-compatible AI agent
* ✅ Simple `ask_user(...)` tool
* ✅ Telegram support out of the box
* ✅ Streamable HTTP / SSE
* ✅ Token authentication
* ✅ Self-hosted
* ✅ No database required for single-user deployments
* ✅ Extensible channel abstraction (`IHumanChannel`)
* ✅ Includes the AFK Skill for autonomous agents

---

## Repository Layout

```
src/
└── HumanInTheLoop.Mcp
    ├── Abstractions/      IHumanChannel, IRecipientResolver, shared models
    ├── HumanTools.cs      the ask_user tool
    └── ...                token authentication + MCP server wiring

samples/
└── telegram              Telegram IHumanChannel implementation

skills/
└── afk                   AI skill for Copilot / Claude Code
```

> The abstractions live in an `Abstractions/` folder **inside** the MCP project (namespace
> `HumanInTheLoop.Mcp.Abstractions`) — one project, no extra ceremony.

---

# Quick Start

## 0. Get the self-hosted server

Clone the open-source server from GitHub:

```bash
git clone https://github.com/darthlotu5/HumanInTheLoopMCP.git
cd HumanInTheLoopMCP
```

---

## 1. Create a Telegram Bot

Create a bot using **@BotFather** and copy the bot token.

---

## 2. Configure

```bash
cp samples/telegram/appsettings-example.json samples/telegram/appsettings.json
```

Configure:

| Setting          | Description                |
| ---------------- | -------------------------- |
| AccessToken      | Secret used by MCP clients |
| TelegramBotToken | Bot token from BotFather   |
| DefaultRecipient | Your Telegram chat id      |

To find your chat id:

1. Send a message to your bot.
2. Visit:

```
https://api.telegram.org/bot<YOUR_BOT_TOKEN>/getUpdates
```

3. Copy:

```
result[].message.chat.id
```

---

## 3. Run

```bash
cd samples/telegram

dotnet run
```

Your MCP endpoint will be available at

```
http://localhost:5000/mcp
```

---

## 4. Deploy

Deploy anywhere that supports .NET 10.

Examples:

* Azure App Service
* Azure Container Apps
* Docker
* Linux VM

No WebSockets are required.

---

# Connect Your AI Agent

Configure your MCP client to use your server.

```json
{
  "mcpServers": {
    "human-in-the-loop": {
      "type": "http",
      "url": "https://YOUR_SERVER/mcp",
      "headers": {
        "Authorization": "Bearer YOUR_ACCESS_TOKEN"
      }
    }
  }
}
```

Compatible with:

* GitHub Copilot
* Claude Code
* Cursor
* Any MCP client supporting Streamable HTTP

---

# The ask_user Tool

```text
ask_user(
    question,
    choices?,
    allowFreeform?,
    repository?,
    branch?,
    conversation?,
    context?
)
```

Example:

```
Should I deploy to Production?

[ Deploy ]

[ Cancel ]
```

Once you answer, the AI continues automatically.

---

# Bring Your Own Channel

Telegram is only the default implementation.

Create your own channel by implementing:

```csharp
public sealed class MyChannel : IHumanChannel
{
    public string ChannelId => "mychannel";

    public Task<string> AskAsync(
        Recipient recipient,
        HumanQuestion question,
        CancellationToken cancellationToken)
    {
        // Deliver the question
        // Wait for the response
    }
}
```

Register it:

```csharp
builder.Services.AddHumanInTheLoopMcp(builder.Configuration);

builder.Services.AddSingleton<IHumanChannel, MyChannel>();
```

Possible channels include:

* Slack
* Microsoft Teams
* Discord
* Email
* SMS
* Push notifications
* Web dashboard

---

# Install the AFK Skill

The included **AFK** skill teaches AI agents when they should ask for clarification instead of making assumptions, and adds `/afk start` and `/afk stop` to route everything to your channel while you're away from the keyboard.

Install it into your AI client with a single command — no clone required:

```bash
# GitHub Copilot CLI
npx github:darthlotu5/HumanInTheLoopMCP --ai copilot

# Claude Code
npx github:darthlotu5/HumanInTheLoopMCP --ai claude
```

This copies the skill into your client's skills directory (`~/.copilot/skills/afk` or `~/.claude/skills/afk`).

Prefer a script? From a clone of this repo:

```bash
./install-skill.sh --ai copilot   # or --ai claude
```

Or copy `skills/afk` into your AI client's skills directory manually.

After installation, **restart your AI client**.

---

# Security

* Keep your access token secret.
* Never commit `appsettings.json`.
* Rotate your Telegram bot token if compromised.
* Use environment variables in production.

---

# Roadmap

Planned features:

* Microsoft Teams
* Slack
* Discord
* Hosted HumanInTheLoop Cloud
* Multi-user support
* Organization management
* Approval workflows
* Audit history

---

# License

MIT
