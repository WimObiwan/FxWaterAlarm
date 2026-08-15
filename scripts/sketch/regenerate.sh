#!/usr/bin/env bash
# Regenerate every drawing in Docs/_Tekeningen/.
# The output is deterministic: without script changes the SVGs stay identical.
set -euo pipefail
cd "$(dirname "$0")"
for f in draw_*.py; do
    echo "== $f"
    python3 "$f"
done
