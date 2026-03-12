# RuleSync.Sdk.DotNet

A C# SDK for [rulesync](https://github.com/dyoshikawa/rulesync) - generate AI tool configurations programmatically from .NET applications.

[![CI](https://github.com/rudironsoni/rulesync-dotnet/actions/workflows/ci.yml/badge.svg)](https://github.com/rudironsoni/rulesync-dotnet/actions/workflows/ci.yml)
[![NuGet](https://img.shields.io/nuget/v/RuleSync.Sdk.DotNet.svg)](https://www.nuget.org/packages/RuleSync.Sdk.DotNet/)

## Features

- **Multi-target support**: .NET Standard 2.1, .NET 6.0, .NET 8.0
- **Native executables**: Standalone binaries for Windows, macOS, and Linux (no Node.js required)
- **Slim variant**: Small package size (~300KB) for environments with Node.js already installed
- **Source-generated types**: Types are auto-generated from rulesync's TypeScript source at compile time
- **Result pattern**: Functional error handling with `Result<T>`
- **Async/await**: `ValueTask<T>` for efficient async operations

## Installation

### From NuGet.org

**Full variant** (recommended - includes native executables):
```bash
dotnet add package RuleSync.Sdk.DotNet
```

**Slim variant** (~300KB, requires Node.js):
```bash
dotnet add package RuleSync.Sdk.DotNet.Slim
```

> **💡 Interchangeable**: Both packages use the same API, namespaces, and assembly name. You can switch between them at any time without changing your code - only the package ID differs.

### From GitHub Packages

```bash
# Add GitHub Packages source (replace USERNAME with your GitHub username)
dotnet nuget add source "https://nuget.pkg.github.com/rudironsoni/index.json" \
  --name "github" \
  --username USERNAME \
  --password YOUR_GITHUB_TOKEN

# Install the package
dotnet add package RuleSync.Sdk.DotNet --source github
```

Or add to your `nuget.config`:

```xml
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <packageSources>
    <add key="github" value="https://nuget.pkg.github.com/rudironsoni/index.json" />
  </packageSources>
  <packageSourceCredentials>
    <github>
      <add key="Username" value="USERNAME" />
      <add key="ClearTextPassword" value="YOUR_GITHUB_TOKEN" />
    </github>
  </packageSourceCredentials>
</configuration>
```

**Note**: You need a GitHub personal access token with `read:packages` scope.

### From Source

```bash
git clone --recursive https://github.com/rudironsoni/rulesync-dotnet.git
cd rulesync-dotnet
dotnet pack src/RuleSync.Sdk.DotNet/RuleSync.Sdk.DotNet.csproj -c Release
dotnet add package RuleSync.Sdk.DotNet --source ./src/RuleSync.Sdk.DotNet/bin/Release
```

## Package Variants

Two NuGet packages are available:

| Package | Size | Node.js Required | Use Case |
|---------|------|------------------|----------|
| `RuleSync.Sdk.DotNet` | ~130MB | **No** | Default choice. Includes native executables for all platforms. |
| `RuleSync.Sdk.DotNet.Slim` | ~300KB | Yes | For environments where Node.js is already available. |

The full variant automatically detects your platform and uses the appropriate native executable. The SDK falls back to bundled JavaScript if no native executable is available for your platform.

### Runtime Behavior

Both packages use the same runtime priority:

1. **Native executable** (full package only): Fastest, no Node.js needed
2. **Bundled JavaScript**: Uses included JS bundle with system Node.js
3. **npx**: Falls back to `npx rulesync` from npm

The slim package skips step 1 (no native executables), so it requires Node.js. Your code doesn't need to change - the SDK handles this automatically.

### Switching Between Packages

Since both packages share the same assembly name (`RuleSync.Sdk.DotNet.dll`) and namespaces, you can switch at any time:

```xml
<!-- In your .csproj, just change the PackageReference -->
<!-- Before: -->
<PackageReference Include="RuleSync.Sdk.DotNet" Version="7.18.1" />

<!-- After: -->
<PackageReference Include="RuleSync.Sdk.DotNet.Slim" Version="7.18.1" />
```

No code changes required!

## Prerequisites

### For RuleSync.Sdk.DotNet (full variant)
- **No prerequisites!** Native executables are included.

### For RuleSync.Sdk.DotNet.Slim (slim variant)
- Node.js 20+ installed and available in PATH

## Quick Start

```csharp
using Rulesync.Sdk.DotNet;
using Rulesync.Sdk.DotNet.Models;

// Create client
using var client = new RulesyncClient();

// Generate configurations for specific targets
var result = await client.GenerateAsync(new GenerateOptions
{
    Targets = new[] { ToolTarget.ClaudeCode, ToolTarget.Cursor },
    Features = new[] { Feature.Rules, Feature.Mcp }
});

if (result.IsSuccess)
{
    Console.WriteLine($"Generated configs");
}
else
{
    Console.WriteLine($"Error: {result.Error}");
}
```

## API Reference

### RulesyncClient

```csharp
public sealed class RulesyncClient : IDisposable
{
    // Uses "node" from PATH, 60 second timeout
    public RulesyncClient();

    // Custom configuration
    public RulesyncClient(
        string? nodeExecutablePath = null,  // Path to node executable
        string? rulesyncPath = null,         // Path to rulesync package (null = use npx)
        TimeSpan? timeout = null);           // Operation timeout

    // Generate AI tool configurations
    public ValueTask<Result<GenerateResult>> GenerateAsync(
        GenerateOptions? options = null,
        CancellationToken cancellationToken = default);

    // Import configuration from existing AI tool
    public ValueTask<Result<ImportResult>> ImportAsync(
        ImportOptions options,
        CancellationToken cancellationToken = default);

    // Initialize a new rulesync project
    public ValueTask<Result<InitResult>> InitAsync(
        InitOptions? options = null,
        CancellationToken cancellationToken = default);

    // Manage .gitignore entries for AI tool configs
    public ValueTask<Result<GitignoreResult>> GitignoreAsync(
        GitignoreOptions? options = null,
        CancellationToken cancellationToken = default);

    // Fetch remote configurations from GitHub
    public ValueTask<Result<FetchSummary>> FetchAsync(
        FetchOptions options,
        CancellationToken cancellationToken = default);

    // Install skills from declarative sources
    public ValueTask<Result<InstallResult>> InstallAsync(
        InstallOptions? options = null,
        CancellationToken cancellationToken = default);

    // Update rulesync CLI to latest version
    public ValueTask<Result<UpdateResult>> UpdateAsync(
        UpdateOptions? options = null,
        CancellationToken cancellationToken = default);
}
```

### Result<T>

```csharp
public readonly struct Result<T>
{
    public bool IsSuccess { get; }
    public bool IsFailure { get; }
    public T Value { get; }              // Throws if failed
    public RulesyncError Error { get; }  // Throws if success

    public Result<TResult> Map<TResult>(Func<T, TResult> mapper);
    public Result<T> OnSuccess(Action<T> action);
    public Result<T> OnFailure(Action<RulesyncError> action);
}
```

### Source-Generated Types

Types are automatically generated from rulesync's TypeScript source at compile time:

**Feature enum** (from `rulesync/src/types/features.ts`):

- `Rules`, `Ignore`, `Mcp`, `Subagents`, `Commands`, `Skills`, `Hooks`

**ToolTarget enum** (from `rulesync/src/types/tool-targets.ts`):

- `Agentsmd`, `Agentsskills`, `Antigravity`, `Augmentcode`, `Claudecode`, `Codexcli`, `Copilot`, `Cursor`, `Factorydroid`, `Geminicli`, `Goose`, `Junie`, `Kilo`, `Kiro`, `Opencode`, `Qwencode`, `Replit`, `Roo`, `Warp`, `Windsurf`, `Zed`

**GenerateOptions** (from `rulesync/src/lib/generate.ts`):

```csharp
public sealed class GenerateOptions
{
    public IReadOnlyList<ToolTarget>? Targets { get; init; }
    public IReadOnlyList<Feature>? Features { get; init; }
    public bool? Verbose { get; init; }
    public bool? Silent { get; init; }
    public bool? Delete { get; init; }
    public bool? Global { get; init; }
    public bool? SimulateCommands { get; init; }
    public bool? SimulateSubagents { get; init; }
    public bool? SimulateSkills { get; init; }
    public bool? DryRun { get; init; }
    public bool? Check { get; init; }
}
```

**ImportOptions** (from `rulesync/src/lib/import.ts`):

```csharp
public sealed class ImportOptions
{
    public ToolTarget Target { get; init; }  // Required
    public IReadOnlyList<Feature>? Features { get; init; }
    public bool? Verbose { get; init; }
    public bool? Silent { get; init; }
    public bool? Global { get; init; }
}
```

**InitOptions** (from `rulesync/src/cli/commands/init.ts`):

```csharp
public sealed class InitOptions
{
    public string ConfigPath { get; set; }  // Path to rulesync.jsonc
    public bool Verbose { get; set; }
    public bool Silent { get; set; }
}

public sealed class InitResult
{
    public InitFileResult ConfigFile { get; init; }
    public List<InitFileResult> SampleFiles { get; init; }
}
```

**GitignoreOptions** (from `rulesync/src/cli/commands/gitignore.ts`):

```csharp
public sealed class GitignoreOptions
{
    public string ConfigPath { get; set; }
    public bool Verbose { get; set; }
    public bool Silent { get; set; }
}

public sealed class GitignoreResult
{
    public int EntriesAdded { get; init; }
}
```

**FetchOptions** (from `rulesync/src/cli/commands/fetch.ts`):

```csharp
public sealed class FetchOptions
{
    public string Source { get; set; }      // Required: github:owner/repo/path
    public string Path { get; set; }        // Local destination path
    public bool Force { get; set; }        // Overwrite existing files
    public string Token { get; set; }      // GitHub token
    public bool Verbose { get; set; }
    public bool Silent { get; set; }
}

public sealed class FetchFileResult
{
    public string RelativePath { get; init; }
    public string Status { get; init; }     // "created", "updated", "unchanged", etc.
}

public sealed class FetchSummary
{
    public List<FetchFileResult> Files { get; init; }
}
```

**InstallOptions** (from `rulesync/src/cli/commands/install.ts`):

```csharp
public sealed class InstallOptions
{
    public bool Update { get; set; }       // Update existing skills
    public bool Frozen { get; set; }       // Fail if lock file out of sync
    public string Token { get; set; }      // GitHub token
    public string ConfigPath { get; set; }
    public bool Verbose { get; set; }
    public bool Silent { get; set; }
}

public sealed class InstallResult
{
    public int Installed { get; init; }
    public int Updated { get; init; }
}
```

**UpdateOptions** (from `rulesync/src/cli/commands/update.ts`):

```csharp
public sealed class UpdateOptions
{
    public bool Check { get; set; }        // Only check, don't install
    public bool Force { get; set; }        // Force update
    public string Token { get; set; }      // GitHub token
    public bool Verbose { get; set; }
    public bool Silent { get; set; }
}

public sealed class UpdateResult
{
    public bool Available { get; init; }
    public string CurrentVersion { get; init; }
    public string LatestVersion { get; init; }
}
```

## Examples

### Generate all features for all tools

```csharp
var result = await client.GenerateAsync();
```

### Generate specific features for Cursor

```csharp
var result = await client.GenerateAsync(new GenerateOptions
{
    Targets = new[] { ToolTarget.Cursor },
    Features = new[] { Feature.Rules, Feature.Mcp, Feature.Skills }
});
```

### Import from Claude Code

```csharp
var result = await client.ImportAsync(new ImportOptions
{
    Target = ToolTarget.Claudecode,
    Features = new[] { Feature.Rules, Feature.Mcp }
});

if (result.IsSuccess)
{
    Console.WriteLine("Import successful");
}
```

### Initialize a new project

```csharp
var result = await client.InitAsync();

if (result.IsSuccess)
{
    Console.WriteLine($"Created config: {result.Value.ConfigFile.Path}");
    foreach (var file in result.Value.SampleFiles)
    {
        Console.WriteLine($"  - {file.Path}");
    }
}
```

### Manage .gitignore entries

```csharp
var result = await client.GitignoreAsync();

if (result.IsSuccess)
{
    Console.WriteLine($"Added {result.Value.EntriesAdded} entries to .gitignore");
}
```

### Fetch remote configurations

```csharp
var result = await client.FetchAsync(new FetchOptions
{
    Source = "github:owner/repo/main/.cursor/rules",
    Path = "./fetched-configs",
    Force = true  // Overwrite existing files
});

if (result.IsSuccess)
{
    foreach (var file in result.Value.Files)
    {
        Console.WriteLine($"{file.RelativePath}: {file.Status}");
    }
}
```

### Install skills

```csharp
var result = await client.InstallAsync(new InstallOptions
{
    Update = true,  // Update existing skills
    Frozen = false  // Allow lock file updates
});

if (result.IsSuccess)
{
    Console.WriteLine($"Installed: {result.Value.Installed}, Updated: {result.Value.Updated}");
}
```

### Update CLI

```csharp
// Check for updates without installing
var result = await client.UpdateAsync(new UpdateOptions
{
    Check = true
});

if (result.IsSuccess)
{
    if (result.Value.Available)
    {
        Console.WriteLine($"Update available: {result.Value.CurrentVersion} -> {result.Value.LatestVersion}");
    }
    else
    {
        Console.WriteLine("Already up to date");
    }
}
```

### Error handling

```csharp
var result = await client.GenerateAsync(options);

result
    .OnSuccess(r => Console.WriteLine("Generated successfully"))
    .OnFailure(e => Console.WriteLine($"Error {e.Code}: {e.Message}"));

// Or use pattern matching
if (result.IsSuccess)
{
    var value = result.Value;
}
else
{
    var error = result.Error;
}
```

### Custom timeout and paths

```csharp
using var client = new RulesyncClient(
    nodeExecutablePath: "/usr/local/bin/node",
    rulesyncPath: "/path/to/rulesync",  // Local rulesync installation
    timeout: TimeSpan.FromMinutes(2)
);
```

## How It Works

The SDK uses an **incremental source generator** that parses rulesync's TypeScript type definitions at compile time and generates corresponding C# types:

1. **TypeScript parsing**: The generator reads TypeScript files from the rulesync submodule:
   - `rulesync/src/types/features.ts` → `Feature` enum
   - `rulesync/src/types/tool-targets.ts` → `ToolTarget` enum
   - `rulesync/src/lib/generate.ts` → `GenerateOptions`, `GenerateResult`
   - `rulesync/src/lib/import.ts` → `ImportOptions`, `ImportResult`
   - `rulesync/src/lib/init.ts` → `InitFileResult`
   - `rulesync/src/cli/commands/install.ts` → `InstallCommandOptions`
   - `rulesync/src/cli/commands/update.ts` → `UpdateCommandOptions`

2. **Manual types**: CLI-specific types not in TypeScript are defined manually:
   - `InitOptions`, `InitResult`
   - `GitignoreOptions`, `GitignoreResult`
   - `FetchOptions`, `FetchFileResult`, `FetchSummary`
   - `InstallOptions`, `InstallResult`
   - `UpdateOptions`, `UpdateResult`

3. **Compile-time generation**: Types are generated during build, not runtime

4. **IDE support**: Full IntelliSense and autocomplete for all generated types

## Repository Structure

```
rulesync-dotnet/
├── rulesync/                   # Git submodule (dyoshikawa/rulesync)
├── src/
│   ├── RuleSync.Sdk.DotNet/         # Main SDK
│   └── RuleSync.Sdk.DotNet.SourceGenerators/  # Source generators
├── tests/
│   └── RuleSync.Sdk.DotNet.Tests/ # Unit tests
└── .github/workflows/
    ├── ci.yml                # Build and test
    ├── release.yml           # Publish to NuGet
    └── sync-rulesync-release.yml  # Auto-sync with rulesync releases
```

## Building from Source

```bash
# Clone with submodules
git clone --recursive https://github.com/rudironsoni/rulesync-dotnet.git
cd rulesync-dotnet

# Build
dotnet build

# Test
dotnet test

# Pack
dotnet pack -c Release
```

## Version Synchronization

This repository is automatically synchronized with the main rulesync repository:

- The `sync-rulesync-release.yml` workflow checks hourly for new releases
- When a new rulesync version is detected, it creates a PR to update the submodule and version
- After merging the PR, push a tag (e.g., `v7.15.2`) to trigger the release workflow
- The release workflow publishes to both NuGet.org and GitHub Packages

## Architecture

```
RulesyncClient
    |
    +-- Spawns Node.js process
    |       +-- npx rulesync generate ...
    |       +-- npx rulesync import ...
    |       +-- npx rulesync init ...
    |       +-- npx rulesync gitignore ...
    |       +-- npx rulesync fetch ...
    |       +-- npx rulesync install ...
    |       +-- npx rulesync update ...
    |
    +-- JSON output parsing
    |       +-- System.Text.Json
    |
    +-- Returns Result<T>

Source Generator
    |
    +-- Parses TypeScript files at compile time
    |       +-- rulesync/src/types/features.ts
    |       +-- rulesync/src/types/tool-targets.ts
    |       +-- rulesync/src/lib/generate.ts
    |       +-- rulesync/src/lib/import.ts
    |       +-- rulesync/src/lib/init.ts
    |       +-- rulesync/src/cli/commands/install.ts
    |       +-- rulesync/src/cli/commands/update.ts
    |       +-- rulesync/src/cli/commands/fetch.ts
    |
    +-- Generates C# types
            +-- Feature enum
            +-- ToolTarget enum
            +-- GenerateOptions class
            +-- GenerateResult class
            +-- ImportOptions class
            +-- ImportResult class
            +-- InitFileResult class
```

## License

MIT License - see LICENSE file for details.
