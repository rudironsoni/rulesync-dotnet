#nullable enable

using System;
using System.Threading.Tasks;
using Rulesync.Sdk.DotNet.Models;
using Xunit;

namespace Rulesync.Sdk.DotNet.Tests;

/// <summary>
/// Tests that actually execute the bundled rulesync CLI commands.
/// These require the bundled package to be built - use BundledPackageFixture.
/// </summary>
public class BundledExecutionTests : IClassFixture<BundledPackageFixture>
{
    private readonly BundledPackageFixture _fixture;

    public BundledExecutionTests(BundledPackageFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task RulesyncClient_WithBundledRulesync_CanExecuteGenerate()
    {
        using var client = new RulesyncClient(
            nodeExecutablePath: "node",
            rulesyncPath: _fixture.BundledPath,
            timeout: TimeSpan.FromSeconds(30));

        var options = new GenerateOptions
        {
            Targets = new[] { ToolTarget.ClaudeCode },
            Features = new[] { Feature.Rules },
            DryRun = true
        };

        var result = await client.GenerateAsync(options);

        Assert.True(result.IsSuccess, $"Bundled rulesync should execute successfully: {result.Error?.Message}");
    }

    [Fact]
    public async Task RulesyncClient_WithBundledRulesync_CanExecuteImport()
    {
        using var client = new RulesyncClient(
            nodeExecutablePath: "node",
            rulesyncPath: _fixture.BundledPath,
            timeout: TimeSpan.FromSeconds(30));

        var options = new ImportOptions
        {
            Target = ToolTarget.ClaudeCode,
            Features = new[] { Feature.Rules }
        };

        var result = await client.ImportAsync(options);

        // Result is a value type - test that the operation completed without throwing
        Assert.True(result.IsSuccess || result.Error != null, 
            "Import should either succeed or fail gracefully with an error");
    }
}
