#nullable enable

using System;

namespace Rulesync.Sdk.DotNet.Configuration;

/// <summary>
/// Configuration options for RulesyncClient.
/// </summary>
public sealed class RulesyncOptions
{
    /// <summary>
    /// Gets or sets the timeout for operations. Default is 60 seconds.
    /// </summary>
    public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(60);

    /// <summary>
    /// Gets or sets the path to the rulesync executable.
    /// If null, uses the bundled binary.
    /// </summary>
    public string? ExecutablePath { get; set; }

    /// <summary>
    /// Gets or sets whether to parse verbose output for structured results.
    /// Default is true.
    /// </summary>
    public bool ParseVerboseOutput { get; set; } = true;

    /// <summary>
    /// Gets or sets the working directory for operations.
    /// If null, uses the current directory.
    /// </summary>
    public string? WorkingDirectory { get; set; }
}
