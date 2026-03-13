#nullable enable

using System;
using System.IO;
using System.Net.Http;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace Rulesync.Sdk.DotNet.Install;

/// <summary>
/// Downloads native rulesync binaries from GitHub releases at runtime.
/// </summary>
internal static class BinaryDownloader
{
    private const string BaseUrl = "https://github.com/dyoshikawa/rulesync/releases/download";
    private static readonly HttpClient HttpClient = new();
    private static readonly SemaphoreSlim DownloadLock = new(1, 1);

    /// <summary>
    /// Gets the path to the cached binary, downloading if necessary.
    /// Uses a lock to prevent concurrent downloads of the same binary.
    /// </summary>
    public static async Task<string?> EnsureBinaryAsync(
        string version,
        CancellationToken cancellationToken = default)
    {
        var platform = GetPlatformIdentifier();
        var binaryName = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "rulesync.exe" : "rulesync";
        var cacheDir = GetCacheDirectory();
        var binaryPath = Path.Combine(cacheDir, version, platform, binaryName);

        // Fast path: check if already downloaded (no lock needed)
        if (File.Exists(binaryPath))
        {
            return binaryPath;
        }

        // Slow path: download with lock to prevent concurrent downloads
        await DownloadLock.WaitAsync(cancellationToken);
        try
        {
            // Double-check after acquiring lock
            if (File.Exists(binaryPath))
            {
                return binaryPath;
            }

            return await DownloadBinaryAsync(version, platform, binaryPath, cancellationToken);
        }
        finally
        {
            DownloadLock.Release();
        }
    }

    /// <summary>
    /// Downloads the binary for the specified platform.
    /// Downloads to a temp file first, then moves to final location to prevent partial files.
    /// </summary>
    private static async Task<string?> DownloadBinaryAsync(
        string version,
        string platform,
        string outputPath,
        CancellationToken cancellationToken)
    {
        // Use temp file during download to prevent partial files from being executed
        var tempPath = outputPath + ".tmp";
        
        try
        {
            // Ensure directory exists
            var directory = Path.GetDirectoryName(outputPath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            // Map platform to release asset name
            var assetName = platform switch
            {
                "linux-x64" => "rulesync-linux-x64",
                "linux-arm64" => "rulesync-linux-arm64",
                "darwin-x64" => "rulesync-darwin-x64",
                "darwin-arm64" => "rulesync-darwin-arm64",
                "windows-x64" => "rulesync-windows-x64.exe",
                _ => throw new PlatformNotSupportedException($"Platform {platform} not supported")
            };

            var url = $"{BaseUrl}/{version}/{assetName}";

            // Download to temp file first
#if NET5_0_OR_GREATER
            await using var response = await HttpClient.GetStreamAsync(url, cancellationToken);
            await using var fileStream = new FileStream(tempPath, FileMode.Create, FileAccess.Write, FileShare.None, 81920, FileOptions.Asynchronous);
#else
            using var response = await HttpClient.GetStreamAsync(url);
            using var fileStream = new FileStream(tempPath, FileMode.Create, FileAccess.Write, FileShare.None, 81920, FileOptions.Asynchronous);
#endif
            await response.CopyToAsync(fileStream, cancellationToken);
            
            // Ensure all data is flushed to disk before making executable
            await fileStream.FlushAsync(cancellationToken);
            fileStream.Close();

            // Make executable on Unix before moving
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                // Use chmod to make file executable
                var process = new System.Diagnostics.Process
                {
                    StartInfo = new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = "chmod",
                        Arguments = $"+x \"{tempPath}\"",
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        UseShellExecute = false
                    }
                };
                process.Start();
                process.WaitForExit();
            }

            // Atomic move: rename temp file to final path
            // This prevents "text file busy" by ensuring file is complete before being accessible
            // Delete destination if exists (for compatibility with older frameworks)
            if (File.Exists(outputPath))
            {
                File.Delete(outputPath);
            }
            File.Move(tempPath, outputPath);

            return outputPath;
        }
        catch
        {
            // Clean up partial downloads
            try
            {
                if (File.Exists(tempPath))
                {
                    File.Delete(tempPath);
                }
            }
            catch { /* Ignore cleanup errors */ }
            
            try
            {
                if (File.Exists(outputPath))
                {
                    File.Delete(outputPath);
                }
            }
            catch { /* Ignore cleanup errors */ }

            return null;
        }
    }

    /// <summary>
    /// Gets the cache directory for storing downloaded binaries.
    /// </summary>
    private static string GetCacheDirectory()
    {
        var baseDir = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        return Path.Combine(baseDir, "rulesync-dotnet", "binaries");
    }

    /// <summary>
    /// Gets the platform identifier for the current runtime.
    /// </summary>
    private static string GetPlatformIdentifier()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return "windows-x64";
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            return RuntimeInformation.ProcessArchitecture == Architecture.Arm64 ? "darwin-arm64" : "darwin-x64";
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            return RuntimeInformation.ProcessArchitecture == Architecture.Arm64 ? "linux-arm64" : "linux-x64";
        }

        throw new PlatformNotSupportedException($"Unsupported platform: {RuntimeInformation.OSDescription}");
    }
}
