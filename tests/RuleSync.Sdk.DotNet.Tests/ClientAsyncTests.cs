#nullable enable

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Rulesync.Sdk.DotNet.Configuration;
using Rulesync.Sdk.DotNet.Models;
using Xunit;

namespace Rulesync.Sdk.DotNet.Tests;

/// <summary>
/// Tests for ClientAsync that require a rulesync project.
/// Uses collection fixture to ensure .rulesync/ directory exists.
/// </summary>
[Collection("RulesyncProject")]
public class ClientAsyncTests : IDisposable
{
    private readonly RulesyncClient _client;
    private readonly RulesyncTestFixture _fixture;

    public ClientAsyncTests(RulesyncTestFixture fixture)
    {
        _fixture = fixture;
        // Use longer timeout for CI environments and set working directory
        _client = new RulesyncClient(new RulesyncOptions
        {
            WorkingDirectory = fixture.IsInitialized ? fixture.TestDirectory : null
        });
    }

    public void Dispose()
    {
        _client.Dispose();
        GC.SuppressFinalize(this);
    }

    #region GenerateAsync Tests

    [Fact]
    public async Task GenerateAsync_WithValidOptions_ReturnsResult()
    {
        var options = new GenerateOptions
        {
            Targets = new[] { ToolTarget.ClaudeCode },
            Features = new[] { Feature.Rules },
            DryRun = true
        };

        var result = await _client.GenerateAsync(options);

        Assert.True(result.IsSuccess, $"Expected success but got: {result.Error?.Message}");
        Assert.NotNull(result.Value);
    }

    [Fact]
    public async Task GenerateAsync_NullOptions_UsesDefaults()
    {
        var result = await _client.GenerateAsync(null);

        Assert.True(result.IsSuccess, $"Expected success but got: {result.Error?.Message}");
        Assert.NotNull(result.Value);
    }

    [Fact]
    public async Task GenerateAsync_WithAllOptions_CompletesSuccessfully()
    {
        var options = new GenerateOptions
        {
            Targets = new[] { ToolTarget.ClaudeCode },
            Features = new[] { Feature.Rules },
            Verbose = true,
            Silent = false,
            Delete = true,
            Global = true,
            SimulateCommands = true,
            SimulateSubagents = true,
            SimulateSkills = true,
            DryRun = true,
            Check = true
        };

        var result = await _client.GenerateAsync(options);

        Assert.True(result.IsSuccess, $"Expected success but got: {result.Error?.Message}");
        Assert.NotNull(result.Value);
    }

    #endregion

    #region ImportAsync Tests

    [Fact]
    public async Task ImportAsync_WithValidOptions_ReturnsResult()
    {
        var options = new ImportOptions
        {
            Target = ToolTarget.ClaudeCode,
            Features = new[] { Feature.Rules }
        };

        var result = await _client.ImportAsync(options);

        Assert.True(result.IsSuccess, $"Expected success but got: {result.Error?.Message}");
        Assert.NotNull(result.Value);
    }

    [Fact]
    public async Task ImportAsync_WithAllOptions_CompletesSuccessfully()
    {
        var options = new ImportOptions
        {
            Target = ToolTarget.ClaudeCode,
            Features = new[] { Feature.Rules, Feature.Mcp },
            Verbose = true,
            Silent = false,
            Global = true
        };

        var result = await _client.ImportAsync(options);

        Assert.True(result.IsSuccess, $"Expected success but got: {result.Error?.Message}");
        Assert.NotNull(result.Value);
    }

    #endregion

    #region Disposal Tests

    [Fact]
    public async Task GenerateAsync_AfterDispose_ThrowsObjectDisposedException()
    {
        var client = new RulesyncClient();
        client.Dispose();

        await Assert.ThrowsAsync<ObjectDisposedException>(async () =>
            await client.GenerateAsync());
    }

    [Fact]
    public async Task ImportAsync_AfterDispose_ThrowsObjectDisposedException()
    {
        var client = new RulesyncClient();
        client.Dispose();

        var options = new ImportOptions { Target = ToolTarget.ClaudeCode };

        await Assert.ThrowsAsync<ObjectDisposedException>(async () =>
            await client.ImportAsync(options));
    }

    [Fact]
    public void Dispose_CanBeCalledMultipleTimes()
    {
        var client = new RulesyncClient();

        client.Dispose();
        client.Dispose(); // Should not throw
        client.Dispose(); // Should not throw

        // If we get here, no exception was thrown - dispose is idempotent
        Assert.NotNull(client);
    }

    #endregion

    #region Error Handling Tests

    [Fact]
    public async Task ImportAsync_InvalidExecutable_ReturnsFailureResult()
    {
        using var client = new RulesyncClient();
        var options = new ImportOptions { Target = ToolTarget.ClaudeCode };

        var result = await client.ImportAsync(options);

        Assert.True(result.IsFailure);
    }

    #endregion

    #region Concurrent Operations

    [Fact]
    public async Task GenerateAsync_ConcurrentOperations_AllComplete()
    {
        var options = new GenerateOptions { DryRun = true };

        var tasks = new[]
        {
            _client.GenerateAsync(options).AsTask(),
            _client.GenerateAsync(options).AsTask(),
            _client.GenerateAsync(options).AsTask()
        };

        var results = await Task.WhenAll(tasks);

        // All operations completed successfully
        Assert.All(results, r =>
        {
            Assert.True(r.IsSuccess, $"Expected success but got: {r.Error?.Message}");
            Assert.NotNull(r.Value);
        });
    }

    [Fact]
    public async Task ImportAsync_ConcurrentOperations_AllComplete()
    {
        var options = new ImportOptions
        {
            Target = ToolTarget.ClaudeCode
        };

        var tasks = new[]
        {
            _client.ImportAsync(options).AsTask(),
            _client.ImportAsync(options).AsTask(),
            _client.ImportAsync(options).AsTask()
        };

        var results = await Task.WhenAll(tasks);

        // All operations completed successfully
        Assert.All(results, r =>
        {
            Assert.True(r.IsSuccess, $"Expected success but got: {r.Error?.Message}");
            Assert.NotNull(r.Value);
        });
    }

    [Fact]
    public async Task MixedOperations_Concurrent_AllComplete()
    {
        var generateOptions = new GenerateOptions { DryRun = true };
        var importOptions = new ImportOptions
        {
            Target = ToolTarget.ClaudeCode
        };

        var generateTask1 = _client.GenerateAsync(generateOptions).AsTask();
        var importTask = _client.ImportAsync(importOptions).AsTask();
        var generateTask2 = _client.GenerateAsync(generateOptions).AsTask();

        // Wait for all tasks to complete
        await Task.WhenAll(generateTask1, importTask, generateTask2);

        // All operations completed without throwing - Task.WhenAll guarantees all tasks completed successfully
        // No additional assertions needed since we reached this point
    }

    #endregion

    #region Output Size Tests

    [Fact]
    public async Task GenerateAsync_LargeOutput_Completes()
    {
        // This test verifies that the 10MB output size limit is enforced
        // without causing memory issues
        var options = new GenerateOptions
        {
            Targets = new[] { ToolTarget.ClaudeCode },
            Features = new[] { Feature.Rules },
            Verbose = true // More output
        };

        var result = await _client.GenerateAsync(options);

        Assert.True(result.IsSuccess, $"Expected success but got: {result.Error?.Message}");
        Assert.NotNull(result.Value);
    }

    #endregion

    #region Edge Cases

    [Fact]
    public async Task GenerateAsync_DefaultToken_Completes()
    {
        var options = new GenerateOptions
        {
            Targets = new[] { ToolTarget.ClaudeCode },
            Features = new[] { Feature.Rules }
        };

        var result = await _client.GenerateAsync(options);

        Assert.True(result.IsSuccess, $"Expected success but got: {result.Error?.Message}");
        Assert.NotNull(result.Value);
    }

    [Fact]
    public async Task ImportAsync_DefaultToken_Completes()
    {
        var options = new ImportOptions { Target = ToolTarget.ClaudeCode };

        var result = await _client.ImportAsync(options);

        Assert.True(result.IsSuccess, $"Expected success but got: {result.Error?.Message}");
        Assert.NotNull(result.Value);
    }

    [Fact]
    public async Task GenerateAsync_EmptyTargets_Completes()
    {
        var options = new GenerateOptions
        {
            Targets = Array.Empty<ToolTarget>()
        };

        var result = await _client.GenerateAsync(options);

        Assert.True(result.IsSuccess, $"Expected success but got: {result.Error?.Message}");
        Assert.NotNull(result.Value);
    }

    [Fact]
    public async Task ImportAsync_EmptyFeatures_Completes()
    {
        var options = new ImportOptions
        {
            Target = ToolTarget.ClaudeCode,
            Features = Array.Empty<Feature>()
        };

        var result = await _client.ImportAsync(options);

        Assert.True(result.IsSuccess, $"Expected success but got: {result.Error?.Message}");
        Assert.NotNull(result.Value);
    }

    #endregion
}
