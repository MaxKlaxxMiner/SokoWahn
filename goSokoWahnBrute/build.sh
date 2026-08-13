#!/bin/bash
# Baut die goSokoWahnBrute-Binary und aktualisiert die Kopie im Hauptordner des Repos
# Aufruf: ./build.sh  (aus dem goSokoWahnBrute-Ordner heraus)
# Plattform-Erkennung: unter MSYS2/Windows entsteht goSokoWahnBrute.exe, unter Linux
# heißt die Binary schlicht "brute" - im Repo-Root ist der Name goSokoWahnBrute dort
# schon durch den Source-Ordner selbst belegt (Lehre vom ersten Server-Build: das
# fehlgeschlagene cp auf den Ordner schickte den kompletten Source nach .old$$).

set -e
cd "$(dirname "$0")"

case "$(uname -s)" in
    MINGW*|MSYS*|CYGWIN*) base="goSokoWahnBrute"; ext=".exe" ;;
    *)                    base="brute";           ext="" ;;
esac
bin="$base$ext"

go build -o "$bin" .

# Sicherung: die Root-Kopie muss eine normale Datei sein - nie ein Verzeichnis
# umbenennen oder überschreiben (egal was bei der Namenswahl schiefging)
if [ -e "../$bin" ] && [ ! -f "../$bin" ]; then
    echo "FEHLER: ../$bin existiert, ist aber keine normale Datei - Root-Kopie übersprungen"
    exit 1
fi

# Reste früherer Läufe aufräumen (schlägt still fehl, solange dort noch Instanzen laufen)
rm -f ../"$base".old*$ext 2>/dev/null || true

if cp "$bin" "../$bin" 2>/dev/null; then
    echo "OK: $bin gebaut und Kopie im Hauptordner aktualisiert"
else
    # Ziel-Binary läuft gerade: Windows erlaubt kein Überschreiben, Linux meldet
    # "Text file busy" - ein Umbenennen geht auf beiden, die laufende Instanz
    # arbeitet unter dem neuen Namen einfach weiter
    old="../$base.old$$$ext"
    mv "../$bin" "$old"
    cp "$bin" "../$bin"
    echo "OK: $bin gebaut und Kopie im Hauptordner aktualisiert"
    echo "    (laufende Instanz wurde nach ${old#../} verschoben und läuft dort weiter;"
    echo "     die Datei wird beim nächsten Build aufgeräumt)"
fi
