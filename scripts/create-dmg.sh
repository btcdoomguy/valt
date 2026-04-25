#!/bin/bash
set -e

echo "=== Valt DMG Creation Script ==="
echo ""

# 1. Check if Valt.app exists
if [ ! -d "Valt.app" ]; then
    echo "[1/4] Valt.app not found, running package-macos.sh..."
    ./scripts/package-macos.sh
else
    echo "[1/4] Valt.app found, skipping package-macos.sh"
fi
echo ""

# 2. Remove old DMG if it exists
echo "[2/4] Removing old DMG if exists..."
rm -f Valt-macos-arm64.dmg
echo "Removed old Valt-macos-arm64.dmg"
echo ""

# 3. Check if create-dmg is available
if ! command -v create-dmg &> /dev/null; then
    echo "Warning: create-dmg not found. Installing via brew..."
    brew install create-dmg || {
        echo "Failed to install create-dmg. Please install manually:"
        echo "  brew install create-dmg"
        exit 1
    }
fi

# 4. Use create-dmg to generate DMG
echo "[3/4] Creating DMG..."
create-dmg \
  --volname "Valt macOS Apple Silicon" \
  --volicon "src/Valt.UI/Assets/valt-logo.ico" \
  --background "src/Valt.UI/Assets/valt-logo.png" \
  --window-pos 200 120 \
  --window-size 800 400 \
  --icon-size 100 \
  --icon "Valt.app" 200 190 \
  --hide-extension "Valt.app" \
  --no-internet-enable \
  --app-drop-link 600 190 \
  "Valt-macos-arm64.dmg" \
  "Valt.app"

echo "DMG created: Valt-macos-arm64.dmg"
echo ""

# 5. Verify DMG
echo "[4/4] Verifying DMG..."
if [ -f "Valt-macos-arm64.dmg" ]; then
    echo "Success! DMG details:"
    ls -lh Valt-macos-arm64.dmg
    echo ""
    echo "=== DMG Creation Complete ==="
    echo ""
    echo "To test the DMG:"
    echo "  open Valt-macos-arm64.dmg"
    echo ""
    echo "To install:"
    echo "  1. Open the DMG"
    echo "  2. Drag Valt.app to /Applications"
    echo "  3. Right-click Valt.app and select 'Open' (first launch)"
else
    echo "Error: DMG creation failed"
    exit 1
fi