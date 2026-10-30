#!/usr/bin/env bash
set -euo pipefail
cd "$(dirname "$0")/.."
export DOTNET_ROOT="${DOTNET_ROOT:-/workspace/toolchains/dotnet}"
export PATH="$DOTNET_ROOT:$PATH"
export DOTNET_CLI_HOME="${DOTNET_CLI_HOME:-/workspace/.dotnet}"
export NUGET_PACKAGES="${NUGET_PACKAGES:-/workspace/.nuget}"
export XDG_DATA_HOME="${XDG_DATA_HOME:-/workspace/.local/share}"
export XDG_CONFIG_HOME="${XDG_CONFIG_HOME:-/workspace/.config}"
export XDG_CACHE_HOME="${XDG_CACHE_HOME:-/workspace/.cache}"
GODOT_BIN="${GODOT_BIN:-/workspace/toolchains/godot/Godot_v4.5.2-stable_mono_linux_x86_64/Godot_v4.5.2-stable_mono_linux.x86_64}"
dotnet build --nologo
exec "$GODOT_BIN" --path . "$@"
