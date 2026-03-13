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

    /// <summary>
    /// Gets the path to the cached binary, downloading if necessary.
    /// </summary>
    public static async Task<string?> EnsureBinaryAsync(
        string version,
        CancellationToken cancellationToken = default)
    {
        var platform = GetPlatformIdentifier();
        var binaryName = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "rulesync.exe" : "rulesync";
        var cacheDir = GetCacheDirectory();
        var binaryPath = Path.Combine(cacheDir, version, platform, binaryName);

        // Check if already downloaded
        if (File.Exists(binaryPath))
        {
            return binaryPath;
        }

        // Download binary
        return await DownloadBinaryAsync(version, platform, binaryPath, cancellationToken);
    }

    /// <summary>
    /// Downloads the binary for the specified platform.
    /// </summary>
    private static async Task<string?> DownloadBinaryAsync(
        string version,
        string platform,
        string outputPath,
        CancellationToken cancellationToken)
    {
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

            // Download binary
#if NET5_0_OR_GREATER
            await using var response = await HttpClient.GetStreamAsync(url, cancellationToken);
            await using var fileStream = File.Create(outputPath);
#else
            using var response = await HttpClient.GetStreamAsync(url);
            using var fileStream = File.Create(outputPath);
#endif
            await response.CopyToAsync(fileStream, cancellationToken);

            // Make executable on Unix
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                File.SetUnixFileMode(outputPath, UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);
            }

            return outputPath;
        }
        catch (Exception ex)
        {
            // Clean up partial download
            if (File.Exists(outputPath))
            {
                File.Delete(outputPath);
            }

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
