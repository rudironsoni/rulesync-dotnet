#nullable enable

using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Rulesync.Sdk.DotNet.Models;

namespace Rulesync.Sdk.DotNet.Abstractions;

/// <summary>
/// Client for interacting with the rulesync CLI tool.
/// </summary>
public interface IRulesyncClient : IDisposable
{
    /// <summary>
    /// Generates AI tool configurations from .rulesync directory.
    /// </summary>
#if NET6_0_OR_GREATER
    [RequiresUnreferencedCode("JSON serialization uses reflection which may require unreferenced code")]
#endif
#if NET7_0_OR_GREATER
    [RequiresDynamicCode("JSON serialization uses reflection which requires dynamic code")]
#endif
    ValueTask<Result<GenerateResult>> GenerateAsync(
        GenerateOptions? options = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Imports existing AI tool configurations into .rulesync.
    /// </summary>
#if NET6_0_OR_GREATER
    [RequiresUnreferencedCode("JSON serialization uses reflection which may require unreferenced code")]
#endif
#if NET7_0_OR_GREATER
    [RequiresDynamicCode("JSON serialization uses reflection which requires dynamic code")]
#endif
    ValueTask<Result<ImportResult>> ImportAsync(
        ImportOptions options,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Initializes a new .rulesync directory.
    /// </summary>
#if NET6_0_OR_GREATER
    [RequiresUnreferencedCode("JSON serialization uses reflection which may require unreferenced code")]
#endif
#if NET7_0_OR_GREATER
    [RequiresDynamicCode("JSON serialization uses reflection which requires dynamic code")]
#endif
    ValueTask<Result<InitResult>> InitAsync(
        InitOptions? options = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Fetches rules from a remote source.
    /// </summary>
#if NET6_0_OR_GREATER
    [RequiresUnreferencedCode("JSON serialization uses reflection which may require unreferenced code")]
#endif
#if NET7_0_OR_GREATER
    [RequiresDynamicCode("JSON serialization uses reflection which requires dynamic code")]
#endif
    ValueTask<Result<FetchSummary>> FetchAsync(
        FetchOptions options,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Installs rulesync globally or locally.
    /// </summary>
#if NET6_0_OR_GREATER
    [RequiresUnreferencedCode("JSON serialization uses reflection which may require unreferenced code")]
#endif
#if NET7_0_OR_GREATER
    [RequiresDynamicCode("JSON serialization uses reflection which requires dynamic code")]
#endif
    ValueTask<Result<InstallResult>> InstallAsync(
        InstallOptions? options = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates rulesync installation.
    /// </summary>
#if NET6_0_OR_GREATER
    [RequiresUnreferencedCode("JSON serialization uses reflection which may require unreferenced code")]
#endif
#if NET7_0_OR_GREATER
    [RequiresDynamicCode("JSON serialization uses reflection which requires dynamic code")]
#endif
    ValueTask<Result<UpdateResult>> UpdateAsync(
        UpdateOptions? options = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Generates or updates .gitignore entries.
    /// </summary>
#if NET6_0_OR_GREATER
    [RequiresUnreferencedCode("JSON serialization uses reflection which may require unreferenced code")]
#endif
#if NET7_0_OR_GREATER
    [RequiresDynamicCode("JSON serialization uses reflection which requires dynamic code")]
#endif
    ValueTask<Result<GitignoreResult>> GitignoreAsync(
        GitignoreOptions? options = null,
        CancellationToken cancellationToken = default);
}
