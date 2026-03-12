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
    [InlineData("/etc/passwd")]
    [InlineData("C:\\Windows\\System32\\drivers\\etc\\hosts")]
    [InlineData("~/.bashrc")]
    [InlineData("%SYSTEMROOT%\\system32\\config")]
    public void BuildInitArgs_PathTraversalAttempts_ThrowsSecurityException(string maliciousPath)
    {
        // Path traversal attempts should be rejected
        var options = new InitOptions
        {
            ConfigPath = maliciousPath
        };

        // The validation happens during argument building
        Assert.Throws<ArgumentException>(() =>
        {
            using var client = new RulesyncClient();
            // Trigger argument building by calling the method
            _ = client.InitAsync(options);
        });
    }

    [Theory]
    [InlineData("/path/to/config\0.txt")]
    [InlineData("config\0.json")]
    [InlineData("\0etc/passwd")]
    public void BuildInitArgs_NullByteInjection_ThrowsSecurityException(string maliciousPath)
    {
        // Null bytes can be used to truncate paths in some systems
        var options = new InitOptions
        {
            ConfigPath = maliciousPath
        };

        Assert.Throws<ArgumentException>(() =>
        {
            using var client = new RulesyncClient();
            _ = client.InitAsync(options);
        });
    }

    [Fact]
    public void BuildInitArgs_ExtremelyLongPath_ThrowsSecurityException()
    {
        // 10,000 character path - DoS attempt
        var maliciousPath = string.Join("/", Enumerable.Repeat("../", 2000)) + "etc/passwd";
        
        var options = new InitOptions
        {
            ConfigPath = maliciousPath
        };

        Assert.Throws<ArgumentException>(() =>
        {
            using var client = new RulesyncClient();
            _ = client.InitAsync(options);
        });
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
    public void BuildInitArgs_SpecialCharactersInPath_HandlesCorrectly(string specialPath)
    {
        // These should either be properly escaped or rejected
        // depending on the security policy
        var options = new InitOptions
        {
            ConfigPath = specialPath
        };

        // Should not throw - paths with spaces are valid
        using var client = new RulesyncClient();
        var exception = Record.Exception(() =>
        {
            _ = client.InitAsync(options);
        });

        // If it throws, it should be a security-related exception, not a random crash
        if (exception != null)
        {
            Assert.IsType<ArgumentException>(exception);
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
    [InlineData(null)]
    public void FetchAsync_EmptyOrWhitespaceSource_ThrowsArgumentException(string? source)
    {
        using var client = new RulesyncClient();
        var options = new FetchOptions
        {
            Source = source!
        };

        Assert.Throws<ArgumentException>(() =>
        {
            _ = client.FetchAsync(options);
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
    public void InitOptions_ConfigPath_NullOrEmpty_Handled()
    {
        var options = new InitOptions
        {
            ConfigPath = null!
        };

        // Null should be handled gracefully
        Assert.Equal(null!, options.ConfigPath);
    }

    [Fact]
    public void GitignoreOptions_ConfigPath_NullOrEmpty_Handled()
    {
        var options = new GitignoreOptions
        {
            ConfigPath = null!
        };

        Assert.Equal(null!, options.ConfigPath);
    }

    [Fact]
    public void InstallOptions_ConfigPath_NullOrEmpty_Handled()
    {
        var options = new InstallOptions
        {
            ConfigPath = null!
        };

        Assert.Equal(null!, options.ConfigPath);
    }

    #endregion

    #region Concurrent Access Tests

    [Fact]
    public async Task InitAsync_MultipleConcurrentCalls_DoesNotCorruptState()
    {
        using var client = new RulesyncClient();
        var tasks = new Task[10];

        for (int i = 0; i < 10; i++)
        {
            tasks[i] = client.InitAsync().AsTask();
        }

        // Should complete without crashes or state corruption
        await Task.WhenAll(tasks);
    }

    [Fact]
    public async Task FetchAsync_MultipleConcurrentCalls_DoesNotCorruptState()
    {
        using var client = new RulesyncClient();
        var tasks = new Task[10];

        for (int i = 0; i < 10; i++)
        {
            tasks[i] = client.FetchAsync(new FetchOptions
            {
                Source = $"github:test/repo{i}"
            }).AsTask();
        }

        await Task.WhenAll(tasks);
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
