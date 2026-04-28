#!/bin/bash
set -e

echo "=== Valt macOS Packaging Script ==="
echo ""

# 1. Clean previous local outputs
echo "[1/11] Cleaning previous outputs..."
rm -rf publish/
rm -rf Valt.app
rm -f Valt-macos-arm64.dmg
echo "Cleaned publish/, Valt.app, Valt-macos-arm64.dmg"
echo ""

# 2. Publish the Avalonia/.NET app for macOS Apple Silicon
echo "[2/11] Publishing app for macOS arm64..."
dotnet publish src/Valt.UI/Valt.UI.csproj -c Release -r osx-arm64 --self-contained true -p:PublishSingleFile=false -p:PublishTrimmed=false -o publish/macos-arm64/
echo "Publish complete."
echo ""

# 3. Create a proper macOS app bundle structure
echo "[3/11] Creating .app bundle structure..."
mkdir -p Valt.app/Contents/MacOS
mkdir -p Valt.app/Contents/Resources
echo "Created Valt.app/Contents/{MacOS,Resources}"
echo ""

# 4. Copy all published files into Valt.app/Contents/MacOS/
echo "[4/11] Copying published files..."
cp -R publish/macos-arm64/* Valt.app/Contents/MacOS/
echo "Copied published files to Valt.app/Contents/MacOS/"
echo ""

# 5. Copy Info.plist
echo "[5/11] Copying Info.plist..."
cp src/Valt.UI/Info.plist Valt.app/Contents/Info.plist
echo "Copied src/Valt.UI/Info.plist to Valt.app/Contents/Info.plist"
echo ""

# 6. Copy the icon if available
echo "[6/11] Copying icon resources..."
if [ -f src/Valt.UI/Assets/valt-logo.ico ]; then
    cp src/Valt.UI/Assets/valt-logo.ico Valt.app/Contents/Resources/
    echo "Copied valt-logo.ico to Resources/"
else
    echo "Warning: valt-logo.ico not found, skipping icon copy"
fi

# Also copy PNG if available
if [ -f src/Valt.UI/Assets/valt-logo.png ]; then
    cp src/Valt.UI/Assets/valt-logo.png Valt.app/Contents/Resources/
    echo "Copied valt-logo.png to Resources/"
fi
echo ""

# 7. Make the executable file executable
echo "[7/11] Setting executable permissions..."
chmod +x Valt.app/Contents/MacOS/Valt
echo "Made Valt executable"
echo ""

# 8. Remove quarantine attributes
echo "[8/11] Removing quarantine attributes..."
xattr -cr Valt.app || true
echo "Removed extended attributes"
echo ""

# 9. Sign locally with ad-hoc codesign using entitlements
echo "[9/11] Codesigning with ad-hoc signature..."
codesign --deep --force --sign - --entitlements src/Valt.UI/entitlements.plist Valt.app
echo "Codesign complete"
echo ""

# 10. Verify codesign if possible
echo "[10/11] Verifying codesign..."
codesign --verify --verbose Valt.app 2>&1 || echo "Warning: codesign verification had issues"
echo ""

# 11. Print final commands
echo "[11/11] Packaging complete!"
echo ""
echo "=== Final Commands ==="
echo "Open the app:"
echo "  open Valt.app"
echo ""
echo "Run from terminal:"
echo "  ./Valt.app/Contents/MacOS/Valt"
echo ""
echo "Create DMG for distribution (optional):"
echo "  hdiutil create -volname Valt -srcfolder Valt.app -ov -format UDZO Valt-macos-arm64.dmg"
echo ""
echo "=== Done ==="