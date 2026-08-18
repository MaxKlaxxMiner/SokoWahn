#!/bin/bash
# Baut das Rooms-Vergleichs-CLI (C#-Orakel für den Go-Port): merged Raum-Gruppen
# im Original und dumpt Zustände/Varianten/BoxSwaps zum direkten Vergleich.
# Aufruf: ./build.sh  (aus dem roomscli-Ordner heraus), danach ./roomscli.exe
set -e
cd "$(dirname "$0")/.."

CSC="/c/Windows/Microsoft.NET/Framework64/v4.0.30319/csc.exe"
sources=("$(cygpath -w roomscli/roomscli.cs)")
for f in SokoWahnLib/*.cs SokoWahnLib/Extras/*.cs SokoWahnLib/Rooms/*.cs \
         SokoWahnLib/Rooms/Filter/*.cs SokoWahnLib/Rooms/HashCrc/*.cs \
         SokoWahnLib/Rooms/Merger/*.cs SokoWahnLib/Rooms/QuickScan/*.cs \
         SokoWahnLib/Rooms/States/*.cs SokoWahnLib/Rooms/Tasks/*.cs SokoWahnLib/Rooms/Variants/*.cs; do
  [[ "$f" == *AssemblyInfo* ]] && continue
  sources+=("$(cygpath -w "$f")")
done

"$CSC" -nologo -optimize+ -unsafe -codepage:65001 \
  -reference:System.dll -reference:System.Core.dll -reference:System.Numerics.dll \
  -reference:System.Drawing.dll -reference:System.Windows.Forms.dll -reference:System.Xml.Linq.dll \
  "-out:$(cygpath -w roomscli/roomscli.exe)" "${sources[@]}"
echo "OK: roomscli/roomscli.exe gebaut"
