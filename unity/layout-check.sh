#!/usr/bin/env bash
# One development build, the same 60 profiles as Godot 0.4.0's layout check.
# Usage: unity/layout-check.sh [log-directory]
# UNITY, SKIP_BUILD=1, LAYOUT_JOBS and LAYOUT_TIMEOUT (seconds per player) override defaults.
set -euo pipefail
here="$(cd "$(dirname "$0")" && pwd)"
out="${1:-$here/Build/layout-check}"
mkdir -p "$out"
out="$(cd "$out" && pwd)"
unity="${UNITY:-$HOME/Unity/Hub/Editor/6000.3.25f1/Editor/Unity}"
if [ -z "${SKIP_BUILD:-}" ]; then
  "$unity" -batchmode -nographics -quit -projectPath "$here" \
    -executeMethod Awakening.Editor.Builds.Linux -out Build/linux-dev -dev -logFile "$out/build.log" \
    || { grep -E 'error CS|Exception|Error' "$out/build.log" || true; exit 1; }
fi
jobs="${LAYOUT_JOBS:-1}"
if ! [[ "$jobs" =~ ^[1-9][0-9]*$ ]]; then
  echo "LAYOUT_JOBS must be a positive integer" >&2
  exit 2
fi
player="$here/Build/linux-dev/the-awakening.x86_64"
profiles=("1600 900 1 desktop" "1280 720 1.3 desktop" "1600 900 1.6 touch" \
          "844 390 1 touch" "844 390 1.6 touch" "844 390 2 touch")
# Workers have separate logs/results; only the parent totals the 60 fresh results.
run_case() {
  local name="$1" width="$2" height="$3" scale="$4" input="$5" scenario="$6" crowd="$7"
  local code=0 child
  local args=(--layout-check 1 --scenario "$scenario" --scale "$scale")
  [ "$crowd" = 0 ] || args+=(--crowd 1)
  [ "$input" = desktop ] || args+=(--touch 1)
  trap 'kill "${child:-}" 2>/dev/null || true; exit 143' INT TERM
  timeout "${LAYOUT_TIMEOUT:-120}" "$player" \
    -screen-width "$width" -screen-height "$height" -screen-fullscreen 0 \
    -logFile "$out/$name.log" "${args[@]}" >"$out/$name.stdout" 2>&1 &
  child=$!
  wait "$child" || code=$?
  trap - INT TERM
  if [ "$code" = 0 ] && grep -qE '^LAYOUT_CHECK_PASSED$' "$out/$name.log" && \
      ! grep -qE 'error CS|Exception|LAYOUT_CHECK_FAILED' "$out/$name.log"; then
    echo "PASS $name"
    echo PASS >"$out/$name.result"
  else
    echo "FAIL $name (exit $code): $out/$name.log"
    grep -E 'LAYOUT_CHECK|error CS|Exception' "$out/$name.log" || true
    echo FAIL >"$out/$name.result"
  fi
}
pids=()
names=()
trap 'for pid in "${pids[@]}"; do kill "$pid" 2>/dev/null || true; done; exit 143' INT TERM
for profile in "${profiles[@]}"; do
  read -r width height scale input <<< "$profile"
  for scenario in early mid late alignment awakening; do
    for crowd in 0 1; do
      name="${width}x${height}-${scale}-${input}-${scenario}-crowd${crowd}"
      names+=("$name")
      : >"$out/$name.result"
      run_case "$name" "$width" "$height" "$scale" "$input" "$scenario" "$crowd" &
      pids+=("$!")
      if [ "${#pids[@]}" -ge "$jobs" ]; then
        wait "${pids[0]}"
        pids=("${pids[@]:1}")
      fi
    done
  done
done
for pid in "${pids[@]}"; do wait "$pid"; done
trap - INT TERM
passed=0
failed=0
for name in "${names[@]}"; do
  if [ "$(cat "$out/$name.result")" = PASS ]; then
    passed=$((passed + 1))
  else
    failed=$((failed + 1))
  fi
done
echo "LAYOUT_MATRIX: $passed/60 passed; $failed failed. Logs: $out"
[ "$failed" = 0 ]
