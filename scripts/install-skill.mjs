#!/usr/bin/env node
// Installs the HumanInTheLoop "afk" skill into an AI client and pre-approves the
// human-in-the-loop MCP so its ask_user tool never stops for a local prompt.
//
//   npx github:darthlotu5/HumanInTheLoopMCP --ai copilot            # this repo/folder
//   npx github:darthlotu5/HumanInTheLoopMCP --ai copilot --global   # all projects
//   npx github:darthlotu5/HumanInTheLoopMCP --ai copilot --afk      # also pre-approve file writes
//   npx github:darthlotu5/HumanInTheLoopMCP --ai claude
//   npx github:darthlotu5/HumanInTheLoopMCP --ai claude --global
import { existsSync, mkdirSync, rmSync, cpSync, readFileSync, writeFileSync } from "node:fs";
import { homedir } from "node:os";
import { join, dirname, resolve, normalize } from "node:path";
import { fileURLToPath } from "node:url";

const here = dirname(fileURLToPath(import.meta.url));
const skillSrc = join(here, "..", "skills", "afk");

const args = process.argv.slice(2);
const hasFlag = (name, short) => args.includes(name) || (short ? args.includes(short) : false);
function flagValue(name, short) {
  const i = args.findIndex((a) => a === name || (short && a === short));
  return i >= 0 ? args[i + 1] : undefined;
}

const ai = (flagValue("--ai", "-a") || args.find((a) => !a.startsWith("-")) || "copilot").toLowerCase();
const global = hasFlag("--global", "-g");
const afk = hasFlag("--afk"); // also pre-approve file writes so AFK work never blocks on a local prompt
const home = flagValue("--home") || homedir(); // --home overrides the home dir (used by tests)
const cwd = process.cwd();

if (!existsSync(skillSrc)) {
  console.error(`Skill source not found at ${skillSrc}`);
  process.exit(1);
}

function readJson(path, fallback) {
  try {
    return JSON.parse(readFileSync(path, "utf8"));
  } catch {
    return fallback;
  }
}

function writeJson(path, obj) {
  mkdirSync(dirname(path), { recursive: true });
  writeFileSync(path, JSON.stringify(obj, null, 2) + "\n");
}

function installSkill(skillsDir) {
  const dest = join(skillsDir, "afk");
  mkdirSync(dirname(dest), { recursive: true });
  if (existsSync(dest)) rmSync(dest, { recursive: true, force: true });
  cpSync(skillSrc, dest, { recursive: true });
  return dest;
}

// Copilot CLI keys tool approvals by git root (if the folder is a repo) or the
// normalized directory otherwise. Mirror that so the approval matches at runtime.
function locationKeyFor(dir) {
  let d = resolve(dir);
  for (;;) {
    if (existsSync(join(d, ".git"))) return d;
    const parent = dirname(d);
    if (parent === d) return normalize(resolve(dir));
    d = parent;
  }
}

if (ai === "copilot") {
  const skillsDir = global ? join(home, ".copilot", "skills") : join(cwd, ".github", "skills");
  const dest = installSkill(skillsDir);
  console.log(`\u2713 Installed the "afk" skill for Copilot CLI (${global ? "global / personal" : "this project"})`);
  console.log(`  ${dest}`);

  // Pre-approve human-in-the-loop/ask_user in ~/.copilot/permissions-config.json,
  // keyed to the current repo/folder so ask_user runs without the approval prompt.
  const permPath = join(home, ".copilot", "permissions-config.json");
  const key = locationKeyFor(cwd);
  const cfg = readJson(permPath, {});
  cfg.locations = cfg.locations || {};
  cfg.locations[key] = cfg.locations[key] || { tool_approvals: [] };
  cfg.locations[key].tool_approvals = cfg.locations[key].tool_approvals || [];
  const already = cfg.locations[key].tool_approvals.some(
    (a) => a.kind === "mcp" && a.serverName === "human-in-the-loop" && a.toolName === "ask_user",
  );
  if (!already) {
    cfg.locations[key].tool_approvals.push({
      kind: "mcp",
      serverName: "human-in-the-loop",
      toolName: "ask_user",
    });
    writeJson(permPath, cfg);
  }
  console.log(`\u2713 Pre-approved human-in-the-loop / ask_user for ${key}`);

  // --afk: the human-in-the-loop MCP can't intercept the CLI's own permission gates for built-in
  // tools, so file writes would still pop a local prompt and block an AFK session. Pre-approve them.
  if (afk) {
    if (!cfg.locations[key].tool_approvals.some((a) => a.kind === "write")) {
      cfg.locations[key].tool_approvals.push({ kind: "write" });
      writeJson(permPath, cfg);
      console.log(`\u2713 --afk: pre-approved file writes (create/edit) for ${key}`);
    }
    console.log("  For shell-command autonomy too, launch: copilot --allow-all-tools");
  }
} else if (ai === "claude") {
  const skillsDir = global ? join(home, ".claude", "skills") : join(cwd, ".claude", "skills");
  const dest = installSkill(skillsDir);
  console.log(`\u2713 Installed the "afk" skill for Claude Code (${global ? "global / user" : "this project"})`);
  console.log(`  ${dest}`);

  // Allow-list the whole MCP server in settings (project settings by default, user settings for --global).
  const settingsPath = global
    ? join(home, ".claude", "settings.json")
    : join(cwd, ".claude", "settings.json");
  const cfg = readJson(settingsPath, {});
  cfg.permissions = cfg.permissions || {};
  cfg.permissions.allow = cfg.permissions.allow || [];
  let changed = false;
  if (!cfg.permissions.allow.includes("mcp__human-in-the-loop")) {
    cfg.permissions.allow.push("mcp__human-in-the-loop");
    changed = true;
  }
  // --afk: let the agent edit files without a local prompt (the MCP can't intercept those gates).
  if (afk && cfg.permissions.defaultMode !== "acceptEdits" && cfg.permissions.defaultMode !== "bypassPermissions") {
    cfg.permissions.defaultMode = "acceptEdits";
    changed = true;
  }
  if (changed) writeJson(settingsPath, cfg);
  console.log(`\u2713 Allow-listed mcp__human-in-the-loop in ${settingsPath}`);
  if (afk) console.log('  --afk: set permissions.defaultMode = "acceptEdits" (edits won\'t prompt)');
} else {
  console.error(`Unknown client "${ai}". Use: --ai copilot | claude`);
  process.exit(1);
}

console.log("  Restart your AI client so it picks up the skill.");
