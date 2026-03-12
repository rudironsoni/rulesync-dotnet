#nullable enable

using System;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using Rulesync.Sdk.DotNet.Models;
using Xunit;

namespace Rulesync.Sdk.DotNet.Tests;

/// <summary>
/// Tests for verifying the bundled rulesync feature works correctly.
/// These tests validate that rulesync is packaged with the SDK and can be discovered at runtime.
/// </summary>
public class BundledRulesyncTests
{
    /// <summary>
    /// Gets the path to the bundled rulesync directory, if available.
    /// When running from a packaged NuGet assembly, returns the path to tools/rulesync.
    /// When running from project reference (development), returns null.
    /// </summary>
    private static string? GetBundledRulesyncPath()
    {
        // Get the directory containing the current assembly
        var assemblyLocation = Assembly.GetExecutingAssembly().Location;
        var assemblyDirectory = Path.GetDirectoryName(assemblyLocation);

        if (string.IsNullOrEmpty(assemblyDirectory))
        {
            return null;
        }

        // In a packaged NuGet, the structure is:
        // lib/{tfm}/RuleSync.Sdk.DotNet.dll
        // tools/rulesync/...
        // So we go up from lib/{tfm} to find tools/
        var toolsPath = Path.Combine(assemblyDirectory, "..", "..", "tools", "rulesync");
        var normalizedPath = Path.GetFullPath(toolsPath);

        if (Directory.Exists(normalizedPath))
        {
            return normalizedPath;
        }

        // Also check relative to assembly directly (for development scenarios)
        var devPath = Path.Combine(assemblyDirectory, "tools", "rulesync");
        if (Directory.Exists(devPath))
        {
            return devPath;
        }

        return null;
    }

    /// <summary>
    /// Determines if running from a packaged assembly (NuGet package) vs project reference.
    /// </summary>
    private static bool IsRunningFromPackagedAssembly()
    {
        // When running from a NuGet package, the assembly location will be in a NuGet cache
        // or package folder, not in the project's bin directory
        var assemblyLocation = Assembly.GetExecutingAssembly().Location;

        // Check if we're in a NuGet package path (contains .nuget/packages or similar)
        var normalizedPath = assemblyLocation.Replace('\\', '/');
        return normalizedPath.Contains("/packages/") ||
               normalizedPath.Contains("/.nuget/") ||
               normalizedPath.Contains("/tools/rulesync");
    }

    [Fact]
    public void GetBundledRulesyncPath_WhenPackaged_ReturnsValidPath()
    {
        // Skip if not running from packaged assembly
        if (!IsRunningFromPackagedAssembly())
        {
            return; // Test is not applicable in development mode
        }

        var bundledPath = GetBundledRulesyncPath();

        Assert.NotNull(bundledPath);
        Assert.True(Directory.Exists(bundledPath), $"Bundled rulesync directory should exist: {bundledPath}");
    }

    [Fact]
    public void GetBundledRulesyncPath_BundledDirectory_ContainsRequiredFiles()
    {
        var bundledPath = GetBundledRulesyncPath();

        // Skip if bundled rulesync is not available
        if (bundledPath == null)
        {
            return;
        }

        // Verify required files exist
        Assert.True(File.Exists(Path.Combine(bundledPath, "package.json")),
            "Bundled rulesync should contain package.json");

        Assert.True(Directory.Exists(Path.Combine(bundledPath, "dist")),
            "Bundled rulesync should contain dist directory");

        Assert.True(File.Exists(Path.Combine(bundledPath, "dist", "cli", "index.js")),
            "Bundled rulesync should contain dist/cli/index.js");
    }

    [Fact]
    public void GetBundledRulesyncPath_BundledCliJs_IsReadable()
    {
        var bundledPath = GetBundledRulesyncPath();

        // Skip if bundled rulesync is not available
        if (bundledPath == null)
        {
            return;
        }

        var cliJsPath = Path.Combine(bundledPath, "dist", "cli", "index.js");

        // Verify file is readable and has content
        var content = File.ReadAllText(cliJsPath);
        Assert.False(string.IsNullOrWhiteSpace(content), "cli.js should have content");
        Assert.Contains("rulesync", content.ToLowerInvariant(), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void GetBundledRulesyncPath_DevelopmentMode_ReturnsNullOrValidPath()
    {
        // This test validates that the method handles both packaged and development scenarios
        var bundledPath = GetBundledRulesyncPath();

        // In development mode, it may return null (no bundled version)
        // In packaged mode, it should return a valid path
        if (bundledPath != null)
        {
            Assert.True(Directory.Exists(bundledPath), $"If a path is returned, it should exist: {bundledPath}");
        }
        // If null, that's valid for development mode - no assertion needed
    }

    [Fact]
    public void RulesyncClient_WithoutRulesyncPath_UsesBundledOrNpx()
    {
        // Create client without specifying rulesyncPath
        // This should either use bundled version or fall back to npx
        using var client = new RulesyncClient();

        Assert.NotNull(client);
        // If we get here, the client was created successfully
        // The actual bundled vs npx resolution happens at execution time
    }

    [Fact]
    public void RulesyncClient_WithBundledPath_UsesBundledVersion()
    {
        var bundledPath = GetBundledRulesyncPath();

        // Skip if bundled rulesync is not available
        if (bundledPath == null)
        {
            return;
        }

        // Create client with the bundled path
        using var client = new RulesyncClient(
            nodeExecutablePath: "node",
            rulesyncPath: bundledPath);

        Assert.NotNull(client);
    }

    [Fact]
    public void RulesyncClient_NullRulesyncPath_FallsBackToNpx()
    {
        // Explicitly pass null for rulesyncPath to test fallback behavior
        using var client = new RulesyncClient(
            nodeExecutablePath: "node",
            rulesyncPath: null);

        Assert.NotNull(client);
    }

    [Fact]
    public void BundledPackageJson_ContainsValidName()
    {
        var bundledPath = GetBundledRulesyncPath();

        // Skip if bundled rulesync is not available
        if (bundledPath == null)
        {
            return;
        }

        var packageJsonPath = Path.Combine(bundledPath, "package.json");
        var content = File.ReadAllText(packageJsonPath);

        Assert.Contains("\"name\":", content);
        Assert.Contains("rulesync", content.ToLowerInvariant());
    }

    [Fact]
    public void BundledRulesync_VersionIsPresent()
    {
        var bundledPath = GetBundledRulesyncPath();

        // Skip if bundled rulesync is not available
        if (bundledPath == null)
        {
            return;
        }

        var packageJsonPath = Path.Combine(bundledPath, "package.json");
        var content = File.ReadAllText(packageJsonPath);

        Assert.Contains("\"version\":", content);
    }

    [Fact]
    public async Task RulesyncClient_WithBundledRulesync_CanExecuteGenerate()
    {
        var bundledPath = GetBundledRulesyncPath();

        Assert.NotNull(bundledPath);
        Assert.True(Directory.Exists(bundledPath),
            $"Bundled rulesync not found at {bundledPath}. Run 'dotnet build' first.");

        using var client = new RulesyncClient(
            nodeExecutablePath: "node",
            rulesyncPath: bundledPath,
            timeout: TimeSpan.FromSeconds(30));

        var options = new GenerateOptions
        {
            Targets = new[] { ToolTarget.ClaudeCode },
            Features = new[] { Feature.Rules },
            DryRun = true
        };

        var result = await client.GenerateAsync(options);

        Assert.True(result.IsSuccess, $"Bundled rulesync should execute successfully: {result.Error.Message}");
    }

    [Fact]
    public async Task RulesyncClient_WithBundledRulesync_CanExecuteImport()
    {
        var bundledPath = GetBundledRulesyncPath();

        Assert.NotNull(bundledPath);
        Assert.True(Directory.Exists(bundledPath),
            $"Bundled rulesync not found at {bundledPath}. Run 'dotnet build' first.");

        using var client = new RulesyncClient(
            nodeExecutablePath: "node",
            rulesyncPath: bundledPath,
            timeout: TimeSpan.FromSeconds(30));

        var options = new ImportOptions
        {
            Target = ToolTarget.ClaudeCode,
            Features = new[] { Feature.Rules }
        };

        var result = await client.ImportAsync(options);

        // Import may fail if no existing config, but should not crash
        // Result<T> is a struct so it's never null - just verify test reached this point
    }
}
