#!/usr/bin/env node
// Installs the HumanInTheLoop "afk" skill into an AI client's skills directory.
//   npx github:darthlotu5/HumanInTheLoopMCP --ai copilot
//   npx github:darthlotu5/HumanInTheLoopMCP --ai claude
import { existsSync, mkdirSync, rmSync, cpSync } from "node:fs";
import { homedir } from "node:os";
import { join, dirname } from "node:path";
import { fileURLToPath } from "node:url";

const here = dirname(fileURLToPath(import.meta.url));
const skillSrc = join(here, "..", "skills", "afk");

const args = process.argv.slice(2);
function flag(name, short) {
  const i = args.findIndex((a) => a === name || (short && a === short));
  return i >= 0 ? args[i + 1] : undefined;
}

const ai = (flag("--ai", "-a") || args.find((a) => !a.startsWith("-")) || "copilot").toLowerCase();
const base = flag("--dir") || homedir();

const dirs = {
  copilot: join(base, ".copilot", "skills", "afk"),
  claude: join(base, ".claude", "skills", "afk"),
  cursor: join(base, ".cursor", "skills", "afk"),
};

const dest = dirs[ai];
if (!dest) {
  console.error(`Unknown client "${ai}". Use: --ai copilot | claude | cursor`);
  process.exit(1);
}
if (!existsSync(skillSrc)) {
  console.error(`Skill source not found at ${skillSrc}`);
  process.exit(1);
}

mkdirSync(dirname(dest), { recursive: true });
if (existsSync(dest)) rmSync(dest, { recursive: true, force: true });
cpSync(skillSrc, dest, { recursive: true });

console.log(`\u2713 Installed the "afk" skill for ${ai}`);
console.log(`  ${dest}`);
console.log(`  Restart your AI client so it picks up the skill.`);
