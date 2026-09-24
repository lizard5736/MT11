#!/usr/bin/env bash
# Renders every Grip screen to PNG on Linux through Wine (developer tool).
# On Windows simply run:  Grip.exe --render-previews <folder>
#
# Needs: dotnet SDK 10, wine, xvfb, fonts-open-sans, python3 + fonttools.
# Usage: tools/preview/render.sh [output-folder]
set -euo pipefail
ROOT="$(cd "$(dirname "$0")/../.." && pwd)"
OUT="$(realpath -m "${1:-$ROOT/artifacts/previews}")"
WORK="$ROOT/artifacts/wine"
export WINEPREFIX="$WORK/prefix" WINEDEBUG=-all

mkdir -p "$OUT" "$WORK"
dotnet publish "$ROOT/src/Grip/Grip.csproj" -c Release -r win-x64 --self-contained true \
  -p:PublishSingleFile=false -o "$WORK/app" >/dev/null

if [ ! -f "$WINEPREFIX/grip-fonts.reg" ]; then
  xvfb-run -a wineboot -i >/dev/null 2>&1 || true
  python3 "$ROOT/tools/preview/wine_fonts.py" "$WINEPREFIX"
  wine regedit /S "$WINEPREFIX/grip-fonts.reg"
  wine reg add 'HKCU\Software\Microsoft\Avalon.Graphics' /v DisableHWAcceleration /t REG_DWORD /d 1 /f >/dev/null
  wineserver -k || true
fi

rm -f "$OUT"/*.png
xvfb-run -a -s "-screen 0 1600x1000x24" wine "$WORK/app/Grip.exe" --render-previews "Z:${OUT//\//\\}" || true
wineserver -k || true
grep -E "^(ok|FAIL)" "$OUT/render-log.txt"
