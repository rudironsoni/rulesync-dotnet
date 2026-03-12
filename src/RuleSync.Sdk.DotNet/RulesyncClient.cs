#nullable enable

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Rulesync.Sdk.DotNet.Models;
using Rulesync.Sdk.DotNet.Serialization;
#if NETSTANDARD2_1
using Rulesync.Sdk.DotNet.Polyfills;
#endif

namespace Rulesync.Sdk.DotNet;

/// <summary>
/// Client for interacting with rulesync from .NET applications.
/// Spawns Node.js process to execute rulesync commands.
/// </summary>
public sealed class RulesyncClient : IDisposable
{
    private readonly string _nodeExecutablePath;
    private readonly string? _rulesyncPath;
    private readonly string? _nativeExecutablePath;
    private readonly TimeSpan _timeout;
    private bool _disposed;

    /// <summary>
    /// Creates a new RulesyncClient instance.
    /// </summary>
    /// <param name="nodeExecutablePath">
    /// Optional path to Node.js executable. If null, uses "node" from PATH.
    /// Security note: Ensure this path is trusted. Providing an untrusted path could result in arbitrary code execution.
    /// </param>
    /// <param name="rulesyncPath">Optional path to rulesync package. If null, uses bundled or npx.</param>
    /// <param name="timeout">Optional timeout for operations. Default is 60 seconds.</param>
    public RulesyncClient(
        string? nodeExecutablePath = null,
        string? rulesyncPath = null,
        TimeSpan? timeout = null)
    {
        _timeout = timeout ?? TimeSpan.FromSeconds(60);

        // Priority: explicit rulesyncPath > native executable > bundled JS > npx
        if (rulesyncPath != null)
        {
            _rulesyncPath = ValidateExecutablePath(rulesyncPath, nameof(rulesyncPath));
            _nodeExecutablePath = ValidateExecutablePath(nodeExecutablePath ?? FindNodeExecutable(), nameof(nodeExecutablePath));
            _nativeExecutablePath = null;
        }
        else if (GetNativeExecutablePath() is { } nativePath)
        {
            // Use native executable (Bun-compiled) - no Node.js needed!
            _nativeExecutablePath = nativePath;
            _nodeExecutablePath = string.Empty;
            _rulesyncPath = null;
        }
        else
        {
            // Fall back to JS bundle with Node.js
            _nativeExecutablePath = null;
            _rulesyncPath = GetBundledRulesyncPath();
            _nodeExecutablePath = ValidateExecutablePath(nodeExecutablePath ?? FindNodeExecutable(), nameof(nodeExecutablePath));
        }
    }

    /// <summary>
    /// Generates AI tool configurations based on the provided options.
    /// </summary>
    /// <param name="options">Generation options</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Result of the generation operation</returns>
#if NET6_0_OR_GREATER
    [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCode("JSON serialization uses reflection which may require unreferenced code")]
#endif
#if NET7_0_OR_GREATER
    [System.Diagnostics.CodeAnalysis.RequiresDynamicCode("JSON serialization uses reflection which requires dynamic code")]
#endif
    public async ValueTask<Result<GenerateResult>> GenerateAsync(
        GenerateOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();

        // Build args first (validates options) - this throws for invalid arguments
        var opts = options ?? new GenerateOptions();
        var args = BuildGenerateArgs(opts);

        try
        {
            var result = await ExecuteRulesyncAsync(args, cancellationToken).ConfigureAwait(false);

            if (result.ExitCode != 0)
            {
                return Result<GenerateResult>.Failure(
                    "GENERATE_FAILED",
                    $"Rulesync generate failed with exit code {result.ExitCode}: {result.Stderr}");
            }

            var generateResult = RulesyncJsonContext.DeserializeGenerateResult(result.Stdout);
            if (generateResult == null)
            {
                return Result<GenerateResult>.Failure(
                    "DESERIALIZATION_FAILED",
                    "Failed to deserialize generate result.");
            }

            return Result<GenerateResult>.Success(generateResult);
        }
        catch (Exception ex)
        {
            return Result<GenerateResult>.Failure(
                "EXCEPTION",
                $"An error occurred during generate: {ex.Message}");
        }
    }

    /// <summary>
    /// Imports configuration from an existing AI tool.
    /// </summary>
    /// <param name="options">Import options (target is required)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Result of the import operation</returns>
#if NET6_0_OR_GREATER
    [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCode("JSON serialization uses reflection which may require unreferenced code")]
#endif
#if NET7_0_OR_GREATER
    [System.Diagnostics.CodeAnalysis.RequiresDynamicCode("JSON serialization uses reflection which requires dynamic code")]
#endif
    public async ValueTask<Result<ImportResult>> ImportAsync(
        ImportOptions options,
        CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();

        // Build args first (validates options) - this throws for invalid arguments
        var args = BuildImportArgs(options);

        try
        {
            var result = await ExecuteRulesyncAsync(args, cancellationToken).ConfigureAwait(false);

            if (result.ExitCode != 0)
            {
                return Result<ImportResult>.Failure(
                    "IMPORT_FAILED",
                    $"Rulesync import failed with exit code {result.ExitCode}: {result.Stderr}");
            }

            var importResult = RulesyncJsonContext.DeserializeImportResult(result.Stdout);
            if (importResult == null)
            {
                return Result<ImportResult>.Failure(
                    "DESERIALIZATION_FAILED",
                    "Failed to deserialize import result.");
            }

            return Result<ImportResult>.Success(importResult);
        }
        catch (Exception ex)
        {
            return Result<ImportResult>.Failure(
                "EXCEPTION",
                $"An error occurred during import: {ex.Message}");
        }
    }

    /// <summary>
    /// Initializes a new rulesync project by creating the .rulesync/ directory and sample files.
    /// </summary>
    /// <param name="options">Initialization options</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Result of the init operation</returns>
#if NET6_0_OR_GREATER
    [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCode("JSON serialization uses reflection which may require unreferenced code")]
#endif
#if NET7_0_OR_GREATER
    [System.Diagnostics.CodeAnalysis.RequiresDynamicCode("JSON serialization uses reflection which requires dynamic code")]
#endif
    public async ValueTask<Result<InitResult>> InitAsync(
        InitOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();

        var opts = options ?? new InitOptions();
        var args = BuildInitArgs(opts);

        try
        {
            var result = await ExecuteRulesyncAsync(args, cancellationToken).ConfigureAwait(false);

            if (result.ExitCode != 0)
            {
                return Result<InitResult>.Failure(
                    "INIT_FAILED",
                    $"Rulesync init failed with exit code {result.ExitCode}: {result.Stderr}");
            }

            var initResult = RulesyncJsonContext.DeserializeInitResult(result.Stdout);
            if (initResult == null)
            {
                return Result<InitResult>.Failure(
                    "DESERIALIZATION_FAILED",
                    "Failed to deserialize init result.");
            }

            return Result<InitResult>.Success(initResult);
        }
        catch (Exception ex)
        {
            return Result<InitResult>.Failure(
                "EXCEPTION",
                $"An error occurred during init: {ex.Message}");
        }
    }

    /// <summary>
    /// Manages .gitignore entries for rulesync files.
    /// </summary>
    /// <param name="options">Gitignore options</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Result of the gitignore operation</returns>
#if NET6_0_OR_GREATER
    [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCode("JSON serialization uses reflection which may require unreferenced code")]
#endif
#if NET7_0_OR_GREATER
    [System.Diagnostics.CodeAnalysis.RequiresDynamicCode("JSON serialization uses reflection which requires dynamic code")]
#endif
    public async ValueTask<Result<GitignoreResult>> GitignoreAsync(
        GitignoreOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();

        var opts = options ?? new GitignoreOptions();
        var args = BuildGitignoreArgs(opts);

        try
        {
            var result = await ExecuteRulesyncAsync(args, cancellationToken).ConfigureAwait(false);

            if (result.ExitCode != 0)
            {
                return Result<GitignoreResult>.Failure(
                    "GITIGNORE_FAILED",
                    $"Rulesync gitignore failed with exit code {result.ExitCode}: {result.Stderr}");
            }

            var gitignoreResult = RulesyncJsonContext.DeserializeGitignoreResult(result.Stdout);
            if (gitignoreResult == null)
            {
                return Result<GitignoreResult>.Failure(
                    "DESERIALIZATION_FAILED",
                    "Failed to deserialize gitignore result.");
            }

            return Result<GitignoreResult>.Success(gitignoreResult);
        }
        catch (Exception ex)
        {
            return Result<GitignoreResult>.Failure(
                "EXCEPTION",
                $"An error occurred during gitignore: {ex.Message}");
        }
    }

    /// <summary>
    /// Fetches remote configuration files from GitHub repositories.
    /// </summary>
    /// <param name="options">Fetch options (source is required)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Result of the fetch operation</returns>
#if NET6_0_OR_GREATER
    [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCode("JSON serialization uses reflection which may require unreferenced code")]
#endif
#if NET7_0_OR_GREATER
    [System.Diagnostics.CodeAnalysis.RequiresDynamicCode("JSON serialization uses reflection which requires dynamic code")]
#endif
    public async ValueTask<Result<FetchSummary>> FetchAsync(
        FetchOptions options,
        CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();

        // Validate Source is provided
        if (string.IsNullOrWhiteSpace(options.Source))
        {
            throw new ArgumentException("Source is required.", nameof(options));
        }

        var args = BuildFetchArgs(options);

        try
        {
            var result = await ExecuteRulesyncAsync(args, cancellationToken).ConfigureAwait(false);

            if (result.ExitCode != 0)
            {
                return Result<FetchSummary>.Failure(
                    "FETCH_FAILED",
                    $"Rulesync fetch failed with exit code {result.ExitCode}: {result.Stderr}");
            }

            var fetchSummary = RulesyncJsonContext.DeserializeFetchSummary(result.Stdout);
            if (fetchSummary == null)
            {
                return Result<FetchSummary>.Failure(
                    "DESERIALIZATION_FAILED",
                    "Failed to deserialize fetch result.");
            }

            return Result<FetchSummary>.Success(fetchSummary);
        }
        catch (Exception ex)
        {
            return Result<FetchSummary>.Failure(
                "EXCEPTION",
                $"An error occurred during fetch: {ex.Message}");
        }
    }

    /// <summary>
    /// Installs skills from declarative sources defined in rulesync.jsonc.
    /// </summary>
    /// <param name="options">Install options</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Result of the install operation</returns>
#if NET6_0_OR_GREATER
    [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCode("JSON serialization uses reflection which may require unreferenced code")]
#endif
#if NET7_0_OR_GREATER
    [System.Diagnostics.CodeAnalysis.RequiresDynamicCode("JSON serialization uses reflection which requires dynamic code")]
#endif
    public async ValueTask<Result<InstallResult>> InstallAsync(
        InstallOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();

        var opts = options ?? new InstallOptions();
        var args = BuildInstallArgs(opts);

        try
        {
            var result = await ExecuteRulesyncAsync(args, cancellationToken).ConfigureAwait(false);

            if (result.ExitCode != 0)
            {
                return Result<InstallResult>.Failure(
                    "INSTALL_FAILED",
                    $"Rulesync install failed with exit code {result.ExitCode}: {result.Stderr}");
            }

            var installResult = RulesyncJsonContext.DeserializeInstallResult(result.Stdout);
            if (installResult == null)
            {
                return Result<InstallResult>.Failure(
                    "DESERIALIZATION_FAILED",
                    "Failed to deserialize install result.");
            }

            return Result<InstallResult>.Success(installResult);
        }
        catch (Exception ex)
        {
            return Result<InstallResult>.Failure(
                "EXCEPTION",
                $"An error occurred during install: {ex.Message}");
        }
    }

    /// <summary>
    /// Updates the rulesync CLI to the latest version.
    /// </summary>
    /// <param name="options">Update options</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Result of the update operation</returns>
#if NET6_0_OR_GREATER
    [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCode("JSON serialization uses reflection which may require unreferenced code")]
#endif
#if NET7_0_OR_GREATER
    [System.Diagnostics.CodeAnalysis.RequiresDynamicCode("JSON serialization uses reflection which requires dynamic code")]
#endif
    public async ValueTask<Result<UpdateResult>> UpdateAsync(
        UpdateOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();

        var opts = options ?? new UpdateOptions();
        var args = BuildUpdateArgs(opts);

        try
        {
            var result = await ExecuteRulesyncAsync(args, cancellationToken).ConfigureAwait(false);

            if (result.ExitCode != 0)
            {
                return Result<UpdateResult>.Failure(
                    "UPDATE_FAILED",
                    $"Rulesync update failed with exit code {result.ExitCode}: {result.Stderr}");
            }

            var updateResult = RulesyncJsonContext.DeserializeUpdateResult(result.Stdout);
            if (updateResult == null)
            {
                return Result<UpdateResult>.Failure(
                    "DESERIALIZATION_FAILED",
                    "Failed to deserialize update result.");
            }

            return Result<UpdateResult>.Success(updateResult);
        }
        catch (Exception ex)
        {
            return Result<UpdateResult>.Failure(
                "EXCEPTION",
                $"An error occurred during update: {ex.Message}");
        }
    }

    /// <summary>
    /// Disposes the client.
    /// </summary>
    public void Dispose()
    {
        _disposed = true;
        GC.SuppressFinalize(this);
    }

    private void ThrowIfDisposed()
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(RulesyncClient));
        }
    }

    private string[] BuildGenerateArgs(GenerateOptions options)
    {
        // Validate Targets enum values
        if (options.Targets?.Count > 0)
        {
            foreach (var target in options.Targets)
            {
                if (!Enum.IsDefined(typeof(ToolTarget), target))
                {
                    throw new ArgumentException(
                        $"Invalid ToolTarget value: {target}.",
                        "targets");
                }
            }
        }

        // Validate Features enum values
        if (options.Features?.Count > 0)
        {
            foreach (var feature in options.Features)
            {
                if (!Enum.IsDefined(typeof(Feature), feature))
                {
                    throw new ArgumentException(
                        $"Invalid Feature value: {feature}.",
                        "features");
                }
            }
        }

        var args = new System.Collections.Generic.List<string>();
        args.Add("generate");

        if (options.Targets?.Count > 0)
        {
            args.Add("--targets");
            args.Add(string.Join(",", options.Targets.Select(ToCliValue)));
        }

        if (options.Features?.Count > 0)
        {
            args.Add("--features");
            args.Add(string.Join(",", options.Features.Select(ToCliValue)));
        }

        if (options.Verbose == true) args.Add("--verbose");
        if (options.Silent == false) args.Add("--no-silent");
        if (options.Delete == true) args.Add("--delete");
        if (options.Global == true) args.Add("--global");
        if (options.SimulateCommands == true) args.Add("--simulate-commands");
        if (options.SimulateSubagents == true) args.Add("--simulate-subagents");
        if (options.SimulateSkills == true) args.Add("--simulate-skills");
        if (options.DryRun == true) args.Add("--dry-run");
        if (options.Check == true) args.Add("--check");

        // Always validate config path if provided (even if empty, to catch invalid characters)
        if (options.ConfigPath is not null)
        {
            var configPath = ValidateConfigPath(options.ConfigPath);
            if (!string.IsNullOrEmpty(configPath))
            {
                args.Add("--config");
                args.Add(configPath);
            }
        }

        // Add JSON output flag
        args.Add("--json");

        return args.ToArray();
    }

    private string[] BuildImportArgs(ImportOptions options)
    {
        // Validate Target is defined (not default/unset)
        // Check for default value (0) which is invalid for ToolTarget
        if (!Enum.IsDefined(typeof(ToolTarget), options.Target) || EqualityComparer<ToolTarget>.Default.Equals(options.Target, default))
        {
            throw new ArgumentException(
                $"Invalid ToolTarget value: {options.Target}. Target must be specified and cannot be the default value.",
                "target");
        }

        var args = new System.Collections.Generic.List<string>();
        args.Add("import");
        args.Add("--target");
        args.Add(ToCliValue(options.Target));

        if (options.Features?.Count > 0)
        {
            args.Add("--features");
            args.Add(string.Join(",", options.Features.Select(ToCliValue)));
        }

        if (options.Verbose == true) args.Add("--verbose");
        if (options.Silent == false) args.Add("--no-silent");
        if (options.Global == true) args.Add("--global");

        // Always validate config path if provided (even if empty, to catch invalid characters)
        if (options.ConfigPath is not null)
        {
            var configPath = ValidateConfigPath(options.ConfigPath);
            if (!string.IsNullOrEmpty(configPath))
            {
                args.Add("--config");
                args.Add(configPath);
            }
        }

        // Add JSON output flag
        args.Add("--json");

        return args.ToArray();
    }

    private static string[] BuildInitArgs(InitOptions options)
    {
        var args = new List<string> { "init" };

        if (!string.IsNullOrEmpty(options.ConfigPath))
        {
            args.Add("--config");
            args.Add(ValidateConfigPath(options.ConfigPath));
        }

        if (options.Verbose)
        {
            args.Add("--verbose");
        }

        if (options.Silent)
        {
            args.Add("--silent");
        }

        args.Add("--json");
        return args.ToArray();
    }

    private static string[] BuildGitignoreArgs(GitignoreOptions options)
    {
        var args = new List<string> { "gitignore" };

        if (!string.IsNullOrEmpty(options.ConfigPath))
        {
            args.Add("--config");
            args.Add(ValidateConfigPath(options.ConfigPath));
        }

        if (options.Verbose)
        {
            args.Add("--verbose");
        }

        if (options.Silent)
        {
            args.Add("--silent");
        }

        args.Add("--json");
        return args.ToArray();
    }

    private static string[] BuildFetchArgs(FetchOptions options)
    {
        var args = new List<string> { "fetch" };

        // Source is required
        args.Add(options.Source!);

        if (!string.IsNullOrEmpty(options.Path))
        {
            args.Add("--path");
            args.Add(options.Path);
        }

        if (options.Force)
        {
            args.Add("--force");
        }

        if (!string.IsNullOrEmpty(options.Token))
        {
            args.Add("--token");
            args.Add(options.Token);
        }

        if (options.Verbose)
        {
            args.Add("--verbose");
        }

        if (options.Silent)
        {
            args.Add("--silent");
        }

        args.Add("--json");
        return args.ToArray();
    }

    private static string[] BuildInstallArgs(InstallOptions options)
    {
        var args = new List<string> { "install" };

        if (options.Update)
        {
            args.Add("--update");
        }

        if (options.Frozen)
        {
            args.Add("--frozen");
        }

        if (!string.IsNullOrEmpty(options.Token))
        {
            args.Add("--token");
            args.Add(options.Token);
        }

        if (!string.IsNullOrEmpty(options.ConfigPath))
        {
            args.Add("--config");
            args.Add(ValidateConfigPath(options.ConfigPath));
        }

        if (options.Verbose)
        {
            args.Add("--verbose");
        }

        if (options.Silent)
        {
            args.Add("--silent");
        }

        args.Add("--json");
        return args.ToArray();
    }

    private static string[] BuildUpdateArgs(UpdateOptions options)
    {
        var args = new List<string> { "update" };

        if (options.Check)
        {
            args.Add("--check");
        }

        if (options.Force)
        {
            args.Add("--force");
        }

        if (!string.IsNullOrEmpty(options.Token))
        {
            args.Add("--token");
            args.Add(options.Token);
        }

        if (options.Verbose)
        {
            args.Add("--verbose");
        }

        if (options.Silent)
        {
            args.Add("--silent");
        }

        args.Add("--json");
        return args.ToArray();
    }

    private static string ValidateConfigPath(string configPath)
    {
        // Check for null bytes (path injection)
        if (configPath.Contains('\0'))
        {
            throw new ArgumentException("Config path contains invalid characters.", nameof(configPath));
        }

        // Normalize the path to prevent traversal attacks
        var fullPath = Path.GetFullPath(configPath);

        return fullPath;
    }

    /// <summary>
    /// Validates an executable path for null bytes and requires absolute paths.
    /// </summary>
    private static string ValidateExecutablePath(string path, string paramName)
    {
        // Check for null bytes (path injection)
        if (path.Contains('\0'))
        {
            throw new ArgumentException("Executable path contains invalid characters.", paramName);
        }

        // Require absolute paths for security
        if (!Path.IsPathRooted(path))
        {
            // Allow relative paths like "node" or "npx" that are resolved from PATH
            // but validate they don't contain directory traversal sequences
            // Only check for actual traversal patterns, not encoded sequences like "..%2f"
            if (path.Contains("../") || path.Contains("..\\") || path == ".." ||
                path.StartsWith("./") || path.StartsWith(".\\"))
            {
                throw new ArgumentException("Executable path cannot contain directory traversal characters.", paramName);
            }
            return path;
        }

        // For absolute paths, normalize to prevent traversal
        return Path.GetFullPath(path);
    }

    /// <summary>
    /// Discovers the bundled rulesync CLI path relative to the SDK assembly location.
    /// </summary>
    /// <returns>Path to bundled rulesync directory if found, null otherwise.</returns>
    private static string? GetBundledRulesyncPath()
    {
        try
        {
            var assemblyLocation = typeof(RulesyncClient).Assembly.Location;
            if (string.IsNullOrEmpty(assemblyLocation))
                return null;

            var assemblyDir = Path.GetDirectoryName(assemblyLocation);
            if (string.IsNullOrEmpty(assemblyDir))
                return null;

            // Navigate from assembly location (bin/Release/net8.0/) to package tools folder
            var bundledPath = Path.Combine(assemblyDir, "..", "..", "..", "tools", "rulesync");
            var fullPath = Path.GetFullPath(bundledPath);

            // Verify the expected CLI entry point exists
            var cliPath = Path.Combine(fullPath, "dist", "cli", "index.js");
            return File.Exists(cliPath) ? fullPath : null;
        }
        catch (Exception ex)
        {
            // Log exception for debugging but gracefully fall back
            Console.WriteLine($"[RuleSync SDK] Failed to locate bundled rulesync: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Discovers the native executable path for the current platform.
    /// Native executables are built using Bun and bundled with the SDK.
    /// </summary>
    /// <returns>Path to native executable if found, null otherwise.</returns>
    private static string? GetNativeExecutablePath()
    {
        try
        {
            var assemblyLocation = typeof(RulesyncClient).Assembly.Location;
            if (string.IsNullOrEmpty(assemblyLocation))
                return null;

            var assemblyDir = Path.GetDirectoryName(assemblyLocation);
            if (string.IsNullOrEmpty(assemblyDir))
                return null;

            // Determine runtime identifier for current platform
            var runtimeId = GetRuntimeId();
            var exeName = GetNativeExecutableName();

            // Look for native executable in tools/rulesync-native/{runtime-id}/
            var nativePath = Path.Combine(assemblyDir, "..", "..", "..", "tools", "rulesync-native", runtimeId, exeName);
            var fullPath = Path.GetFullPath(nativePath);

            return File.Exists(fullPath) ? fullPath : null;
        }
        catch (Exception ex)
        {
            // Log exception for debugging but gracefully fall back
            Console.WriteLine($"[RuleSync SDK] Failed to locate native executable: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Gets the runtime identifier for the current platform (e.g., "linux-x64", "osx-arm64").
    /// </summary>
    private static string GetRuntimeId()
    {
#if NETSTANDARD2_1
        var isWindows = System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.Windows);
        var isMacOS = System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.OSX);
        var arch = System.Runtime.InteropServices.RuntimeInformation.ProcessArchitecture;
#else
        var isWindows = OperatingSystem.IsWindows();
        var isMacOS = OperatingSystem.IsMacOS();
        var arch = System.Runtime.InteropServices.RuntimeInformation.ProcessArchitecture;
#endif
        string platform;
        if (isWindows)
            platform = "win";
        else if (isMacOS)
            platform = "osx";
        else
            platform = "linux";

        string architecture = arch.ToString().ToLowerInvariant();
        return $"{platform}-{architecture}";
    }

    /// <summary>
    /// Gets the native executable name for the current platform.
    /// </summary>
    private static string GetNativeExecutableName()
    {
#if NETSTANDARD2_1
        var isWindows = System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.Windows);
#else
        var isWindows = OperatingSystem.IsWindows();
#endif
        return isWindows ? "rulesync.exe" : "rulesync";
    }

    /// <summary>
    /// Finds the Node.js executable across common platform paths.
    /// </summary>
    /// <returns>Path to node executable if found, "node" as fallback.</returns>
    private static string FindNodeExecutable()
    {
#if NETSTANDARD2_1
        var isWindows = System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.Windows);
#else
        var isWindows = OperatingSystem.IsWindows();
#endif
        if (isWindows)
        {
            // On Windows, check common installation locations
            var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            var programFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
            var programFilesX86 = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);

            var windowsPaths = new[]
            {
                Path.Combine(localAppData, "nvm", "current", "node.exe"),
                Path.Combine(programFiles, "nodejs", "node.exe"),
                Path.Combine(programFilesX86, "nodejs", "node.exe"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "nvm", "current", "node.exe"),
            };

            foreach (var path in windowsPaths)
            {
                if (File.Exists(path))
                    return path;
            }
        }
        else
        {
            // Unix-like systems: check common paths
            var unixPaths = new[]
            {
                "/usr/bin/node",
                "/usr/local/bin/node",
                "/opt/homebrew/bin/node",
                "/opt/local/bin/node",
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".nvm", "current", "bin", "node"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".local", "share", "nvm", "current", "bin", "node"),
            };

            foreach (var path in unixPaths)
            {
                if (File.Exists(path))
                    return path;
            }
        }

        // Fall back to PATH resolution
        return "node";
    }

    /// <summary>
    /// Converts an enum value to its CLI-compatible kebab-case string representation.
    /// </summary>
    private static string ToCliValue<T>(T value) where T : struct, Enum
    {
        // Get the enum name and convert to kebab-case
        var name = value.ToString();
        if (string.IsNullOrEmpty(name))
            return string.Empty;

        // Special case mappings for legacy names with hyphens
        return name switch
        {
            "ClaudeCodeLegacy" => "claudecode-legacy",
            "AugmentCodeLegacy" => "augmentcode-legacy",
            "AgentsMd" => "agentsmd",
            "AgentsSkills" => "agentsskills",
            _ => PascalCaseToKebabCase(name)
        };
    }

    /// <summary>
    /// Converts PascalCase to kebab-case.
    /// </summary>
    private static string PascalCaseToKebabCase(string name)
    {
        var result = new System.Text.StringBuilder();
        for (int i = 0; i < name.Length; i++)
        {
            var c = name[i];
            if (char.IsUpper(c))
            {
                if (i > 0 && (char.IsLower(name[i - 1]) || (i + 1 < name.Length && char.IsLower(name[i + 1]))))
                {
                    result.Append('-');
                }
                result.Append(char.ToLowerInvariant(c));
            }
            else
            {
                result.Append(c);
            }
        }
        return result.ToString();
    }

    private const int MaxOutputSize = 10 * 1024 * 1024; // 10 MB limit

    private async Task<ProcessResult> ExecuteRulesyncAsync(
        string[] args,
        CancellationToken cancellationToken)
    {
        var startInfo = new ProcessStartInfo
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        if (_nativeExecutablePath != null)
        {
            // Use native executable (Bun-compiled) - no Node.js needed!
            startInfo.FileName = _nativeExecutablePath;
        }
        else if (_rulesyncPath != null)
        {
            // Use bundled JS with Node.js
            startInfo.FileName = _nodeExecutablePath;
            startInfo.ArgumentList.Add(Path.Combine(_rulesyncPath, "dist", "cli", "index.js"));
        }
        else
        {
            // Fall back to npx
            startInfo.FileName = "npx";
            startInfo.ArgumentList.Add("rulesync");
        }

        foreach (var arg in args)
        {
            startInfo.ArgumentList.Add(arg);
        }

        using var process = new Process { StartInfo = startInfo };
        var stdoutBuilder = new StringBuilder();
        var stderrBuilder = new StringBuilder();
        int stdoutLength = 0;
        int stderrLength = 0;

        process.OutputDataReceived += (sender, e) =>
        {
            if (e.Data == null) return;
            var dataLength = e.Data.Length + Environment.NewLine.Length;
            // Atomic check-and-reserve: try to reserve space, only append if successful
            var newLength = Interlocked.Add(ref stdoutLength, dataLength);
            if (newLength <= MaxOutputSize + dataLength)
            {
                lock (stdoutBuilder)
                {
                    stdoutBuilder.AppendLine(e.Data);
                }
            }
            else
            {
                // Revert the reservation if over limit
                Interlocked.Add(ref stdoutLength, -dataLength);
            }
        };

        process.ErrorDataReceived += (sender, e) =>
        {
            if (e.Data == null) return;
            var dataLength = e.Data.Length + Environment.NewLine.Length;
            // Atomic check-and-reserve: try to reserve space, only append if successful
            var newLength = Interlocked.Add(ref stderrLength, dataLength);
            if (newLength <= MaxOutputSize + dataLength)
            {
                lock (stderrBuilder)
                {
                    stderrBuilder.AppendLine(e.Data);
                }
            }
            else
            {
                // Revert the reservation if over limit
                Interlocked.Add(ref stderrLength, -dataLength);
            }
        };

        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        cts.CancelAfter(_timeout);

        try
        {
            await process.WaitForExitAsync(cts.Token).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            process.Kill();
            throw new TimeoutException($"Rulesync operation timed out after {_timeout.TotalSeconds} seconds.");
        }
        catch
        {
            process.Kill();
            throw;
        }

        return new ProcessResult(
            process.ExitCode,
            stdoutBuilder.ToString(),
            stderrBuilder.ToString());
    }

    private readonly record struct ProcessResult(int ExitCode, string Stdout, string Stderr);
}
