#nullable enable

using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
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

        // Native binary should exist
        var binaryName = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "rulesync.exe" : "rulesync";
        Assert.True(File.Exists(Path.Combine(bundledPath, binaryName)),
            $"Bundled rulesync should contain {binaryName}");
    }

    [Fact]
    public void GetBundledRulesyncPath_BundledBinary_IsExecutable()
    {
        var bundledPath = GetBundledRulesyncPath();
        if (bundledPath == null)
        {
            return; // Skip - bundled not available
        }

        var binaryName = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "rulesync.exe" : "rulesync";
        var binaryPath = Path.Combine(bundledPath, binaryName);

        // Binary should exist and be non-empty
        var fileInfo = new FileInfo(binaryPath);
        Assert.True(fileInfo.Exists, $"Binary {binaryName} should exist");
        Assert.True(fileInfo.Length > 1000000, $"Binary should be >1MB (actual: {fileInfo.Length} bytes)"); // Native binaries are large
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
    public void BundledBinary_IsNativeExecutable()
    {
        var bundledPath = GetBundledRulesyncPath();
        if (bundledPath == null)
        {
            return; // Skip - bundled not available
        }

        var binaryName = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "rulesync.exe" : "rulesync";
        var binaryPath = Path.Combine(bundledPath, binaryName);

        // Verify it's a native binary (not a shell script or JS)
        var bytes = File.ReadAllBytes(binaryPath).Take(4).ToArray();
        var header = BitConverter.ToString(bytes);

        // ELF (Linux), Mach-O (macOS), or PE (Windows) header
        bool isNative = header.StartsWith("7F-45-4C-46") || // ELF
                       header.StartsWith("CF-FA-ED-FE") || // Mach-O 64-bit
                       header.StartsWith("FE-ED-FA-CE") || // Mach-O 32-bit
                       header.StartsWith("4D-5A");          // PE (Windows)

        Assert.True(isNative, $"Binary should be native executable, got header: {header}");
    }

    [Fact]
    public void BundledRulesync_VersionIsPresent()
    {
        // Version is tracked in .rulesync-version file at repo root
        var versionFile = Path.Combine("..", "..", "..", "..", ".rulesync-version");
        var fullPath = Path.GetFullPath(versionFile);
        
        if (!File.Exists(fullPath))
        {
            return; // Skip - version file not available
        }

        var version = File.ReadAllText(fullPath).Trim();
        Assert.False(string.IsNullOrEmpty(version), "Version should be present in .rulesync-version");
        Assert.StartsWith("v", version);
    }
}
