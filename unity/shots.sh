#!/usr/bin/env bash
# Builds a Linux development player and captures the screenshot scenarios, like the Godot harness.
# Usage: unity/shots.sh <out-dir> [scenario spec ...]   e.g.  "mid --select africa" "late --dialog menu"
set -euo pipefail
here="$(cd "$(dirname "$0")" && pwd)"
out="$1"; shift
mkdir -p "$out"
unity="${UNITY:-$HOME/Unity/Hub/Editor/6000.3.25f1/Editor/Unity}"
if [ -z "${SKIP_BUILD:-}" ]; then
  "$unity" -batchmode -nographics -quit -projectPath "$here" -executeMethod Awakening.Editor.Builds.Linux -out Build/linux-dev -dev -logFile "$out/build.log" \
    || { grep -E "error CS" "$out/build.log" | sort -u; exit 1; }
fi
specs=("$@")
[ ${#specs[@]} -eq 0 ] && specs=("start" "early" "mid --select africa" "late --select europe" "alignment" "awakening" "won" "lost" "mid --dialog menu" "mid --dialog doctrine" "mid --dialog guide" "mid --dialog chronicle")
for spec in "${specs[@]}"; do
  read -r -a parts <<< "$spec"
  name="$(echo "$spec" | tr ' -' '__' | tr -s _)"
  timeout 60 "$here/Build/linux-dev/the-awakening.x86_64" -screen-width "${WIDTH:-1600}" -screen-height "${HEIGHT:-900}" -screen-fullscreen 0 \
    -logFile "$out/$name.log" --shot "$out/$name.png" --scenario "${parts[@]}" >/dev/null 2>&1 || true
  errors=$(grep -cE "Exception" "$out/$name.log" || true)
  echo "$name: $( [ -f "$out/$name.png" ] && echo ok || echo MISSING ) exceptions=$errors"
done
