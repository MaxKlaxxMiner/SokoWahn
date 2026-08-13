#!/bin/bash
# tools/umlaut.sh - findet und korrigiert ASCII-Ersatz (ae/oe/ue statt ä/ö/ü)
# in deutscher Prosa des Projekts (Doku, Skripte, Code-Kommentare).
# Übernommen aus dem atomuhr-Projekt (dort aus idle-rsa, dort aus vidplayer);
# Includes und Ausnahmen an SokoWahn angepasst (.md/.sh/.go/.mod/.cs).
# Dieses Script ist von Check UND Fix ausgenommen: sein Wörterbuch ist
# absichtlich voller Funde und würde sich sonst selbst zerstören.
# Ebenfalls ausgenommen (siehe EXCLUDES, PCRE - deshalb grep -P):
# - goSokoWahn/ (eingefrorener erster Go-Ansatz, wird nicht mehr angefasst)
# - oldstuff/ (historische C#-Originale von 2013) - AUSSER refcli/, das ist
#   das neue Konsolen-Orakel und gehört zur aktiven Codebasis
#
# Verwendung:
#   tools/umlaut.sh          Check: verdächtige Wörter melden (Exit 1 bei Fund)
#   tools/umlaut.sh --fix    bekannte Ersetzungen anwenden, danach Check
#
# Zweistufig:
# 1. Wörterbuch: kuratierte, gefahrlose Stamm-Ersetzungen (sed).
#    Bewusst NICHT pauschal: "uell" (aktuell/manuell!), "eiss" (Preissenkung!),
#    ss-Wörter allgemein (dass/muss/Prozess sind korrekt).
#    Zeilen mit URLs oder E-Mail-Adressen werden NIE automatisch angefasst
#    (github.com/mueller ...) - dort entscheidet ein Mensch. Ebenso Zeilen
#    mit '[sic]': damit markiert Prosa absichtlich zitierte Falschschreibung.
# 2. Verdachts-Scan: Wörter mit ae/oe/ue nach Konsonant (Diphthonge wie
#    "neue"/"Dauer" bleiben stumm), minus Whitelist legitimer Wörter.
#    Neue Funde: eindeutige Fälle ins Wörterbuch, legitime in die Whitelist.
#    "muesli" ist der Go-Modul-Autor github.com/muesli (termenv, cancelreader)
#    und steht deshalb in der Whitelist - go.mod-Modulpfade nie "korrigieren".

set -uo pipefail
cd "$(dirname "$0")/.."

# Pfade, die weder Check noch Fix anfassen (PCRE auf den Dateipfad)
EXCLUDES='tools/umlaut\.sh|/goSokoWahn/|/oldstuff/(?!refcli/)'

# --- Stufe 1: Ersetzungs-Wörterbuch (Stämme, Substring-Match gewollt) ---
# Der umschließende Block schaltet den Auto-Fix auf Zeilen mit URL/E-Mail ab.
SED_RULES='
\#(://|@|\[sic\])#!{
s/fuer/für/g;    s/Fuer/Für/g
s/ueber/über/g;  s/Ueber/Über/g
s/zurueck/zurück/g
s/rueck/rück/g;  s/Rueck/Rück/g
s/zueg/züg/g;    s/Zueg/Züg/g
s/gruend/gründ/g
s/fuell/füll/g;  s/\bmuell\b/müll/g;  s/\bMuell\b/Müll/g
s/fueg/füg/g
s/gueltig/gültig/g
s/muess/müss/g
s/wuensch/wünsch/g
s/uerde/ürde/g;  s/uerfe/ürfe/g;  s/uerft/ürft/g
s/spaet/spät/g;  s/Spaet/Spät/g
s/aehl/ähl/g
s/aeum/äum/g
s/aeng/äng/g;    s/laenger/länger/g
s/aend/änd/g;    s/Aend/Änd/g
s/aerts/ärts/g
s/aeufig/äufig/g
s/aechst/ächst/g
s/laeuf/läuf/g;  s/Laeuf/Läuf/g
s/haelt/hält/g
s/aetig/ätig/g
s/itaet/ität/g
s/anael/anäl/g
s/binaer/binär/g; s/Binaer/Binär/g
s/noetig/nötig/g
s/oeffn/öffn/g
s/oeglich/öglich/g
s/loes/lös/g;    s/Loes/Lös/g
s/oeher/öher/g;  s/hoech/höch/g;  s/oehe/öhe/g
s/koenn/könn/g
s/oenig/önig/g
s/uetz/ütz/g
s/uegbar/ügbar/g
s/pruef/prüf/g;  s/Pruef/Prüf/g
s/raet/rät/g;    s/Raet/Rät/g
s/zuverlaess/zuverläss/g
s/waehrend/während/g
s/fluessig/flüssig/g
s/praesent/präsent/g; s/Praesent/Präsent/g
s/kuenst/künst/g
s/uecke/ücke/g
s/aeusch/äusch/g
s/sprueng/sprüng/g; s/Sprueng/Sprüng/g
s/waer/wär/g
s/haett/hätt/g
s/schoen/schön/g
s/naemlich/nämlich/g
s/maessig/mäßig/g
s/uebl/übl/g;    s/uebrig/übrig/g
s/gross/groß/g;  s/Gross/Groß/g;  s/groess/größ/g;  s/Groess/Größ/g
s/heiss/heiß/g
s/schliess/schließ/g
s/ausser/außer/g; s/Ausser/Außer/g
s/staendig/ständig/g
s/laess/läss/g
s/stoer/stör/g;  s/Stoer/Stör/g
s/traeg/träg/g;  s/Traeg/Träg/g
s/oeffentlich/öffentlich/g
s/aechlich/ächlich/g
s/staerk/stärk/g; s/Staerk/Stärk/g
s/aetz/ätz/g
s/loeck/löck/g
s/faell/fäll/g;  s/Faell/Fäll/g
s/kuend/künd/g
s/tueck/tück/g
s/fuehr/führ/g;  s/Fuehr/Führ/g
s/frueh/früh/g;  s/Frueh/Früh/g
s/groeb/gröb/g
}
'

# --- Stufe 2: Verdachts-Scan ---
# ae/oe/ue nach Konsonant oder am Wortanfang (erfasst "fuer", "Ueber",
# "aendern"; ignoriert Diphthonge wie "neue", "genauer", "Dauer").
# "q" fehlt bewusst in der Konsonantenklasse: "ue" nach q ist immer legitim
# (Quelle, Frequenz, Query, sequentiell) - "qü" existiert im Deutschen nicht.
SUSPECT='([bcdfghjklmnprstvwxzBCDFGHJKLMNPRSTVWXZ](ae|oe|ue)|^(ae|oe|ue|Ae|Oe|Ue))'

# Legitime Wörter (Regex-Alternation, case-insensitiv geprüft).
# Englisch + deutsche uell-Familie + Go-Modul-Autor muesli + C#-Generics.
WHITELIST='true|values?|continues?|continued|issues?|due|cue|clue|blue|glue|hues?|sue|tissue|pursue|revenue|venue|avenue|rescue|argues?|argued|league|dialogue|vague|guests?|guessed|guesses|guess|does|doesn|doe|muesli|fluent|tvalue|zuerst|aktuell(e|en|er|es|em)?|manuell(e|en|er|es|em)?|eventuell(e|en|er|es|em)?|individuell(e|en|er|es|em)?|visuell(e|en|er|es|em)?|virtuell(e|en|er|es|em)?|punktuell(e|en|er|es|em)?'

GREP_INCLUDES=(--include='*.md' --include='*.sh' --include='*.go' --include='*.mod'
	--include='*.cs')

collect_files() {
	grep -rlE '(ae|oe|ue|Ae|Oe|Ue)' "${GREP_INCLUDES[@]}" . 2>/dev/null \
		| grep -vP "$EXCLUDES"
}

if [[ "${1:-}" == "--fix" ]]; then
	mapfile -t FILES < <(collect_files)
	for f in "${FILES[@]}"; do
		tmp="$(mktemp)"
		# -b (Binärmodus): CRLF-Dateien behalten ihre Zeilenenden. Ohne -b
		# normalisiert sed -i CRLF -> LF und erzeugt riesige Schein-Diffs
		# (gerade die C#-Dateien sind CRLF). Geschrieben wird nur, wenn sich
		# inhaltlich etwas ändert - unveränderte Dateien bleiben unberührt.
		sed -b -E "$SED_RULES" "$f" > "$tmp"
		if ! cmp -s "$f" "$tmp"; then
			cat "$tmp" > "$f"
		fi
		rm -f "$tmp"
	done
fi

# Verdachts-Scan: Fundstellen extrahieren, Whitelist-Wörter ausfiltern.
# CamelCase-Treffer (Kleinbuchstabe direkt vor Großbuchstabe im Wort) sind
# Code-Identifier (searchValueTrue, ...), keine deutsche Prosa.
RAW=$(grep -rnoE "[A-Za-zÄÖÜäöüß]*${SUSPECT}[A-Za-zäöüß]*" "${GREP_INCLUDES[@]}" . 2>/dev/null \
	| grep -vP "^[^:]*(${EXCLUDES})" \
	| grep -viE ":(${WHITELIST})\$" \
	| grep -vE ':[^:]*[a-z][A-Z][^:]*$' || true)

# Funde auf URL-/E-Mail-Zeilen verwerfen - die fasst auch der Auto-Fix
# nicht an, also gibt es dort nichts zu melden.
SUSPECTS=""
while IFS=: read -r file line word; do
	[[ -z "$file" ]] && continue
	src=$(sed -n "${line}p" "$file")
	[[ "$src" == *"://"* || "$src" == *"@"* ]] && continue
	[[ "$src" == *"[sic]"* ]] && continue
	SUSPECTS+="$file:$line:$word"$'\n'
done <<<"$RAW"
SUSPECTS="${SUSPECTS%$'\n'}"

if [[ -n "$SUSPECTS" ]]; then
	echo "Verdacht auf ASCII-Ersatz (ggf. Wörterbuch/Whitelist in tools/umlaut.sh erweitern):"
	echo "$SUSPECTS"
	exit 1
fi
echo "OK: kein ASCII-Ersatz gefunden"
