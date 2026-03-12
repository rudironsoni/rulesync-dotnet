#nullable enable

using System;
using System.IO;
using System.Reflection;

namespace Rulesync.Sdk.DotNet.Tests;

/// <summary>
/// Shared fixture for tests that require the bundled rulesync package to exist.
/// Throws in constructor if bundled not available - fail fast with clear message.
/// </summary>
public class BundledPackageFixture : IDisposable
{
    public string BundledPath { get; }

    public BundledPackageFixture()
    {
        BundledPath = GetBundledRulesyncPath()
            ?? throw new InvalidOperationException(
                "Bundled rulesync not found. " +
                "Run: cd src/RuleSync.Sdk.DotNet/bundled && npm ci && npm run build");

        if (!Directory.Exists(BundledPath))
        {
            throw new InvalidOperationException(
                $"Bundled rulesync path does not exist: {BundledPath}");
        }
    }

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

    public void Dispose()
    {
        // Cleanup if needed
        GC.SuppressFinalize(this);
    }
}
