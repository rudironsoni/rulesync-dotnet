#nullable enable

using System.Text.Json;
using System.Text.Json.Serialization;
using Rulesync.Sdk.DotNet.Models;

namespace Rulesync.Sdk.DotNet.Serialization;

/// <summary>
/// JSON serialization helpers for Rulesync models.
/// </summary>
/// <remarks>
/// This implementation uses reflection-based deserialization with proper trimming annotations.
/// Native AOT compatibility is achieved through the use of trimming annotations that inform
/// the linker to preserve the necessary types and members.
/// </remarks>
internal static class RulesyncJsonContext
{
    private static readonly JsonSerializerOptions s_options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        MaxDepth = 64,
        NumberHandling = JsonNumberHandling.AllowReadingFromString,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
    };

    /// <summary>
    /// Deserializes a GenerateResult from JSON.
    /// Returns null if the JSON is invalid, whitespace-only, or cannot be parsed.
    /// </summary>
#if NET6_0_OR_GREATER
    [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCode("JSON serialization uses reflection which may require unreferenced code")]
#endif
#if NET7_0_OR_GREATER
    [System.Diagnostics.CodeAnalysis.RequiresDynamicCode("JSON serialization uses reflection which requires dynamic code")]
#endif
    internal static GenerateResult? DeserializeGenerateResult(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return null;

        try
        {
            return JsonSerializer.Deserialize<GenerateResult>(json, s_options);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    /// <summary>
    /// Deserializes an ImportResult from JSON.
    /// Returns null if the JSON is invalid, whitespace-only, or cannot be parsed.
    /// </summary>
#if NET6_0_OR_GREATER
    [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCode("JSON serialization uses reflection which may require unreferenced code")]
#endif
#if NET7_0_OR_GREATER
    [System.Diagnostics.CodeAnalysis.RequiresDynamicCode("JSON serialization uses reflection which requires dynamic code")]
#endif
    internal static ImportResult? DeserializeImportResult(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return null;

        try
        {
            return JsonSerializer.Deserialize<ImportResult>(json, s_options);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    /// <summary>
    /// Deserializes an InitResult from JSON.
    /// Returns null if the JSON is invalid, whitespace-only, or cannot be parsed.
    /// </summary>
#if NET6_0_OR_GREATER
    [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCode("JSON serialization uses reflection which may require unreferenced code")]
#endif
#if NET7_0_OR_GREATER
    [System.Diagnostics.CodeAnalysis.RequiresDynamicCode("JSON serialization uses reflection which requires dynamic code")]
#endif
    internal static InitResult? DeserializeInitResult(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return null;

        try
        {
            return JsonSerializer.Deserialize<InitResult>(json, s_options);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    /// <summary>
    /// Deserializes a GitignoreResult from JSON.
    /// Returns null if the JSON is invalid, whitespace-only, or cannot be parsed.
    /// </summary>
#if NET6_0_OR_GREATER
    [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCode("JSON serialization uses reflection which may require unreferenced code")]
#endif
#if NET7_0_OR_GREATER
    [System.Diagnostics.CodeAnalysis.RequiresDynamicCode("JSON serialization uses reflection which requires dynamic code")]
#endif
    internal static GitignoreResult? DeserializeGitignoreResult(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return null;

        try
        {
            return JsonSerializer.Deserialize<GitignoreResult>(json, s_options);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    /// <summary>
    /// Deserializes a FetchSummary from JSON.
    /// Returns null if the JSON is invalid, whitespace-only, or cannot be parsed.
    /// </summary>
#if NET6_0_OR_GREATER
    [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCode("JSON serialization uses reflection which may require unreferenced code")]
#endif
#if NET7_0_OR_GREATER
    [System.Diagnostics.CodeAnalysis.RequiresDynamicCode("JSON serialization uses reflection which requires dynamic code")]
#endif
    internal static FetchSummary? DeserializeFetchSummary(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return null;

        try
        {
            return JsonSerializer.Deserialize<FetchSummary>(json, s_options);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    /// <summary>
    /// Deserializes an InstallResult from JSON.
    /// Returns null if the JSON is invalid, whitespace-only, or cannot be parsed.
    /// </summary>
#if NET6_0_OR_GREATER
    [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCode("JSON serialization uses reflection which may require unreferenced code")]
#endif
#if NET7_0_OR_GREATER
    [System.Diagnostics.CodeAnalysis.RequiresDynamicCode("JSON serialization uses reflection which requires dynamic code")]
#endif
    internal static InstallResult? DeserializeInstallResult(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return null;

        try
        {
            return JsonSerializer.Deserialize<InstallResult>(json, s_options);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    /// <summary>
    /// Deserializes an UpdateResult from JSON.
    /// Returns null if the JSON is invalid, whitespace-only, or cannot be parsed.
    /// </summary>
#if NET6_0_OR_GREATER
    [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCode("JSON serialization uses reflection which may require unreferenced code")]
#endif
#if NET7_0_OR_GREATER
    [System.Diagnostics.CodeAnalysis.RequiresDynamicCode("JSON serialization uses reflection which requires dynamic code")]
#endif
    internal static UpdateResult? DeserializeUpdateResult(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return null;

        try
        {
            return JsonSerializer.Deserialize<UpdateResult>(json, s_options);
        }
        catch (JsonException)
        {
            return null;
        }
    }
}
