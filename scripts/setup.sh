#!/usr/bin/env bash
set -euo pipefail
cd "$(dirname "$0")/.."
CREATURE_TOOLS="${CREATURE_TOOLS:-/workspace/toolchains}"
mkdir -p "$CREATURE_TOOLS/downloads"
archive="$CREATURE_TOOLS/downloads/Godot_v4.5.2-stable_mono_linux_x86_64.zip"
godot_dir="$CREATURE_TOOLS/godot/Godot_v4.5.2-stable_mono_linux_x86_64"
if [ ! -x "$godot_dir/Godot_v4.5.2-stable_mono_linux.x86_64" ]; then
  curl --fail --location --silent --show-error https://github.com/godotengine/godot/releases/download/4.5.2-stable/Godot_v4.5.2-stable_mono_linux_x86_64.zip -o "$archive"
  curl --fail --location --silent --show-error https://github.com/godotengine/godot/releases/download/4.5.2-stable/SHA512-SUMS.txt -o "$CREATURE_TOOLS/downloads/SHA512-SUMS.txt"
  python3 - "$CREATURE_TOOLS" <<'PY'
import hashlib,pathlib,sys,zipfile
root=pathlib.Path(sys.argv[1]); archive=root/'downloads/Godot_v4.5.2-stable_mono_linux_x86_64.zip'
entry=next(s for s in (root/'downloads/SHA512-SUMS.txt').read_text().splitlines() if s.endswith(archive.name))
if hashlib.sha512(archive.read_bytes()).hexdigest()!=entry.split()[0]: raise SystemExit('Godot SHA512 mismatch')
zipfile.ZipFile(archive).extractall(root/'godot')
PY
  chmod +x "$godot_dir/Godot_v4.5.2-stable_mono_linux.x86_64"
fi
if [ ! -x "$CREATURE_TOOLS/dotnet/dotnet" ]; then
  curl --fail --location --silent --show-error https://dot.net/v1/dotnet-install.sh -o "$CREATURE_TOOLS/downloads/dotnet-install.sh"
  bash "$CREATURE_TOOLS/downloads/dotnet-install.sh" --version 8.0.408 --install-dir "$CREATURE_TOOLS/dotnet" --no-path
fi
export DOTNET_ROOT="$CREATURE_TOOLS/dotnet"
export GODOT_BIN="$godot_dir/Godot_v4.5.2-stable_mono_linux.x86_64"
./scripts/run.sh --headless --import --editor --quit
