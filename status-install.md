# Claude Code Statusline einrichten — Kontext-Balken

Anleitung zur Einrichtung einer custom Statusline für Claude Code, die die aktuelle
Kontextfenster-Nutzung als Fortschrittsbalken anzeigt:

```
[#####---------------] 127.58 k
```

- Balken = `used_percentage` des Kontextfensters (20 Zeichen breit)
- Zahl = aktuell belegte Input-Tokens in k (input + cache_creation + cache_read)
- Farbe: dunkles Cyan

Die Anleitung deckt **beide** Installationen ab:

1. **Native Windows** Claude Code → Config unter `C:\Users\<User>\.claude\`
2. **msys2/ucrt64** Claude Code → Config unter `$HOME/.claude/` (msys2-Home,
   z. B. `<msys2-root>\home\<User>\.claude\`)

Beide Installationen sind hart getrennt — das Script muss daher **zweimal**
abgelegt und in **beiden** `settings.json` eingetragen werden.

---

## Voraussetzungen

Das Script benötigt `bash`, `jq` und `awk`.

### msys2/ucrt64

```bash
pacman -S --needed jq
```

(`awk`/`gawk` und `bash` sind in msys2 bereits Teil der Basis-Installation.)

### Native Windows

Die native Claude-Code-Installation führt das Statusline-Command über die
konfigurierte Bash aus (`CLAUDE_CODE_GIT_BASH_PATH` bzw. Git Bash). `jq` muss
**in dieser Bash** verfügbar sein:

- Zeigt `CLAUDE_CODE_GIT_BASH_PATH` auf die msys2-Bash, reicht das `pacman`-Install oben.
- Bei klassischer Git Bash: `jq.exe` von <https://jqlang.org/download/> nach
  `C:\Program Files\Git\usr\bin\` (oder einen anderen Ordner im PATH) legen.

Test in der jeweiligen Bash:

```bash
echo '{"a":1}' | jq -r '.a'    # muss "1" ausgeben
```

---

## Schritt 1: Script anlegen

Folgenden Inhalt **unverändert** speichern als:

| Installation | Pfad |
|---|---|
| Native Windows | `C:\Users\<User>\.claude\statusline-command.sh` |
| msys2/ucrt64 | `$HOME/.claude/statusline-command.sh` (im msys2-Home) |

```bash
#!/usr/bin/env bash
# Claude Code statusLine: Kontext-Balken

input=$(cat)
used=$(echo "$input" | jq -r '.context_window.used_percentage // empty')
input_tok=$(echo "$input" | jq -r '.context_window.current_usage.input_tokens // 0')
cache_create=$(echo "$input" | jq -r '.context_window.current_usage.cache_creation_input_tokens // 0')
cache_read=$(echo "$input" | jq -r '.context_window.current_usage.cache_read_input_tokens // 0')

if [ -z "$used" ]; then
  exit 0
fi

# Aktuell im Kontext belegte Input-Tokens (konsistent zu used_percentage).
tokens_k=$(awk "BEGIN {printf \"%.2f\", ($input_tok + $cache_create + $cache_read) / 1000}")

# Fortschrittsbalken (20 Zeichen breit)
width=20
filled=$(echo "$used $width" | awk '{printf "%d", int($1 * $2 / 100 + 0.5)}')
empty=$((width - filled))
bar=""
for ((i=0; i<filled; i++)); do bar="${bar}#"; done
for ((i=0; i<empty; i++)); do bar="${bar}-"; done

# Dunkel Cyan
color='\033[0;36m'

printf "${color}[%s] %s k\033[00m" "$bar" "$tokens_k"
```

Danach ausführbar machen (in der msys2-Shell; für die native Installation ist
das `chmod` nicht zwingend, schadet aber nicht):

```bash
chmod +x ~/.claude/statusline-command.sh
chmod +x /c/Users/<User>/.claude/statusline-command.sh
```

> **Wichtig:** Unix-Zeilenenden (LF) verwenden, nicht CRLF — sonst schlägt der
> Shebang fehl. Bei Bedarf: `dos2unix <datei>` oder in Git
> `* text=auto eol=lf` beachten.

---

## Schritt 2: settings.json erweitern

In **beiden** `settings.json` den `statusLine`-Block ergänzen (vorhandene
Einträge unberührt lassen, nur den Key hinzufügen):

### Native Windows — `C:\Users\<User>\.claude\settings.json`

```json
{
  "statusLine": {
    "type": "command",
    "command": "C:/Users/<User>/.claude/statusline-command.sh"
  }
}
```

> Forward-Slashes im Pfad verwenden — auch unter Windows.

### msys2/ucrt64 — `$HOME/.claude/settings.json`

```json
{
  "statusLine": {
    "type": "command",
    "command": "/home/<User>/.claude/statusline-command.sh"
  }
}
```

> Absoluten POSIX-Pfad eintragen (`echo $HOME` in der ucrt64-Shell verrät den
> genauen Pfad), nicht `~` — Tilde-Expansion ist hier nicht garantiert.

Existiert noch keine `settings.json`, einfach mit genau diesem Inhalt anlegen.

---

## Schritt 3: Testen

### Manueller Test (ohne Claude Code)

Das Script bekommt von Claude Code ein JSON auf stdin. Simulieren:

```bash
echo '{"context_window":{"used_percentage":25,"current_usage":{"input_tokens":10000,"cache_creation_input_tokens":5000,"cache_read_input_tokens":35000}}}' \
  | bash /pfad/zum/statusline-command.sh
```

Erwartete Ausgabe (in Cyan):

```
[#####---------------] 50.00 k
```

Liefert das Script nichts: prüfen ob `jq` in der verwendeten Bash im PATH ist.

### Im echten Betrieb

Claude Code (neu) starten — die Statusline erscheint unten, sobald die erste
Antwort generiert wurde. Sie aktualisiert sich bei jeder Nachricht.

---

## Troubleshooting

| Symptom | Ursache / Lösung |
|---|---|
| Keine Statusline sichtbar | `claude` komplett neu starten; Pfad in `settings.json` prüfen (Tippfehler, Slashes) |
| Statusline leer | `jq` fehlt in der Bash, die das Script ausführt → Voraussetzungen prüfen |
| `bad interpreter`-Fehler | CRLF-Zeilenenden → `dos2unix` aufs Script anwenden |
| Script läuft manuell, aber nicht in Claude Code | Native Installation nutzt evtl. eine andere Bash als die Test-Shell → `CLAUDE_CODE_GIT_BASH_PATH` in `settings.json` prüfen |
| Falsche `settings.json` bearbeitet | Native Windows liest `C:\Users\<User>\.claude\`, die msys2-Variante `$HOME/.claude/` im msys2-Home — das sind **zwei verschiedene Verzeichnisse** |

---

## Referenz: Eingabe-JSON

Claude Code übergibt dem Statusline-Command per stdin u. a.:

```json
{
  "context_window": {
    "used_percentage": 13.79,
    "current_usage": {
      "input_tokens": 2580,
      "cache_creation_input_tokens": 11000,
      "cache_read_input_tokens": 14000
    }
  }
}
```

Das Script nutzt nur diese Felder. Weitere verfügbare Felder (Modellname,
cwd, Git-Branch, Kosten …) sind in der offiziellen Doku beschrieben:
<https://code.claude.com/docs/en/statusline>
