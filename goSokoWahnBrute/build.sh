#!/bin/bash
# Baut die goSokoWahnBrute-Binary und aktualisiert die Kopie im Hauptordner des Repos
# Aufruf: ./build.sh  (aus dem goSokoWahnBrute-Ordner heraus)
# Plattform-Erkennung: unter MSYS2/Windows entsteht goSokoWahnBrute.exe,
# unter Linux (z.B. Ubuntu-Server) goSokoWahnBrute ohne Endung.

set -e
cd "$(dirname "$0")"

ext=""
case "$(uname -s)" in
    MINGW*|MSYS*|CYGWIN*) ext=".exe" ;;
esac
bin="goSokoWahnBrute$ext"

go build -o "$bin" .

# Reste früherer Läufe aufräumen (schlägt still fehl, solange dort noch Instanzen laufen)
rm -f ../goSokoWahnBrute.old*$ext 2>/dev/null || true

if cp "$bin" "../$bin" 2>/dev/null; then
    echo "OK: $bin gebaut und Kopie im Hauptordner aktualisiert"
else
    # Ziel-Binary läuft gerade: Windows erlaubt kein Überschreiben, Linux meldet
    # "Text file busy" - ein Umbenennen geht auf beiden, die laufende Instanz
    # arbeitet unter dem neuen Namen einfach weiter
    old="../goSokoWahnBrute.old$$$ext"
    mv "../$bin" "$old"
    cp "$bin" "../$bin"
    echo "OK: $bin gebaut und Kopie im Hauptordner aktualisiert"
    echo "    (laufende Instanz wurde nach ${old#../} verschoben und läuft dort weiter;"
    echo "     die Datei wird beim nächsten Build aufgeräumt)"
fi
