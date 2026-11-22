#!/usr/bin/env bash
# Export synthetic and golden-through-session batches; validate each against the real GTC contract.
set -euo pipefail
playtest_root="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")/.." && pwd)"
playtest_gtc="${1:-/mnt/1tb-p/Proyects/GTC-page/gtc-web}"
playtest_output="${2:-$(mktemp -d /tmp/awakening-playtest-batches.XXXXXX)}"
mkdir -p -- "$playtest_output"
playtest_output="$(cd -- "$playtest_output" && pwd)"
AWAKENING_PLAYTEST_BATCH_OUTPUT="$playtest_output" "${DOTNET:-$HOME/.dotnet/dotnet}" test "$playtest_root/core-tests/Tests" --filter 'FullyQualifiedName~PlaytestContractTests|FullyQualifiedName~PlaytestAlignment5Tests|FullyQualifiedName~PlaytestAlignment6Tests|FullyQualifiedName~GameSessionTests.GameCommandsEmitActualCostsAndPreserveGoldenOutcomes'
cd -- "$playtest_gtc"
for playtest_batch in "$playtest_output"/batch-*.json; do
    PYTHONDONTWRITEBYTECODE=1 uv run --script contracts/playtests/validate.py --json "$playtest_batch"
done
if PYTHONDONTWRITEBYTECODE=1 uv run --script contracts/playtests/validate.py --json "$playtest_output/invalid-awakening-1-scatter.json"; then
    echo 'ERROR: awakening-1 accepted scatter/silence' >&2
    exit 1
else
    playtest_result=$?
    if [[ "$playtest_result" -ne 1 ]]; then exit "$playtest_result"; fi
fi
printf 'Validated batches and expected rejection in %s\n' "$playtest_output"
