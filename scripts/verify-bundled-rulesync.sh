#!/bin/bash
# verify-bundled-rulesync.sh
# Verifies that the RuleSync.Sdk.DotNet NuGet package contains bundled rulesync files

set -e

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "$SCRIPT_DIR/.." && pwd)"
NUPKG_DIR="$REPO_ROOT/nupkg"

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

echo "=== Verifying Bundled Rulesync in NuGet Package ==="
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
unzip -l "$NUPKG_FILE" | grep -E "(tools|rulesync)" || true
echo ""

# Check for tools/rulesync directory
echo "Checking for tools/rulesync directory..."
if unzip -l "$NUPKG_FILE" | grep -q "tools/rulesync"; then
    echo -e "${GREEN}✓ Found tools/rulesync in package${NC}"
else
    echo -e "${RED}ERROR: tools/rulesync not found in package${NC}"
    echo ""
    echo "Full package contents:"
    unzip -l "$NUPKG_FILE"
    exit 1
fi

# Check for key bundled files
echo ""
echo "Checking for key bundled files..."

REQUIRED_FILES=(
    "tools/rulesync/dist/cli/index.js"
    "tools/rulesync/package.json"
)

MISSING_FILES=()

for file in "${REQUIRED_FILES[@]}"; do
    if unzip -l "$NUPKG_FILE" | grep -q "$file"; then
        echo -e "${GREEN}✓ Found $file${NC}"
    else
        echo -e "${RED}✗ Missing $file${NC}"
        MISSING_FILES+=("$file")
    fi
done

# Summary
echo ""
if [ ${#MISSING_FILES[@]} -eq 0 ]; then
    echo -e "${GREEN}=== Verification PASSED ===${NC}"
    echo "All required bundled files are present."
    exit 0
else
    echo -e "${RED}=== Verification FAILED ===${NC}"
    echo "Missing files:"
    printf '  - %s\n' "${MISSING_FILES[@]}"
    exit 1
fi
