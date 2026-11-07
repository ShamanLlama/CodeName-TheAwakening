#!/usr/bin/env bash
# Builds a smaller Godot 4.7.2 web export template for The Awakening (no threads, release).
# The official template ships 3D, physics, navigation, XR, advanced GUI controls, SVG and a
# dozen import modules the game never uses; dropping them shrinks the download
# players wait for. The game draws everything in code, so it needs only GDScript, FreeType,
# RegEx, mbedtls (Crypto) and the advanced text server. The fallback text server was tried and
# misplaces glyphs in the game's period fonts (overlapping capitals), so shaping stays on
# HarfBuzz; only SIL Graphite is dropped. Check and slider icons are drawn by UiTheme, so the
# SVG module is not needed.
#
# Requirements: Godot source at the 4.7.2-stable tag, Emscripten 4.0.11 (the version the
# official 4.7.2 web templates use) and SCons. Defaults match the developer machine:
#   GODOT_SRC=~/build/godot-4.7.2  EMSDK=~/build/emsdk  SCONS=~/build/venv/bin/scons
# Output: ~/.local/share/godot/export_templates/4.7.2.stable/awakening/web_nothreads_release.zip,
# the custom release template the Web export preset points to.
set -euo pipefail

GODOT_SRC="${GODOT_SRC:-$HOME/build/godot-4.7.2}"
EMSDK="${EMSDK:-$HOME/build/emsdk}"
SCONS="${SCONS:-$HOME/build/venv/bin/scons}"
OUT_DIR="${OUT_DIR:-$HOME/.local/share/godot/export_templates/4.7.2.stable/awakening}"

test "$(git -C "$GODOT_SRC" describe --tags --exact-match 2>/dev/null)" = "4.7.2-stable" || {
	echo "Godot source at $GODOT_SRC is not the 4.7.2-stable tag" >&2
	exit 1
}
# shellcheck disable=SC1091
source "$EMSDK/emsdk_env.sh" >/dev/null
emcc --version | head -1 | grep -q "4.0.11" || { echo "Emscripten 4.0.11 is required" >&2; exit 1; }

cd "$GODOT_SRC"
"$SCONS" -j"$(nproc)" platform=web target=template_release threads=no \
	optimize=size lto=full \
	disable_3d=yes disable_advanced_gui=yes disable_xr=yes \
	disable_physics_2d=yes disable_physics_3d=yes disable_navigation_2d=yes disable_navigation_3d=yes \
	minizip=no brotli=no \
	modules_enabled_by_default=no \
	module_gdscript_enabled=yes module_freetype_enabled=yes module_regex_enabled=yes \
	module_mbedtls_enabled=yes module_text_server_adv_enabled=yes graphite=no

mkdir -p "$OUT_DIR"
cp bin/godot.web.template_release.wasm32.nothreads.zip "$OUT_DIR/web_nothreads_release.zip"
echo "Template: $OUT_DIR/web_nothreads_release.zip"
