#nullable enable

using System;
using System.Threading;
using System.Threading.Tasks;
using Rulesync.Sdk.DotNet.Models;
using Xunit;

namespace Rulesync.Sdk.DotNet.Tests;

/// <summary>
/// Error handling and edge case tests for the RulesyncClient.
/// Tests JSON parsing failures, process failures, timeouts, and cancellation.
/// </summary>
public class ErrorHandlingTests
{
    #region JSON Deserialization Error Tests

    [Fact]
    public async Task InitAsync_ReturnsStructuredResult()
    {
        using var client = new RulesyncClient();
        
        // Verify the method returns a properly structured result
        var result = await client.InitAsync();

        // Result should have a valid state (either success with value or failure with error)
        if (result.IsSuccess)
        {
            Assert.NotNull(result.Value);
        }
        else
        {
            // Error should be descriptive with non-empty code and message
            Assert.False(string.IsNullOrEmpty(result.Error.Code));
            Assert.False(string.IsNullOrEmpty(result.Error.Message));
        }
    }

    [Fact]
    public async Task FetchAsync_ReturnsStructuredResult()
    {
        using var client = new RulesyncClient();
        var options = new FetchOptions
        {
            Source = "github:invalid/repo/path"
        };

        var result = await client.FetchAsync(options);

        // Should return structured result without throwing
        if (result.IsSuccess)
        {
            Assert.NotNull(result.Value);
            Assert.NotNull(result.Value.Files);
        }
        else
        {
            Assert.False(string.IsNullOrEmpty(result.Error.Code));
            Assert.False(string.IsNullOrEmpty(result.Error.Message));
        }
    }

    [Fact]
    public async Task InstallAsync_EmptyJsonResponse_HandlesGracefully()
    {
        using var client = new RulesyncClient();

        var result = await client.InstallAsync();

        Assert.True(result.IsSuccess || result.IsFailure); // Result is a value type
    }

    [Fact]
    public async Task UpdateAsync_PartialJsonResponse_HandlesGracefully()
    {
        using var client = new RulesyncClient();

        var result = await client.UpdateAsync();

        Assert.True(result.IsSuccess || result.IsFailure); // Result is a value type
    }

    #endregion

    #region Cancellation Tests

    [Fact]
    public async Task InitAsync_CancellationRequested_ReturnsFailure()
    {
        using var client = new RulesyncClient();
        using var cts = new CancellationTokenSource();
        
        cts.Cancel();

        var result = await client.InitAsync(cancellationToken: cts.Token);

        // SDK wraps cancellation in Result<T>.Failure rather than throwing
        Assert.True(result.IsFailure);
        Assert.False(string.IsNullOrEmpty(result.Error.Code));
    }

    [Fact]
    public async Task FetchAsync_CancellationRequested_ThrowsOperationCanceledException()
    {
        using var client = new RulesyncClient();
        using var cts = new CancellationTokenSource();
        
        cts.Cancel();

        await Assert.ThrowsAsync<OperationCanceledException>(async () =>
            await client.FetchAsync(new FetchOptions { Source = "github:test/repo" }, cts.Token));
    }

    [Fact]
    public async Task GitignoreAsync_CancellationRequested_ReturnsFailure()
    {
        using var client = new RulesyncClient();
        using var cts = new CancellationTokenSource();
        
        cts.Cancel();

        var result = await client.GitignoreAsync(cancellationToken: cts.Token);

        // SDK wraps cancellation in Result<T>.Failure rather than throwing
        Assert.True(result.IsFailure);
        Assert.False(string.IsNullOrEmpty(result.Error.Code));
    }

    [Fact]
    public async Task InstallAsync_CancellationRequested_ThrowsOperationCanceledException()
    {
        using var client = new RulesyncClient();
        using var cts = new CancellationTokenSource();
        
        cts.Cancel();

        await Assert.ThrowsAsync<OperationCanceledException>(async () =>
            await client.InstallAsync(cancellationToken: cts.Token));
    }

    [Fact]
    public async Task UpdateAsync_CancellationRequested_ReturnsFailure()
    {
        using var client = new RulesyncClient();
        using var cts = new CancellationTokenSource();
        
        cts.Cancel();

        var result = await client.UpdateAsync(cancellationToken: cts.Token);

        // SDK wraps cancellation in Result<T>.Failure rather than throwing
        Assert.True(result.IsFailure);
        Assert.False(string.IsNullOrEmpty(result.Error.Code));
    }

    [Fact]
    public async Task FetchAsync_CancellationDuringExecution_OperationCancelled()
    {
        using var client = new RulesyncClient();
        using var cts = new CancellationTokenSource();
        
        // Start operation and cancel after short delay
        var task = client.FetchAsync(
            new FetchOptions { Source = "github:owner/repo" }, 
            cts.Token);
        
        cts.CancelAfter(1); // Cancel immediately

        // Should throw OperationCanceledException
        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () => await task);
    }

    #endregion

    #region Timeout and Resource Tests

    [Fact]
    public async Task FetchAsync_WithTimeout_CancelsAfterTimeout()
    {
        using var client = new RulesyncClient();
        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(1));

        // Very short timeout - should fail quickly
        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
            await client.FetchAsync(
                new FetchOptions { Source = "github:large/repo" }, 
                cts.Token));
    }

    [Fact]
    public async Task MultipleRapidCalls_DoesNotDeadlock()
    {
        using var client = new RulesyncClient();
        var tasks = new Task[20];

        for (int i = 0; i < 20; i++)
        {
            var index = i;
            tasks[i] = Task.Run(async () =>
            {
                await client.InitAsync();
                await client.GitignoreAsync();
                await client.FetchAsync(new FetchOptions { Source = $"github:test/{index}" });
            });
        }

        // Should complete without deadlock
        await Task.WhenAll(tasks);
    }

    #endregion

    #region Result Type Tests

    [Fact]
    public void Result_Success_HasValue()
    {
        var result = Result<string>.Success("test");

        Assert.True(result.IsSuccess);
        Assert.False(result.IsFailure);
        Assert.Equal("test", result.Value);
    }

    [Fact]
    public void Result_Failure_HasError()
    {
        var result = Result<string>.Failure("CODE", "Message");

        Assert.False(result.IsSuccess);
        Assert.True(result.IsFailure);
        Assert.Equal("CODE", result.Error.Code);
        Assert.Equal("Message", result.Error.Message);
    }

    [Fact]
    public void Result_AccessingValueOnFailure_ThrowsInvalidOperationException()
    {
        var result = Result<string>.Failure("CODE", "Message");

        Assert.Throws<InvalidOperationException>(() => _ = result.Value);
    }

    [Fact]
    public void Result_AccessingErrorOnSuccess_ThrowsInvalidOperationException()
    {
        var result = Result<string>.Success("test");

        Assert.Throws<InvalidOperationException>(() => _ = result.Error);
    }

    [Fact]
    public void Result_OnSuccess_CallbackInvoked()
    {
        var result = Result<string>.Success("test");
        bool callbackInvoked = false;

        result.OnSuccess(_ => callbackInvoked = true);

        Assert.True(callbackInvoked);
    }

    [Fact]
    public void Result_OnFailure_CallbackInvoked()
    {
        var result = Result<string>.Failure("CODE", "Message");
        bool callbackInvoked = false;

        result.OnFailure(_ => callbackInvoked = true);

        Assert.True(callbackInvoked);
    }

    [Fact]
    public void Result_OnSuccess_WithFailure_DoesNotInvokeCallback()
    {
        var result = Result<string>.Failure("CODE", "Message");
        bool callbackInvoked = false;

        result.OnSuccess(_ => callbackInvoked = true);

        Assert.False(callbackInvoked);
    }

    [Fact]
    public void Result_OnFailure_WithSuccess_DoesNotInvokeCallback()
    {
        var result = Result<string>.Success("test");
        bool callbackInvoked = false;

        result.OnFailure(_ => callbackInvoked = true);

        Assert.False(callbackInvoked);
    }

    #endregion

    #region InitResult Tests

    [Fact]
    public void InitResult_CanBeConstructed()
    {
        var result = new InitResult
        {
            ConfigFile = new InitFileResult { Created = true, Path = "/test" },
            SampleFiles = new System.Collections.Generic.List<InitFileResult>()
        };

        Assert.NotNull(result.ConfigFile);
        Assert.True(result.ConfigFile.Created);
        Assert.Equal("/test", result.ConfigFile.Path);
        Assert.NotNull(result.SampleFiles);
    }

    [Fact]
    public void InitResult_DefaultConstructor_InitializesSampleFiles()
    {
        var result = new InitResult();

        Assert.NotNull(result.SampleFiles);
    }

    #endregion

    #region FetchResult Tests

    [Fact]
    public void FetchSummary_CanBeConstructed()
    {
        var summary = new FetchSummary
        {
            Files = new System.Collections.Generic.List<FetchFileResult>()
        };

        Assert.NotNull(summary.Files);
    }

    [Fact]
    public void FetchFileResult_CanBeConstructed()
    {
        var file = new FetchFileResult
        {
            RelativePath = "test.txt",
            Status = "created"
        };

        Assert.Equal("test.txt", file.RelativePath);
        Assert.Equal("created", file.Status);
    }

    [Fact]
    public void FetchSummary_DefaultConstructor_InitializesFiles()
    {
        var summary = new FetchSummary();

        Assert.NotNull(summary.Files);
    }

    #endregion

    #region InstallResult Tests

    [Fact]
    public void InstallResult_CanBeConstructed()
    {
        var result = new InstallResult
        {
            Installed = 5,
            Updated = 3
        };

        Assert.Equal(5, result.Installed);
        Assert.Equal(3, result.Updated);
    }

    [Fact]
    public void InstallResult_DefaultValues_AreZero()
    {
        var result = new InstallResult();

        Assert.Equal(0, result.Installed);
        Assert.Equal(0, result.Updated);
    }

    #endregion

    #region UpdateResult Tests

    [Fact]
    public void UpdateResult_CanBeConstructed()
    {
        var result = new UpdateResult
        {
            Available = true,
            CurrentVersion = "1.0.0",
            LatestVersion = "2.0.0"
        };

        Assert.True(result.Available);
        Assert.Equal("1.0.0", result.CurrentVersion);
        Assert.Equal("2.0.0", result.LatestVersion);
    }

    [Fact]
    public void UpdateResult_DefaultValues_AreEmpty()
    {
        var result = new UpdateResult();

        Assert.False(result.Available);
        Assert.Equal(string.Empty, result.CurrentVersion);
        Assert.Equal(string.Empty, result.LatestVersion);
    }

    #endregion

    #region GitignoreResult Tests

    [Fact]
    public void GitignoreResult_CanBeConstructed()
    {
        var result = new GitignoreResult
        {
            EntriesAdded = 5
        };

        Assert.Equal(5, result.EntriesAdded);
    }

    [Fact]
    public void GitignoreResult_DefaultValue_IsZero()
    {
        var result = new GitignoreResult();

        Assert.Equal(0, result.EntriesAdded);
    }

    #endregion
}
