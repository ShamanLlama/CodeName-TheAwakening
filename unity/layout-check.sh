#!/usr/bin/env bash
# One development build, the same 60 profiles as Godot 0.4.0's layout check.
# Usage: unity/layout-check.sh [log-directory]
# UNITY, SKIP_BUILD=1 and LAYOUT_TIMEOUT (seconds per player) override defaults.
set -euo pipefail
here="$(cd "$(dirname "$0")" && pwd)"
out="${1:-$here/Build/layout-check}"
mkdir -p "$out"
out="$(cd "$out" && pwd)"
unity="${UNITY:-$HOME/Unity/Hub/Editor/6000.3.25f1/Editor/Unity}"
if [ -z "${SKIP_BUILD:-}" ]; then
  "$unity" -batchmode -nographics -quit -projectPath "$here" \
    -executeMethod Awakening.Editor.Builds.Linux -out Build/linux-dev -dev -logFile "$out/build.log" \
    || { rg 'error CS|Exception|Error' "$out/build.log" || true; exit 1; }
fi
player="$here/Build/linux-dev/the-awakening.x86_64"
profiles=("1600 900 1 desktop" "1280 720 1.3 desktop" "1600 900 1.6 touch" \
          "844 390 1 touch" "844 390 1.6 touch" "844 390 2 touch")
passed=0
failed=0
for profile in "${profiles[@]}"; do
  read -r width height scale input <<< "$profile"
  for scenario in early mid late alignment awakening; do
    for crowd in 0 1; do
      name="${width}x${height}-${scale}-${input}-${scenario}-crowd${crowd}"
      args=(--layout-check 1 --scenario "$scenario" --scale "$scale")
      [ "$crowd" = 0 ] || args+=(--crowd 1)
      [ "$input" = desktop ] || args+=(--touch 1)
      code=0
      timeout "${LAYOUT_TIMEOUT:-120}" "$player" \
        -screen-width "$width" -screen-height "$height" -screen-fullscreen 0 \
        -logFile "$out/$name.log" "${args[@]}" >"$out/$name.stdout" 2>&1 || code=$?
      if [ "$code" = 0 ] && rg -q '^LAYOUT_CHECK_PASSED$' "$out/$name.log" && \
          ! rg -q 'error CS|Exception|LAYOUT_CHECK_FAILED' "$out/$name.log"; then
        echo "PASS $name"
        passed=$((passed + 1))
      else
        echo "FAIL $name (exit $code): $out/$name.log"
        rg 'LAYOUT_CHECK|error CS|Exception' "$out/$name.log" || true
        failed=$((failed + 1))
      fi
    done
  done
done
echo "LAYOUT_MATRIX: $passed/60 passed; $failed failed. Logs: $out"
[ "$failed" = 0 ]
