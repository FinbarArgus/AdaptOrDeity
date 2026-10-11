#!/usr/bin/env bash
# Builds the engine-agnostic sim/game assemblies (netstandard2.1) and copies them into the Unity package, with the
# modules' C library (aod_modules, built by tools/scripts/build_cell_library.py) they run on.
# Unity never compiles src/ itself; see docs/adr/0001-engine-agnostic-core.md.
set -euo pipefail

root="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
config="${1:-Release}"
dest="$root/unity/Packages/com.adaptordeity.sim/Plugins"

dotnet build "$root/src/AdaptOrDeity.Game.Session/AdaptOrDeity.Game.Session.csproj" \
    -c "$config" -f netstandard2.1 --nologo -v quiet

out="$root/src/AdaptOrDeity.Game.Session/bin/$config/netstandard2.1"
mkdir -p "$dest"
rm -f "$dest"/AdaptOrDeity.*.dll "$dest"/AdaptOrDeity.*.pdb "$dest"/AdaptOrDeity.*.xml
cp "$out"/AdaptOrDeity.*.dll "$out"/AdaptOrDeity.*.pdb "$dest"/
cells="$root/src/AdaptOrDeity.Native/bin/cells"
copied=0
for f in "$cells"/libaod_modules.so "$cells"/libaod_modules.dylib "$cells"/aod_modules.dll; do
    [[ -f "$f" ]] && cp "$f" "$dest"/ && copied=1
done
[[ $copied == 1 ]] || echo "warning: no module library in $cells: run tools/scripts/build_cell_library.py --build" >&2
echo "Copied $(ls "$dest"/AdaptOrDeity.*.dll | wc -l) assemblies to $dest"
