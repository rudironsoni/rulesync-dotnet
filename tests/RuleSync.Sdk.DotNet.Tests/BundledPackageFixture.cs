#nullable enable

using System;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using Xunit;

namespace Rulesync.Sdk.DotNet.Tests;

/// <summary>
/// Shared fixture for tests that use the committed rulesync binaries.
/// Binaries are downloaded by sync workflow and committed to tools/rulesync/.
/// </summary>
public class BundledPackageFixture
{
    /// <summary>
    /// Path to the rulesync binary for the current platform
    /// </summary>
    public string BundledPath { get; }

    public BundledPackageFixture()
    {
        var platform = GetPlatformIdentifier();
        var binaryName = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "rulesync.exe" : "rulesync";
        
        // Search for the binary in committed tools/rulesync/{platform}/
        var searchPaths = new[]
        {
            // From test assembly location
            GetAbsolutePath("tools", "rulesync", platform, binaryName),
            GetAbsolutePath("..", "..", "tools", "rulesync", platform, binaryName),
            GetAbsolutePath("..", "..", "..", "tools", "rulesync", platform, binaryName),
            // From current directory
            Path.GetFullPath(Path.Combine("tools", "rulesync", platform, binaryName))
        };

        foreach (var path in searchPaths)
        {
            if (File.Exists(path))
            {
                BundledPath = path;
                return;
            }
        }

        throw new InvalidOperationException(
            $"Rulesync binary not found for {platform}. " +
            $"Expected at: tools/rulesync/{platform}/{binaryName}. " +
            "Run sync workflow to download binaries or ensure submodules are built.");
    }

    private static string GetPlatformIdentifier()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return "windows-x64";
        }
        
        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            return RuntimeInformation.ProcessArchitecture == Architecture.Arm64 
                ? "darwin-arm64" 
                : "darwin-x64";
        }
        
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            return RuntimeInformation.ProcessArchitecture == Architecture.Arm64
                ? "linux-arm64"
                : "linux-x64";
        }
        
        throw new PlatformNotSupportedException(
            $"Platform not supported: {RuntimeInformation.OSDescription}");
    }

    private static string GetAbsolutePath(params string[] parts)
    {
        var assemblyDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        if (string.IsNullOrEmpty(assemblyDir))
        {
            return Path.GetFullPath(Path.Combine(parts));
        }
        return Path.GetFullPath(Path.Combine(assemblyDir, Path.Combine(parts)));
    }
}
