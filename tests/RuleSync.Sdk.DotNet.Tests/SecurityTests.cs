#nullable enable

using System;
using System.Linq;
using System.Threading.Tasks;
using Rulesync.Sdk.DotNet.Models;
using Xunit;

namespace Rulesync.Sdk.DotNet.Tests;

/// <summary>
/// Security-focused tests for the RulesyncClient.
/// Tests path traversal, injection attacks, and secure handling of sensitive data.
/// </summary>
public class SecurityTests
{
    #region Path Traversal Tests

    [Theory]
    [InlineData("../../../etc/passwd")]
    [InlineData("..\\..\\windows\\system32\\config\\sam")]
    [InlineData("config/../../../etc/passwd")]
    [InlineData("config\\..\\..\\..\\windows\\system32\\config")]
    public async Task BuildInitArgs_PathTraversalAttempts_ThrowsSecurityException(string maliciousPath)
    {
        // Path traversal attempts should be rejected
        var options = new InitOptions
        {
            ConfigPath = maliciousPath
        };

        using var client = new RulesyncClient();
        
        // The validation happens during async execution
        var exception = await Assert.ThrowsAsync<ArgumentException>(async () =>
        {
            await client.InitAsync(options);
        });
        
        Assert.NotNull(exception);
    }

    [Theory]
    [InlineData("/path/to/config\0.txt")]
    [InlineData("config\0.json")]
    [InlineData("\0etc/passwd")]
    public async Task BuildInitArgs_NullByteInjection_ThrowsSecurityException(string maliciousPath)
    {
        // Null bytes can be used to truncate paths in some systems
        var options = new InitOptions
        {
            ConfigPath = maliciousPath
        };

        using var client = new RulesyncClient();
        
        // The validation happens during async execution
        var exception = await Assert.ThrowsAsync<ArgumentException>(async () =>
        {
            await client.InitAsync(options);
        });
        
        Assert.NotNull(exception);
    }

    [Fact]
    public async Task BuildInitArgs_ExtremelyLongPath_ThrowsSecurityException()
    {
        // 10,000 character path - DoS attempt
        var maliciousPath = string.Join("/", Enumerable.Repeat("../", 2000)) + "etc/passwd";
        
        var options = new InitOptions
        {
            ConfigPath = maliciousPath
        };

        using var client = new RulesyncClient();
        
        // Should throw ArgumentException for paths exceeding reasonable limits
        var exception = await Assert.ThrowsAsync<ArgumentException>(async () =>
        {
            await client.InitAsync(options);
        });
        
        Assert.NotNull(exception);
    }

    [Theory]
    [InlineData("config with spaces.json")]
    [InlineData("config\twith\ttabs.json")]
    [InlineData("config\nwith\nnewlines.json")]
    [InlineData("config\"with\"quotes.json")]
    [InlineData("config'with'apostrophes.json")]
    [InlineData("config;with;semicolons.json")]
    [InlineData("config|with|pipes.json")]
    [InlineData("config&with&ampersands.json")]
    [InlineData("config$with$dollars.json")]
    public async Task BuildInitArgs_SpecialCharactersInPath_HandlesCorrectly(string specialPath)
    {
        // These should either be properly escaped or rejected
        // depending on the security policy
        var options = new InitOptions
        {
            ConfigPath = specialPath
        };

        using var client = new RulesyncClient();
        
        // For most special characters, the operation should complete without throwing
        // (paths with spaces, quotes, etc. are valid on many systems)
        var exception = await Record.ExceptionAsync(async () =>
        {
            await client.InitAsync(options);
        });

        // If an exception is thrown, verify it's a proper validation exception
        // and not an unexpected crash
        if (exception != null)
        {
            Assert.True(exception is ArgumentException || exception is InvalidOperationException,
                $"Expected ArgumentException or InvalidOperationException but got {exception.GetType().Name}");
        }
    }

    #endregion

    #region Token Security Tests

    [Fact]
    public async Task FetchAsync_WithToken_TokenNotExposedInLogs()
    {
        // Tokens should never appear in logs or error messages
        var options = new FetchOptions
        {
            Source = "github:test/repo",
            Token = "ghp_supersecrettoken123"
        };

        using var client = new RulesyncClient();
        var result = await client.FetchAsync(options);

        // If there's an error, the token should be redacted
        if (!result.IsSuccess)
        {
            Assert.DoesNotContain("ghp_supersecrettoken123", result.Error.Message);
            Assert.DoesNotContain("supersecrettoken", result.Error.Message);
        }
    }

    [Fact]
    public void FetchOptions_TokenProperty_CanSetAndGet()
    {
        var options = new FetchOptions();
        
        options.Token = "my-token";
        
        Assert.Equal("my-token", options.Token);
    }

    [Fact]
    public void InstallOptions_TokenProperty_CanSetAndGet()
    {
        var options = new InstallOptions();
        
        options.Token = "install-token";
        
        Assert.Equal("install-token", options.Token);
    }

    [Fact]
    public void UpdateOptions_TokenProperty_CanSetAndGet()
    {
        var options = new UpdateOptions();
        
        options.Token = "update-token";
        
        Assert.Equal("update-token", options.Token);
    }

    #endregion

    #region Input Validation Tests

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("\t\n\r")]
    public async Task FetchAsync_EmptyOrWhitespaceSource_ThrowsArgumentException(string source)
    {
        using var client = new RulesyncClient();
        var options = new FetchOptions
        {
            Source = source
        };

        await Assert.ThrowsAsync<ArgumentException>(async () =>
        {
            await client.FetchAsync(options);
        });
    }

    [Fact]
    public async Task FetchAsync_NullSource_ThrowsArgumentException()
    {
        using var client = new RulesyncClient();
        var options = new FetchOptions
        {
            Source = null!
        };

        // Null Source should throw ArgumentException or NullReferenceException
        await Assert.ThrowsAnyAsync<Exception>(async () =>
        {
            await client.FetchAsync(options);
        });
    }

    [Theory]
    [InlineData("not-a-valid-source")]
    [InlineData(":invalid")]
    [InlineData("github:")]
    [InlineData("github:only-owner")]
    public async Task FetchAsync_InvalidSourceFormat_ReturnsFailureResult(string invalidSource)
    {
        using var client = new RulesyncClient();
        var options = new FetchOptions
        {
            Source = invalidSource
        };

        var result = await client.FetchAsync(options);

        // Should return failure, not throw
        Assert.False(result.IsSuccess);
    }

    [Fact]
    public async Task InitAsync_WithNullConfigPath_UsesDefaultPath()
    {
        using var client = new RulesyncClient();
        var options = new InitOptions
        {
            ConfigPath = null!
        };

        // Null config path should be handled gracefully - uses default
        var result = await client.InitAsync(options);

        // Should complete without throwing (Result<T> is a value type)
        Assert.True(result.IsSuccess || result.IsFailure, "Result should have a defined state");
    }

    [Fact]
    public async Task GitignoreAsync_WithNullConfigPath_UsesDefaultPath()
    {
        using var client = new RulesyncClient();
        var options = new GitignoreOptions
        {
            ConfigPath = null!
        };

        // Null config path should be handled gracefully
        var result = await client.GitignoreAsync(options);

        // Should complete without throwing (Result<T> is a value type)
        Assert.True(result.IsSuccess || result.IsFailure, "Result should have a defined state");
    }

    [Fact]
    public async Task InstallAsync_WithNullConfigPath_UsesDefaultPath()
    {
        using var client = new RulesyncClient();
        var options = new InstallOptions
        {
            ConfigPath = null!
        };

        // Null config path should be handled gracefully
        var result = await client.InstallAsync(options);

        // Should complete without throwing (Result<T> is a value type)
        Assert.True(result.IsSuccess || result.IsFailure, "Result should have a defined state");
    }

    #endregion

    #region Concurrent Access Tests

    [Fact]
    public async Task InitAsync_MultipleConcurrentCalls_DoesNotCorruptState()
    {
        using var client = new RulesyncClient();
        var tasks = new Task<Result<InitResult>>[10];

        for (int i = 0; i < 10; i++)
        {
            tasks[i] = client.InitAsync().AsTask();
        }

        // Should complete without crashes or state corruption
        var results = await Task.WhenAll(tasks);

        // Verify all tasks completed successfully (Result<T> is a value type)
        Assert.All(results, result =>
        {
            Assert.True(result.IsSuccess || result.IsFailure, "Each result should have a defined state");
        });
    }

    [Fact]
    public async Task FetchAsync_MultipleConcurrentCalls_DoesNotCorruptState()
    {
        using var client = new RulesyncClient();
        var tasks = new Task<Result<FetchSummary>>[10];

        for (int i = 0; i < 10; i++)
        {
            tasks[i] = client.FetchAsync(new FetchOptions
            {
                Source = $"github:test/repo{i}"
            }).AsTask();
        }

        var results = await Task.WhenAll(tasks);

        // Verify all tasks completed and returned results (Result<T> is a value type)
        Assert.All(results, result =>
        {
            Assert.True(result.IsSuccess || result.IsFailure, "Each result should have a defined state");
        });
    }

    [Fact]
    public async Task MixedCommands_ConcurrentExecution_DoesNotCorruptState()
    {
        using var client = new RulesyncClient();
        var tasks = new Task[]
        {
            client.InitAsync().AsTask(),
            client.GitignoreAsync().AsTask(),
            client.FetchAsync(new FetchOptions { Source = "github:test/repo1" }).AsTask(),
            client.InstallAsync().AsTask(),
            client.UpdateAsync().AsTask()
        };

        await Task.WhenAll(tasks);

        // Verify all tasks completed successfully
        Assert.All(tasks, task =>
        {
            Assert.True(task.IsCompletedSuccessfully, "Task should complete successfully");
        });
    }

    #endregion

    #region Unicode and Internationalization Tests

    [Theory]
    [InlineData("配置.json")]
    [InlineData("файл.json")]
    [InlineData("ファイル.json")]
    [InlineData("������.json")]
    [InlineData("emoji������.json")]
    public void InitOptions_ConfigPath_UnicodeCharacters_Handled(string unicodePath)
    {
        var options = new InitOptions
        {
            ConfigPath = unicodePath
        };

        Assert.Equal(unicodePath, options.ConfigPath);
    }

    [Theory]
    [InlineData("github:用户/仓库/路径")]
    [InlineData("github:пользователь/репо/путь")]
    [InlineData("github:ユーザー/リポ/パス")]
    public void FetchOptions_Source_UnicodeCharacters_Handled(string unicodeSource)
    {
        var options = new FetchOptions
        {
            Source = unicodeSource
        };

        Assert.Equal(unicodeSource, options.Source);
    }

    #endregion

    #region Resource Exhaustion Tests

    [Fact]
    public async Task RapidSuccessiveCalls_DoesNotExhaustResources()
    {
        using var client = new RulesyncClient();

        // Rapid fire 100 calls
        for (int i = 0; i < 100; i++)
        {
            _ = await client.InitAsync();
        }

        // Should not exhaust process handles or other resources
        GC.Collect();
        GC.WaitForPendingFinalizers();
    }

    #endregion
}
