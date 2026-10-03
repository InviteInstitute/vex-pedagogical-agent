#!/usr/bin/env bash
# Install a Unity WebGL build, whatever it was named in Build Settings, where
# webgl/index.html loads it (build_out/ai-conversation-agent/), then swap it in so the
# live page never sees a half-copied build:
#
#   webgl/install_build.sh ~/Builds/Builder
#
# Accepts uncompressed builds and compressed ones with "Decompression Fallback" on
# (files ending in .unityweb), which plain nginx can serve as-is.
set -euo pipefail

SRC=${1:?usage: webgl/install_build.sh <unity-webgl-build-dir>}
NAME=ai-conversation-agent
OUT="$(cd "$(dirname "$0")" && pwd)/build_out"
DEST="$OUT/$NAME"

loaders=("$SRC"/Build/*.loader.js)
[ -f "${loaders[0]}" ] || { echo "No Build/*.loader.js in $SRC" >&2; exit 1; }
[ "${#loaders[@]}" -eq 1 ] || { echo "More than one loader in $SRC/Build" >&2; exit 1; }
base=$(basename "${loaders[0]}" .loader.js)

mkdir -p "$OUT"
rm -rf "$DEST.new"
mkdir -p "$DEST.new/Build"
cp "${loaders[0]}" "$DEST.new/Build/$NAME.loader.js"
for part in data framework.js wasm; do
  if [ -f "$SRC/Build/$base.$part" ]; then
    cp "$SRC/Build/$base.$part" "$DEST.new/Build/$NAME.$part"
  elif [ -f "$SRC/Build/$base.$part.unityweb" ]; then
    # Self-describing: the loader decompresses it in the browser.
    cp "$SRC/Build/$base.$part.unityweb" "$DEST.new/Build/$NAME.$part"
  else
    echo "Missing $base.$part in $SRC/Build. A .br/.gz build needs Decompression" \
      "Fallback turned on (Player Settings > Publishing Settings)." >&2
    rm -rf "$DEST.new"
    exit 1
  fi
done
if [ -d "$SRC/StreamingAssets" ]; then
  cp -r "$SRC/StreamingAssets" "$DEST.new/StreamingAssets"
fi
chmod -R a+rX "$DEST.new"

rm -rf "$DEST.old"
[ -d "$DEST" ] && mv "$DEST" "$DEST.old"
mv "$DEST.new" "$DEST"
rm -rf "$DEST.old"
echo "Installed $base from $SRC into $DEST"
