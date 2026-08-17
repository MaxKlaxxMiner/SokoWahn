#!/bin/bash
# Baut die Rooms-WinForms-App (FormDebugger, 2017-2019) mit dem .NET-Framework-Compiler
# (csc 4.0, C# 5 reicht - der Code nutzt keine neueren Sprachfeatures).
# Aufruf: ./build-rooms-forms.sh  (aus dem SokoWahn-Ordner heraus)
#
# Aufruf der fertigen Exe: sokorooms-forms.exe [level.txt]
# (Level-Dateien aus dem levelcache funktionieren direkt, Meta-Zeilen werden ignoriert;
#  ohne Argument starten die einkompilierten Test-Räume aus FormDebugger.CreateTestRooms)
#
# Die Forms laden keine .resources zur Laufzeit (kein ComponentResourceManager),
# daher entfällt die resx-Konvertierung, die build-winforms.sh noch brauchte.

set -e
cd "$(dirname "$0")"

CSC="/c/Windows/Microsoft.NET/Framework64/v4.0.30319/csc.exe"

# alle Quellen der Lib und der WinForms-App; das AssemblyInfo der Lib fliegt raus
# (doppelte Assembly-Attribute), SokoWahnTest wird nicht gebaut
sources=()
for f in SokoWahnLib/*.cs SokoWahnLib/Extras/*.cs \
         SokoWahnLib/Rooms/*.cs SokoWahnLib/Rooms/Filter/*.cs SokoWahnLib/Rooms/HashCrc/*.cs \
         SokoWahnLib/Rooms/Merger/*.cs SokoWahnLib/Rooms/QuickScan/*.cs \
         SokoWahnLib/Rooms/States/*.cs SokoWahnLib/Rooms/Tasks/*.cs SokoWahnLib/Rooms/Variants/*.cs \
         SokoWahnWin/*.cs SokoWahnWin/Properties/*.cs; do
  sources+=("$(cygpath -w "$f")")
done

"$CSC" -nologo -optimize+ -unsafe -codepage:65001 -target:winexe \
  -reference:System.dll -reference:System.Core.dll \
  -reference:System.Windows.Forms.dll -reference:System.Drawing.dll \
  -reference:System.Numerics.dll -reference:System.Xml.Linq.dll \
  "-out:$(cygpath -w sokorooms-forms.exe)" \
  "${sources[@]}"

echo "OK: sokorooms-forms.exe gebaut"

# Kopie im Hauptordner des Repos aktualisieren (wie bei den anderen Exes)
cp sokorooms-forms.exe ../sokorooms-forms.exe
echo "OK: Kopie im Hauptordner aktualisiert"
