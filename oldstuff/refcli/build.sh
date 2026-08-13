#!/bin/bash
# Baut das Referenz-Orakel refcli.exe mit dem .NET-Framework-Compiler (csc 4.0)
# Aufruf: ./build.sh  (aus dem refcli-Ordner heraus)
#
# -define:parallelDeaktivieren -> serieller, deterministischer Lauf (vergleichbar mit Go-Port)
# -codepage:65001              -> Quellen sind UTF-8 (Umlaut-Identifier!)
# WinForms-Referenz nur wegen SokowahnBlockerB2.cs (benutzt System.Windows.Forms)
# Hinweis: der alte csc deutet "/" in Pfaden als Options-Trenner -> alle Pfade per cygpath nach "\" wandeln

set -e
cd "$(dirname "$0")/.."

CSC="/c/Windows/Microsoft.NET/Framework64/v4.0.30319/csc.exe"

sources=(refcli/Program.cs SokoWahnInterface.cs SokoWahn_4th_generation.cs SokowahnTools/*.cs ngMaxLite/*.cs)
winSources=()
for f in "${sources[@]}"; do
  # SokowahnHash.cs war auch im Original-csproj nicht enthalten (braucht fehlendes ngMax)
  [ "$f" = "SokowahnTools/SokowahnHash.cs" ] && continue
  winSources+=("$(cygpath -w "$f")")
done

"$CSC" -nologo -optimize+ -unsafe -codepage:65001 \
  -define:parallelDeaktivieren \
  -reference:System.dll -reference:System.Core.dll -reference:System.Windows.Forms.dll \
  "-out:$(cygpath -w refcli/refcli.exe)" \
  "${winSources[@]}"

echo "OK: refcli/refcli.exe gebaut"
