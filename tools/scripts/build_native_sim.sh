#!/usr/bin/env bash
# Builds the simulation as a NativeAOT shared library (src/AdaptOrDeity.Native -> aod_sim.so/.dll/.dylib) and copies it
# into the Unity package beside the managed assemblies. Unity uses it when it loads (Game.Session.Sessions), otherwise
# the managed simulation. NativeAOT needs a C toolchain: on Linux clang (CLANG=<path> to choose one) and zlib.
set -euo pipefail
root="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
rid="${RID:-linux-x64}"
out="$root/src/AdaptOrDeity.Native/bin/publish/$rid"
extra=()
if [[ -n "${CLANG:-}" ]]; then
    extra+=(-p:CppCompilerAndLinker="$CLANG")
    [[ -x "$(dirname "$CLANG")/llvm-objcopy" ]] && extra+=(-p:ObjCopyName="$(dirname "$CLANG")/llvm-objcopy")
fi
dotnet publish "$root/src/AdaptOrDeity.Native/AdaptOrDeity.Native.csproj" -c Release -r "$rid" -o "$out" --nologo -v quiet "${extra[@]}"
dest="$root/unity/Packages/com.adaptordeity.sim/Plugins"
mkdir -p "$dest"
cp "$out"/aod_sim.* "$dest"/ 2>/dev/null || true
rm -f "$dest"/aod_sim.pdb "$dest"/aod_sim.*dbg
echo "Built $(ls "$out"/aod_sim.so "$out"/aod_sim.dll "$out"/aod_sim.dylib 2>/dev/null | head -1) and copied it to $dest"
