#!/bin/bash
# Baut die goSokoWahnBrute.exe und aktualisiert die Kopie im Hauptordner des Repos
# Aufruf: ./build.sh  (aus dem goSokoWahnBrute-Ordner heraus)

set -e
cd "$(dirname "$0")"

go build -o goSokoWahnBrute.exe .
cp goSokoWahnBrute.exe ../goSokoWahnBrute.exe

echo "OK: goSokoWahnBrute.exe gebaut und Kopie im Hauptordner aktualisiert"
