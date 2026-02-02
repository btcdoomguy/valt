#!/bin/bash
set -e

INSTALL_DIR="${HOME}/.local/share/valt"
BIN_DIR="${HOME}/.local/bin"
ICON_DIR="${HOME}/.local/share/icons/hicolor"
APP_DIR="${HOME}/.local/share/applications"

echo "Installing Valt..."

# Create directories
mkdir -p "$INSTALL_DIR" "$BIN_DIR" "$APP_DIR"

# Copy executable
cp Valt "$INSTALL_DIR/"
chmod +x "$INSTALL_DIR/Valt"

# Create symlink in PATH
ln -sf "$INSTALL_DIR/Valt" "$BIN_DIR/valt"

# Install icons
for size in 16 32 48 64 128 256 512; do
    mkdir -p "$ICON_DIR/${size}x${size}/apps"
    cp "icons/${size}x${size}/valt.png" "$ICON_DIR/${size}x${size}/apps/" 2>/dev/null || true
done

# Install desktop file
sed "s|Exec=valt|Exec=$INSTALL_DIR/Valt|g" valt.desktop > "$APP_DIR/valt.desktop"

# Update icon cache
gtk-update-icon-cache "$ICON_DIR" 2>/dev/null || true

echo "Valt installed successfully!"
echo "You can now launch Valt from your application menu or by running 'valt'"
