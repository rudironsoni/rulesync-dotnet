#nullable enable

using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using Xunit;

namespace Rulesync.Sdk.DotNet.Tests;

/// <summary>
/// Shared fixture for tests that require the bundled rulesync package.
/// Builds from rulesync/ submodule at repo root - deterministic location.
/// </summary>
public class BundledPackageFixture : IDisposable
{
    /// <summary>
    /// Path to the built bundled rulesync (tools/rulesync/ relative to assembly)
    /// </summary>
    public string BundledPath { get; }

    public BundledPackageFixture()
    {
        // Deterministic output path: tools/rulesync/ next to test assembly
        var assemblyDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)
            ?? throw new InvalidOperationException("Cannot determine assembly directory");
        
        BundledPath = Path.GetFullPath(Path.Combine(assemblyDir, "tools", "rulesync"));
        
        // Check if already built
        if (Directory.Exists(BundledPath) && 
            File.Exists(Path.Combine(BundledPath, "dist", "cli", "index.js")))
        {
            return; // Already built
        }
        
        // Find rulesync source at repo root
        var sourcePath = FindRulesyncSource();
        if (sourcePath == null)
        {
            throw new InvalidOperationException(
                "Cannot find rulesync/ submodule. Ensure submodules are initialized: git submodule update --init");
        }
        
        // Build to deterministic location
        BuildBundledRulesync(sourcePath, BundledPath);
        
        // Verify build succeeded
        if (!File.Exists(Path.Combine(BundledPath, "dist", "cli", "index.js")))
        {
            throw new InvalidOperationException(
                $"Bundled rulesync build failed. Expected dist/cli/index.js not found at: {BundledPath}");
        }
    }
    
    /// <summary>
    /// Find rulesync/ submodule by walking up from assembly location
    /// </summary>
    private static string? FindRulesyncSource()
    {
        var assemblyDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        if (string.IsNullOrEmpty(assemblyDir))
        {
            return null;
        }
        
        // Walk up directory tree looking for rulesync/package.json
        var dir = new DirectoryInfo(assemblyDir);
        while (dir != null)
        {
            var rulesyncPath = Path.Combine(dir.FullName, "rulesync");
            if (File.Exists(Path.Combine(rulesyncPath, "package.json")))
            {
                return rulesyncPath;
            }
            
            // Also check one level down (CI might have different structure)
            var nestedRulesync = Path.Combine(dir.FullName, "..", "rulesync");
            var nestedFullPath = Path.GetFullPath(nestedRulesync);
            if (File.Exists(Path.Combine(nestedFullPath, "package.json")))
            {
                return nestedFullPath;
            }
            
            dir = dir.Parent;
        }
        
        return null;
    }

    private static void BuildBundledRulesync(string sourcePath, string outputPath)
    {
        Console.WriteLine($"Building rulesync from {sourcePath} to {outputPath}");
        
        // Detect package manager
        var (cmd, installArgs, buildCmd) = DetectPackageManager(sourcePath);
        
        // Install dependencies
        RunCommand(cmd, installArgs, sourcePath, "install dependencies");
        
        // Build
        RunCommand(cmd, buildCmd, sourcePath, "build");
        
        // Copy built files to deterministic location
        var builtDistPath = Path.Combine(sourcePath, "dist");
        if (!Directory.Exists(builtDistPath))
        {
            throw new InvalidOperationException(
                $"Build did not produce dist/ directory at: {sourcePath}");
        }
        
        // Ensure output directory exists
        Directory.CreateDirectory(outputPath);
        
        // Copy dist folder
        CopyDirectory(builtDistPath, Path.Combine(outputPath, "dist"));
        
        // Copy package.json for reference
        File.Copy(
            Path.Combine(sourcePath, "package.json"), 
            Path.Combine(outputPath, "package.json"), 
            overwrite: true);
        
        Console.WriteLine($"Rulesync bundled to: {outputPath}");
    }

    private static (string cmd, string installArgs, string buildCmd) DetectPackageManager(string sourcePath)
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

        return ("npm", "install", "run build");
    }

    private static void RunCommand(string command, string arguments, string workingDirectory, string description)
    {
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
            throw new InvalidOperationException($"Failed to start {command}");
        }

        var output = process.StandardOutput.ReadToEnd();
        var error = process.StandardError.ReadToEnd();
        process.WaitForExit();

        if (process.ExitCode != 0)
        {
            throw new InvalidOperationException(
                $"Failed to {description}: {command} {arguments}\nSTDERR: {error}\nSTDOUT: {output}");
        }
    }

    private static void CopyDirectory(string sourceDir, string destinationDir)
    {
        Directory.CreateDirectory(destinationDir);
        
        foreach (var file in Directory.GetFiles(sourceDir))
        {
            var destFile = Path.Combine(destinationDir, Path.GetFileName(file));
            File.Copy(file, destFile, overwrite: true);
        }
        
        foreach (var subdir in Directory.GetDirectories(sourceDir))
        {
            var destSubdir = Path.Combine(destinationDir, Path.GetFileName(subdir));
            CopyDirectory(subdir, destSubdir);
        }
    }

    public void Dispose()
    {
        GC.SuppressFinalize(this);
    }
}
