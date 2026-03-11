# Native Executable Support

## Overview

The RuleSync.Sdk.DotNet can now include native executables compiled with Bun, eliminating the Node.js requirement entirely for end users.

## How It Works

1. **Bun Compilation**: The rulesync CLI is compiled to a standalone native executable using Bun's `bun build --compile`
2. **Multi-Platform**: Executables are built for each target platform (Windows x64, macOS x64/ARM64, Linux x64)
3. **Automatic Detection**: The SDK automatically discovers and uses the native executable for the current platform
4. **Graceful Fallback**: If no native executable is available, falls back to bundled JS + Node.js

## Building Native Executables

### Prerequisites

- [Bun](https://bun.sh/) installed (`curl -fsSL https://bun.sh/install | bash`)
- rulesync submodule built (`cd rulesync && pnpm install && pnpm run build`)

### Build Current Platform

```bash
node src/RuleSync.Sdk.DotNet/build-native-executables.js
```

This creates:
```
native-binaries/
└── linux-x64/
    └── rulesync      # 105MB standalone executable
```

### Build All Platforms

Bun can cross-compile to different platforms:

```bash
# Linux x64
bun build --compile --target=bun-linux-x64 rulesync/dist/cli/index.js --outfile native-binaries/linux-x64/rulesync

# macOS x64
bun build --compile --target=bun-darwin-x64 rulesync/dist/cli/index.js --outfile native-binaries/osx-x64/rulesync

# macOS ARM64
bun build --compile --target=bun-darwin-arm64 rulesync/dist/cli/index.js --outfile native-binaries/osx-arm64/rulesync

# Windows x64
bun build --compile --target=bun-windows-x64 rulesync/dist/cli/index.js --outfile native-binaries/win-x64/rulesync.exe
```

## Integration with NuGet Package

Native executables are automatically included in the NuGet package when present:

```xml
<ItemGroup>
  <None Include="native-binaries/**/*" Pack="true" PackagePath="tools/rulesync-native/" />
</ItemGroup>
```

## Runtime Priority

The SDK uses this priority order:

1. **Explicit path** - If `rulesyncPath` is provided, use it
2. **Native executable** - Use platform-specific native binary (no Node.js needed!)
3. **Bundled JS** - Use bundled rulesync with system Node.js
4. **npx** - Fall back to `npx rulesync`

## Size Comparison

| Approach | Size | Node.js Required |
|----------|------|------------------|
| Bundled JS only | ~2 MB | Yes |
| Native executable | ~105 MB | No |

## Trade-offs

### Native Executables ✅

- **Zero dependencies** - No Node.js installation needed
- **Faster startup** - No JS parsing overhead
- **Self-contained** - Everything in one file

### Native Executables ❌

- **Larger size** - ~50x larger than JS bundle
- **Platform-specific** - Need separate builds for each platform
- **Bun requirement** - Build process requires Bun

## Recommendation

The bundled JS approach is recommended for most use cases:

- Smaller package size (~2MB vs ~105MB)
- Single package works on all platforms
- Node.js is usually already installed

Native executables are useful for:
- Air-gapped environments without Node.js
- Simplified deployment scenarios
- Faster startup requirements

## Testing

Verify the native executable works:

```bash
./native-binaries/linux-x64/rulesync --version
# Output: 7.18.1
```

Verify SDK uses native executable:

```csharp
using var client = new RulesyncClient(); // Automatically uses native exe if available
var result = await client.GenerateAsync(new GenerateOptions { ... });
```
