#!/bin/bash
# Baut die goSokoWahnRooms.exe und aktualisiert die Kopie im Hauptordner des Repos
# Aufruf: ./build.sh  (aus dem goSokoWahnRooms-Ordner heraus)

set -e
cd "$(dirname "$0")"

go build -o goSokoWahnRooms.exe .

# Reste früherer Läufe aufräumen (schlägt still fehl, solange dort noch Instanzen laufen)
rm -f ../goSokoWahnRooms.old*.exe 2>/dev/null || true

if cp goSokoWahnRooms.exe ../goSokoWahnRooms.exe 2>/dev/null; then
    echo "OK: goSokoWahnRooms.exe gebaut und Kopie im Hauptordner aktualisiert"
else
    # Ziel-exe läuft gerade: Windows erlaubt kein Überschreiben, aber ein Umbenennen -
    # die laufende Instanz arbeitet unter dem neuen Namen einfach weiter
    old="../goSokoWahnRooms.old$$.exe"
    mv ../goSokoWahnRooms.exe "$old"
    cp goSokoWahnRooms.exe ../goSokoWahnRooms.exe
    echo "OK: goSokoWahnRooms.exe gebaut und Kopie im Hauptordner aktualisiert"
    echo "    (laufende Instanz wurde nach ${old#../} verschoben und läuft dort weiter;"
    echo "     die Datei wird beim nächsten Build aufgeräumt)"
fi
