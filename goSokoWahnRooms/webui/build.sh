#!/bin/bash
# Baut das Frontend-Bundle (TypeScript -> web/static/app.js + app.css) mit esbuild.
# Nur nötig, wenn am Frontend gearbeitet wird - das fertige Bundle ist eingecheckt,
# "go build" im Hauptmodul kommt ohne diesen Schritt aus.
# Aufruf: ./build.sh  (aus dem webui-Ordner heraus)

set -e
cd "$(dirname "$0")"

go run .
