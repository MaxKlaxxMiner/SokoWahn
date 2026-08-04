#!/bin/bash
# Baut die alte WinForms-App (Sokosolver) mit dem .NET-Framework-Compiler (csc 4.0)
# Aufruf: ./build-winforms.sh  (aus dem refcli-Ordner heraus)
#
# Ohne parallelDeaktivieren -> volle Parallel-Performance wie damals
# Die .resx-Dateien werden per ResxConv.exe (eigener Mini-resgen) eingebettet

set -e
cd "$(dirname "$0")/.."

CSC="/c/Windows/Microsoft.NET/Framework64/v4.0.30319/csc.exe"

# --- Schritt 1: resx -> resources ---
"$CSC" -nologo -out:"$(cygpath -w refcli/ResxConv.exe)" \
  -reference:System.dll -reference:System.Windows.Forms.dll \
  "$(cygpath -w refcli/ResxConv.cs)"

./refcli/ResxConv.exe "$(cygpath -w Form1.resx)" "$(cygpath -w refcli/Sokosolver.Form1.resources)"
./refcli/ResxConv.exe "$(cygpath -w Properties/Resources.resx)" "$(cygpath -w refcli/Sokosolver.Properties.Resources.resources)"

# --- Schritt 2: WinForms-App bauen (Dateiliste wie im Original-csproj, ohne SokowahnHash.cs) ---
sources=(Program.cs Form1.cs Form1.Designer.cs
  SokoWahnInterface.cs
  SokoWahn_2nd_generation.cs
  SokoWahn_3rd_generation.cs SokoWahn_3rd_generation_List2.cs
  SokoWahn_4th_generation.cs SokoWahn_4th_generation_ByteModus.cs
  SokoWahn_4th_generation_List2.cs SokoWahn_4th_generation_List2_ByteModus.cs
  SokoWahn_5th_generation.cs
  Properties/AssemblyInfo.cs Properties/Resources.Designer.cs Properties/Settings.Designer.cs
  SokowahnTools/*.cs ngMaxLite/*.cs)
winSources=()
for f in "${sources[@]}"; do
  [ "$f" = "SokowahnTools/SokowahnHash.cs" ] && continue
  winSources+=("$(cygpath -w "$f")")
done

"$CSC" -nologo -optimize+ -unsafe -codepage:65001 -target:winexe \
  -reference:System.dll -reference:System.Core.dll \
  -reference:System.Windows.Forms.dll -reference:System.Drawing.dll \
  "-resource:$(cygpath -w refcli/Sokosolver.Form1.resources)" \
  "-resource:$(cygpath -w refcli/Sokosolver.Properties.Resources.resources)" \
  "-out:$(cygpath -w refcli/sokosolver-forms.exe)" \
  "${winSources[@]}"

echo "OK: refcli/sokosolver-forms.exe gebaut"

# Max nutzt eine Kopie im Hauptordner des Repos -> bei jedem Build aktualisieren
if [ -f ../sokosolver-forms.exe ]; then
  cp refcli/sokosolver-forms.exe ../sokosolver-forms.exe
  echo "OK: Kopie im Hauptordner aktualisiert"
fi
