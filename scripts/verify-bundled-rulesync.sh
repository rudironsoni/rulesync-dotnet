#!/bin/bash
# verify-bundled-rulesync.sh
# Verifies that the RuleSync.Sdk.DotNet NuGet package is properly built
# Note: Binaries are now downloaded at runtime, not bundled

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
echo "Note: Binaries are downloaded at runtime from GitHub releases"
echo "      They are no longer bundled in the package to avoid LFS quota issues"
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

# List package contents
echo "Package contents:"
unzip -l "$NUPKG_FILE" | grep -E "(lib/|README)" || true
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

# Summary
echo ""
if [ ${#MISSING_DLLS[@]} -eq 0 ]; then
    echo -e "${GREEN}=== Verification PASSED ===${NC}"
    echo "Package contains required DLLs."
    echo "Binaries will be downloaded at runtime from GitHub releases."
    exit 0
else
    echo -e "${RED}=== Verification FAILED ===${NC}"
    echo "Missing DLLs:"
    printf '  - %s\n' "${MISSING_DLLS[@]}"
    exit 1
fi
