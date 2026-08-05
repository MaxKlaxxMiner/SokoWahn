#!/bin/bash
# Baut die goSokoWahnBrute.exe und aktualisiert die Kopie im Hauptordner des Repos
# Aufruf: ./build.sh  (aus dem goSokoWahnBrute-Ordner heraus)

set -e
cd "$(dirname "$0")"

go build -o goSokoWahnBrute.exe .

# Reste früherer Läufe aufräumen (schlägt still fehl, solange dort noch Instanzen laufen)
rm -f ../goSokoWahnBrute.old*.exe 2>/dev/null || true

if cp goSokoWahnBrute.exe ../goSokoWahnBrute.exe 2>/dev/null; then
    echo "OK: goSokoWahnBrute.exe gebaut und Kopie im Hauptordner aktualisiert"
else
    # Ziel-exe läuft gerade: Windows erlaubt kein Überschreiben, aber ein Umbenennen -
    # die laufende Instanz arbeitet unter dem neuen Namen einfach weiter
    old="../goSokoWahnBrute.old$$.exe"
    mv ../goSokoWahnBrute.exe "$old"
    cp goSokoWahnBrute.exe ../goSokoWahnBrute.exe
    echo "OK: goSokoWahnBrute.exe gebaut und Kopie im Hauptordner aktualisiert"
    echo "    (laufende Instanz wurde nach ${old#../} verschoben und läuft dort weiter;"
    echo "     die Datei wird beim nächsten Build aufgeräumt)"
fi
