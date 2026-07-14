#!/usr/bin/env bash
set -euo pipefail

AI="copilot"
while [ $# -gt 0 ]; do
  case "$1" in
    --ai) AI="${2:-}"; shift 2 ;;
    *) echo "Unknown argument: $1"; exit 1 ;;
  esac
done

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
SRC="$SCRIPT_DIR/skills/afk"
[ -d "$SRC" ] || { echo "Skill source not found: $SRC"; exit 1; }

case "$AI" in
  copilot) DEST="$HOME/.copilot/skills/afk" ;;
  claude)  DEST="$HOME/.claude/skills/afk" ;;
  cursor)  DEST="$HOME/.cursor/skills/afk" ;;
  *) echo "Unknown --ai '$AI' (use copilot|claude|cursor)"; exit 1 ;;
esac

mkdir -p "$(dirname "$DEST")"
rm -rf "$DEST"
cp -R "$SRC" "$DEST"
echo "Installed the afk skill to $DEST"
echo "Restart your agent so it picks up the new skill."

