#nullable enable

using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;

namespace Rulesync.Sdk.DotNet.Tests;

/// <summary>
/// Shared fixture for tests that require the bundled rulesync package to exist.
/// Builds the bundled package automatically if not present - deterministic tests.
/// </summary>
public class BundledPackageFixture : IDisposable
{
    public string BundledPath { get; }

    public BundledPackageFixture()
    {
        var existingPath = FindExistingBundledRulesync();
        
        if (existingPath != null)
        {
            BundledPath = existingPath;
            return;
        }

        // Not found - build it deterministically
        var buildPath = GetBundledSourcePath();
        if (buildPath == null)
        {
            throw new InvalidOperationException(
                "Cannot find bundled rulesync source to build. " +
                "Expected at: src/RuleSync.Sdk.DotNet/bundled or similar.");
        }

        BuildBundledRulesync(buildPath);
        
        // Re-check after build
        BundledPath = FindExistingBundledRulesync()
            ?? throw new InvalidOperationException(
                "Failed to build bundled rulesync. Check npm/pnpm output above.");
    }

    private static string? FindExistingBundledRulesync()
    {
        var assemblyLocation = Assembly.GetExecutingAssembly().Location;
        var assemblyDirectory = Path.GetDirectoryName(assemblyLocation);

        if (string.IsNullOrEmpty(assemblyDirectory))
        {
            return null;
        }

        // Check packaged location
        var toolsPath = Path.Combine(assemblyDirectory, "..", "..", "tools", "rulesync");
        var normalizedPath = Path.GetFullPath(toolsPath);
        if (Directory.Exists(normalizedPath) && 
            File.Exists(Path.Combine(normalizedPath, "dist", "cli", "index.js")))
        {
            return normalizedPath;
        }

        // Check development location
        var devPath = Path.Combine(assemblyDirectory, "tools", "rulesync");
        if (Directory.Exists(devPath) &&
            File.Exists(Path.Combine(devPath, "dist", "cli", "index.js")))
        {
            return devPath;
        }

        return null;
    }

    private static string? GetBundledSourcePath()
    {
        // Try multiple locations where bundled source might exist
        // CI builds from rulesync/ submodule, not from bundled/ subdirectory
        var searchPaths = new[]
        {
            Path.Combine("..", "..", "..", "..", "rulesync"),
            Path.Combine("..", "..", "..", "rulesync"),
            Path.Combine("..", "..", "..", "..", "src", "RuleSync.Sdk.DotNet", "bundled"),
            Path.Combine("..", "..", "..", "..", "src", "RuleSync.Sdk.DotNet", "bundled"),
            Path.Combine("..", "..", "..", "src", "RuleSync.Sdk.DotNet", "bundled"),
            Path.Combine("src", "RuleSync.Sdk.DotNet", "bundled"),
            Path.Combine("rulesync")
        };

        var assemblyDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        if (string.IsNullOrEmpty(assemblyDir))
        {
            return null;
        }

        foreach (var relativePath in searchPaths)
        {
            var fullPath = Path.GetFullPath(Path.Combine(assemblyDir, relativePath));
            if (File.Exists(Path.Combine(fullPath, "package.json")))
            {
                return fullPath;
            }
        }

        return null;
    }

    private static void BuildBundledRulesync(string sourcePath)
    {
        var (packageManager, installArgs, buildArgs) = DetectPackageManager(sourcePath);
        
        Console.WriteLine($"Building bundled rulesync at: {sourcePath}");
        Console.WriteLine($"Using package manager: {packageManager}");

        RunCommand(packageManager, installArgs, sourcePath, "install dependencies");
        RunCommand(packageManager, buildArgs, sourcePath, "build bundled package");
        
        Console.WriteLine("Bundled rulesync built successfully");
    }

    private static (string cmd, string installArgs, string buildArgs) DetectPackageManager(string sourcePath)
    {
        if (File.Exists(Path.Combine(sourcePath, "pnpm-lock.yaml")))
        {
            return ("pnpm", "install", "run build");
        }
        
        if (File.Exists(Path.Combine(sourcePath, "package-lock.json")))
        {
            return ("npm", "ci", "run build");
        }
        
        if (File.Exists(Path.Combine(sourcePath, "yarn.lock")))
        {
            return ("yarn", "install", "run build");
        }

        // Default to npm
        return ("npm", "install", "run build");
    }

    private static void RunCommand(string command, string arguments, string workingDirectory, string description)
    {
        Console.WriteLine($"Running: {command} {arguments} in {workingDirectory}");
        
        var psi = new ProcessStartInfo
        {
            FileName = command,
            Arguments = arguments,
            WorkingDirectory = workingDirectory,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false
        };

        using var process = Process.Start(psi);
        if (process == null)
        {
            throw new InvalidOperationException($"Failed to start {command} for {description}");
        }

        var output = process.StandardOutput.ReadToEnd();
        var error = process.StandardError.ReadToEnd();
        process.WaitForExit();

        if (process.ExitCode != 0)
        {
            Console.WriteLine($"STDOUT: {output}");
            Console.WriteLine($"STDERR: {error}");
            throw new InvalidOperationException(
                $"Failed to {description}: {command} {arguments} exited with code {process.ExitCode}");
        }

        Console.WriteLine($"Successfully completed: {description}");
    }

    public void Dispose()
    {
        GC.SuppressFinalize(this);
    }
}
