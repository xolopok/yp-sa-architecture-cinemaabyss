#!/usr/bin/env bash
set -euo pipefail

if command -v plantuml >/dev/null 2>&1; then
  PUML=(plantuml)
else
  PUML=(java -jar /opt/plantuml.jar)
fi

find . -type f -name '*.puml' ! -name '_*.puml' -print0 | xargs -0 "${PUML[@]}"
