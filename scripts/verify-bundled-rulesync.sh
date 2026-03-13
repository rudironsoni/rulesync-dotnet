#!/bin/bash
# verify-bundled-rulesync.sh
# Verifies that the RuleSync.Sdk.DotNet NuGet package is properly built
# with bundled native binaries downloaded during pack

set -e

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "$SCRIPT_DIR/.." && pwd)"
NUPKG_DIR="$REPO_ROOT/nupkg"

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

echo "=== Verifying RuleSync.Sdk.DotNet NuGet Package ==="
echo ""
echo "Note: Native binaries are downloaded from GitHub releases during pack"
echo "      and bundled in the package at vendor/rulesync/{rid}/"
echo ""

# Find the main SDK package (not SourceGenerators, not symbols)
NUPKG_FILE="$(find "$NUPKG_DIR" -name "RuleSync.Sdk.DotNet.[0-9]*.nupkg" ! -name "*SourceGenerators*" ! -name "*.snupkg" | head -1)"

if [ -z "$NUPKG_FILE" ]; then
    echo -e "${RED}ERROR: No NuGet package found in $NUPKG_DIR/${NC}"
    echo "Expected: RuleSync.Sdk.DotNet.X.Y.Z.nupkg"
    exit 1
fi

echo "Found package: $(basename "$NUPKG_FILE")"
echo ""

# Check package size (should be ~178MB with all binaries)
NUPKG_SIZE=$(stat -f%z "$NUPKG_FILE" 2>/dev/null || stat -c%s "$NUPKG_FILE" 2>/dev/null || echo "0")
NUPKG_SIZE_MB=$((NUPKG_SIZE / 1024 / 1024))
echo "Package size: ${NUPKG_SIZE_MB}MB"
echo ""

# List package contents
echo "Package contents:"
unzip -l "$NUPKG_FILE" | grep -E "(lib/|vendor/|README)" || true
echo ""

# Verify DLLs are present
echo "Checking for required DLLs..."
REQUIRED_DLLS=(
    "lib/net6.0/RuleSync.Sdk.DotNet.dll"
    "lib/net8.0/RuleSync.Sdk.DotNet.dll"
    "lib/netstandard2.1/RuleSync.Sdk.DotNet.dll"
)

MISSING_DLLS=()

for dll in "${REQUIRED_DLLS[@]}"; do
    if unzip -l "$NUPKG_FILE" | grep -q "$dll"; then
        echo -e "${GREEN}✓ Found $dll${NC}"
    else
        echo -e "${RED}✗ Missing $dll${NC}"
        MISSING_DLLS+=("$dll")
    fi
done

# Verify native binaries are present
echo ""
echo "Checking for bundled native binaries..."
REQUIRED_BINARIES=(
    "vendor/rulesync/linux-x64/rulesync"
    "vendor/rulesync/linux-arm64/rulesync"
    "vendor/rulesync/osx-x64/rulesync"
    "vendor/rulesync/osx-arm64/rulesync"
    "vendor/rulesync/win-x64/rulesync.exe"
)

MISSING_BINARIES=()

for binary in "${REQUIRED_BINARIES[@]}"; do
    if unzip -l "$NUPKG_FILE" | grep -q "$binary"; then
        echo -e "${GREEN}✓ Found $binary${NC}"
    else
        echo -e "${RED}✗ Missing $binary${NC}"
        MISSING_BINARIES+=("$binary")
    fi
done

# Summary
echo ""
if [ ${#MISSING_DLLS[@]} -eq 0 ] && [ ${#MISSING_BINARIES[@]} -eq 0 ]; then
    echo -e "${GREEN}=== Verification PASSED ===${NC}"
    echo "Package contains required DLLs and all native binaries."
    echo "Package size: ${NUPKG_SIZE_MB}MB"
    exit 0
else
    echo -e "${RED}=== Verification FAILED ===${NC}"
    if [ ${#MISSING_DLLS[@]} -gt 0 ]; then
        echo "Missing DLLs:"
        printf '  - %s\n' "${MISSING_DLLS[@]}"
    fi
    if [ ${#MISSING_BINARIES[@]} -gt 0 ]; then
        echo "Missing binaries:"
        printf '  - %s\n' "${MISSING_BINARIES[@]}"
    fi
    exit 1
fi
