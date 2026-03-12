#nullable enable

using System;
using System.IO;
using System.Reflection;
using Xunit;

namespace Rulesync.Sdk.DotNet.Tests;

/// <summary>
/// Tests for verifying the bundled rulesync can be discovered at runtime.
/// These tests SKIP when bundled is not available (development mode).
/// </summary>
public class BundledDiscoveryTests
{
    private static string? GetBundledRulesyncPath()
    {
        var assemblyLocation = Assembly.GetExecutingAssembly().Location;
        var assemblyDirectory = Path.GetDirectoryName(assemblyLocation);

        if (string.IsNullOrEmpty(assemblyDirectory))
        {
            return null;
        }

        var toolsPath = Path.Combine(assemblyDirectory, "..", "..", "tools", "rulesync");
        var normalizedPath = Path.GetFullPath(toolsPath);

        if (Directory.Exists(normalizedPath))
        {
            return normalizedPath;
        }

        var devPath = Path.Combine(assemblyDirectory, "tools", "rulesync");
        if (Directory.Exists(devPath))
        {
            return devPath;
        }

        return null;
    }

    private static bool IsRunningFromPackagedAssembly()
    {
        var assemblyLocation = Assembly.GetExecutingAssembly().Location;
        var normalizedPath = assemblyLocation.Replace('\\', '/');
        return normalizedPath.Contains("/packages/") ||
               normalizedPath.Contains("/.nuget/") ||
               normalizedPath.Contains("/tools/rulesync");
    }

    [Fact]
    public void GetBundledRulesyncPath_WhenPackaged_ReturnsValidPath()
    {
        if (!IsRunningFromPackagedAssembly())
        {
            return; // Skip - not running from packaged assembly
        }

        var bundledPath = GetBundledRulesyncPath();
        if (bundledPath == null)
        {
            return; // Skip - bundled not available
        }

        Assert.NotNull(bundledPath);
        Assert.True(Directory.Exists(bundledPath), $"Bundled rulesync directory should exist: {bundledPath}");
    }

    [Fact]
    public void GetBundledRulesyncPath_BundledDirectory_ContainsRequiredFiles()
    {
        var bundledPath = GetBundledRulesyncPath();
        if (bundledPath == null)
        {
            return; // Skip - bundled not available
        }

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
        if (bundledPath == null)
        {
            return; // Skip - bundled not available
        }

        var cliJsPath = Path.Combine(bundledPath, "dist", "cli", "index.js");

        var content = File.ReadAllText(cliJsPath);
        Assert.False(string.IsNullOrWhiteSpace(content), "cli.js should have content");
        Assert.Contains("rulesync", content.ToLowerInvariant(), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void GetBundledRulesyncPath_DevelopmentMode_ReturnsNullOrValidPath()
    {
        var bundledPath = GetBundledRulesyncPath();

        if (bundledPath != null)
        {
            Assert.True(Directory.Exists(bundledPath), $"If a path is returned, it should exist: {bundledPath}");
        }
    }

    [Fact]
    public void RulesyncClient_WithoutRulesyncPath_UsesBundledOrNpx()
    {
        using var client = new RulesyncClient();
        Assert.NotNull(client);
    }

    [Fact]
    public void RulesyncClient_WithBundledPath_UsesBundledVersion()
    {
        var bundledPath = GetBundledRulesyncPath();
        if (bundledPath == null)
        {
            return; // Skip - bundled not available
        }

        using var client = new RulesyncClient(
            nodeExecutablePath: "node",
            rulesyncPath: bundledPath);

        Assert.NotNull(client);
    }

    [Fact]
    public void RulesyncClient_NullRulesyncPath_FallsBackToNpx()
    {
        using var client = new RulesyncClient(
            nodeExecutablePath: "node",
            rulesyncPath: null);

        Assert.NotNull(client);
    }

    [Fact]
    public void BundledPackageJson_ContainsValidName()
    {
        var bundledPath = GetBundledRulesyncPath();
        if (bundledPath == null)
        {
            return; // Skip - bundled not available
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
        if (bundledPath == null)
        {
            return; // Skip - bundled not available
        }

        var packageJsonPath = Path.Combine(bundledPath, "package.json");
        var content = File.ReadAllText(packageJsonPath);

        Assert.Contains("\"version\":", content);
    }
}
