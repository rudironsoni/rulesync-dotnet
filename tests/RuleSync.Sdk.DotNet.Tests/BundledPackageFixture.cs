#nullable enable

using System;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using Xunit;

namespace Rulesync.Sdk.DotNet.Tests;

/// <summary>
/// Shared fixture for tests that use the bundled rulesync binaries.
/// Binaries are included in the SDK via MSBuild and copied to output.
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
        
        // SDK copies binaries to output directory during build
        var assemblyDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        if (string.IsNullOrEmpty(assemblyDir))
        {
            throw new InvalidOperationException("Cannot determine assembly directory");
        }
        
        var bundledPath = Path.Combine(assemblyDir, "tools", "rulesync", platform, binaryName);
        
        if (!File.Exists(bundledPath))
        {
            throw new InvalidOperationException(
                $"Rulesync binary not found at: {bundledPath}. " +
                "Ensure SDK project includes binaries and builds successfully.");
        }
        
        BundledPath = bundledPath;
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

}
