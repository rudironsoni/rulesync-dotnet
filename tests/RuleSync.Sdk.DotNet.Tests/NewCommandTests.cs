#nullable enable

using System;
using System.Threading.Tasks;
using Rulesync.Sdk.DotNet.Models;
using Xunit;

namespace Rulesync.Sdk.DotNet.Tests;

/// <summary>
/// Tests for the new CLI command methods added in full parity implementation.
/// </summary>
public class NewCommandTests
{
    #region Init Command Tests

    [Fact]
    public async Task InitAsync_DefaultOptions_CompletesSuccessfully()
    {
        using var client = new RulesyncClient();
        var options = new InitOptions();

        var result = await client.InitAsync(options);

        // Should complete without throwing
        Assert.NotNull(result);
    }

    [Fact]
    public async Task InitAsync_WithConfigPath_CompletesSuccessfully()
    {
        using var client = new RulesyncClient();
        var options = new InitOptions
        {
            ConfigPath = "/path/to/rulesync.jsonc"
        };

        var result = await client.InitAsync(options);

        Assert.NotNull(result);
    }

    [Fact]
    public async Task InitAsync_VerboseTrue_CompletesSuccessfully()
    {
        using var client = new RulesyncClient();
        var options = new InitOptions
        {
            Verbose = true
        };

        var result = await client.InitAsync(options);

        Assert.NotNull(result);
    }

    [Fact]
    public async Task InitAsync_SilentTrue_CompletesSuccessfully()
    {
        using var client = new RulesyncClient();
        var options = new InitOptions
        {
            Silent = true
        };

        var result = await client.InitAsync(options);

        Assert.NotNull(result);
    }

    [Fact]
    public void InitOptions_DefaultConstructor_HasExpectedDefaults()
    {
        var options = new InitOptions();

        Assert.Equal(string.Empty, options.ConfigPath);
        Assert.False(options.Verbose);
        Assert.False(options.Silent);
    }

    #endregion

    #region Gitignore Command Tests

    [Fact]
    public async Task GitignoreAsync_DefaultOptions_CompletesSuccessfully()
    {
        using var client = new RulesyncClient();
        var options = new GitignoreOptions();

        var result = await client.GitignoreAsync(options);

        Assert.NotNull(result);
    }

    [Fact]
    public async Task GitignoreAsync_WithConfigPath_CompletesSuccessfully()
    {
        using var client = new RulesyncClient();
        var options = new GitignoreOptions
        {
            ConfigPath = "/path/to/rulesync.jsonc"
        };

        var result = await client.GitignoreAsync(options);

        Assert.NotNull(result);
    }

    [Fact]
    public async Task GitignoreAsync_VerboseTrue_CompletesSuccessfully()
    {
        using var client = new RulesyncClient();
        var options = new GitignoreOptions
        {
            Verbose = true
        };

        var result = await client.GitignoreAsync(options);

        Assert.NotNull(result);
    }

    [Fact]
    public void GitignoreOptions_DefaultConstructor_HasExpectedDefaults()
    {
        var options = new GitignoreOptions();

        Assert.Equal(string.Empty, options.ConfigPath);
        Assert.False(options.Verbose);
        Assert.False(options.Silent);
    }

    #endregion

    #region Fetch Command Tests

    [Fact]
    public async Task FetchAsync_WithSource_CompletesSuccessfully()
    {
        using var client = new RulesyncClient();
        var options = new FetchOptions
        {
            Source = "github:owner/repo/path"
        };

        var result = await client.FetchAsync(options);

        Assert.NotNull(result);
    }

    [Fact]
    public async Task FetchAsync_WithAllOptions_CompletesSuccessfully()
    {
        using var client = new RulesyncClient();
        var options = new FetchOptions
        {
            Source = "github:owner/repo/path",
            Path = "./fetched-configs",
            Force = true,
            Token = "ghp_token123",
            Verbose = true,
            Silent = false
        };

        var result = await client.FetchAsync(options);

        Assert.NotNull(result);
    }

    [Fact]
    public void FetchOptions_DefaultConstructor_HasExpectedDefaults()
    {
        var options = new FetchOptions();

        Assert.Equal(string.Empty, options.Source);
        Assert.Equal(string.Empty, options.Path);
        Assert.False(options.Force);
        Assert.Equal(string.Empty, options.Token);
        Assert.False(options.Verbose);
        Assert.False(options.Silent);
    }

    #endregion

    #region Install Command Tests

    [Fact]
    public async Task InstallAsync_DefaultOptions_CompletesSuccessfully()
    {
        using var client = new RulesyncClient();
        var options = new InstallOptions();

        var result = await client.InstallAsync(options);

        Assert.NotNull(result);
    }

    [Fact]
    public async Task InstallAsync_WithUpdateFlag_CompletesSuccessfully()
    {
        using var client = new RulesyncClient();
        var options = new InstallOptions
        {
            Update = true
        };

        var result = await client.InstallAsync(options);

        Assert.NotNull(result);
    }

    [Fact]
    public async Task InstallAsync_WithFrozenFlag_CompletesSuccessfully()
    {
        using var client = new RulesyncClient();
        var options = new InstallOptions
        {
            Frozen = true
        };

        var result = await client.InstallAsync(options);

        Assert.NotNull(result);
    }

    [Fact]
    public async Task InstallAsync_WithToken_CompletesSuccessfully()
    {
        using var client = new RulesyncClient();
        var options = new InstallOptions
        {
            Token = "ghp_token123"
        };

        var result = await client.InstallAsync(options);

        Assert.NotNull(result);
    }

    [Fact]
    public void InstallOptions_DefaultConstructor_HasExpectedDefaults()
    {
        var options = new InstallOptions();

        Assert.False(options.Update);
        Assert.False(options.Frozen);
        Assert.Equal(string.Empty, options.Token);
        Assert.Equal(string.Empty, options.ConfigPath);
        Assert.False(options.Verbose);
        Assert.False(options.Silent);
    }

    #endregion

    #region Update Command Tests

    [Fact]
    public async Task UpdateAsync_DefaultOptions_CompletesSuccessfully()
    {
        using var client = new RulesyncClient();
        var options = new UpdateOptions();

        var result = await client.UpdateAsync(options);

        Assert.NotNull(result);
    }

    [Fact]
    public async Task UpdateAsync_WithCheckFlag_CompletesSuccessfully()
    {
        using var client = new RulesyncClient();
        var options = new UpdateOptions
        {
            Check = true
        };

        var result = await client.UpdateAsync(options);

        Assert.NotNull(result);
    }

    [Fact]
    public async Task UpdateAsync_WithForceFlag_CompletesSuccessfully()
    {
        using var client = new RulesyncClient();
        var options = new UpdateOptions
        {
            Force = true
        };

        var result = await client.UpdateAsync(options);

        Assert.NotNull(result);
    }

    [Fact]
    public async Task UpdateAsync_WithToken_CompletesSuccessfully()
    {
        using var client = new RulesyncClient();
        var options = new UpdateOptions
        {
            Token = "ghp_token123"
        };

        var result = await client.UpdateAsync(options);

        Assert.NotNull(result);
    }

    [Fact]
    public void UpdateOptions_DefaultConstructor_HasExpectedDefaults()
    {
        var options = new UpdateOptions();

        Assert.False(options.Check);
        Assert.False(options.Force);
        Assert.Equal(string.Empty, options.Token);
        Assert.False(options.Verbose);
        Assert.False(options.Silent);
    }

    #endregion

    #region Combined Options Tests

    [Fact]
    public async Task InitAsync_AllOptionsSet_CompletesSuccessfully()
    {
        using var client = new RulesyncClient();
        var options = new InitOptions
        {
            ConfigPath = "/path/to/config",
            Verbose = true,
            Silent = true
        };

        var result = await client.InitAsync(options);

        Assert.NotNull(result);
    }

    [Fact]
    public async Task GitignoreAsync_AllOptionsSet_CompletesSuccessfully()
    {
        using var client = new RulesyncClient();
        var options = new GitignoreOptions
        {
            ConfigPath = "/path/to/config",
            Verbose = true,
            Silent = true
        };

        var result = await client.GitignoreAsync(options);

        Assert.NotNull(result);
    }

    [Fact]
    public async Task InstallAsync_AllOptionsSet_CompletesSuccessfully()
    {
        using var client = new RulesyncClient();
        var options = new InstallOptions
        {
            Update = true,
            Frozen = true,
            Token = "token123",
            ConfigPath = "/path/to/config",
            Verbose = true,
            Silent = true
        };

        var result = await client.InstallAsync(options);

        Assert.NotNull(result);
    }

    [Fact]
    public async Task UpdateAsync_AllOptionsSet_CompletesSuccessfully()
    {
        using var client = new RulesyncClient();
        var options = new UpdateOptions
        {
            Check = true,
            Force = true,
            Token = "token123",
            Verbose = true,
            Silent = true
        };

        var result = await client.UpdateAsync(options);

        Assert.NotNull(result);
    }

    #endregion

    #region Null Options Tests

    [Fact]
    public async Task InitAsync_NullOptions_UsesDefaults()
    {
        using var client = new RulesyncClient();

        var result = await client.InitAsync(null);

        Assert.NotNull(result);
    }

    [Fact]
    public async Task GitignoreAsync_NullOptions_UsesDefaults()
    {
        using var client = new RulesyncClient();

        var result = await client.GitignoreAsync(null);

        Assert.NotNull(result);
    }

    [Fact]
    public async Task InstallAsync_NullOptions_UsesDefaults()
    {
        using var client = new RulesyncClient();

        var result = await client.InstallAsync(null);

        Assert.NotNull(result);
    }

    [Fact]
    public async Task UpdateAsync_NullOptions_UsesDefaults()
    {
        using var client = new RulesyncClient();

        var result = await client.UpdateAsync(null);

        Assert.NotNull(result);
    }

    #endregion

    #region Disposed Client Tests

    [Fact]
    public async Task InitAsync_AfterDispose_ThrowsObjectDisposedException()
    {
        var client = new RulesyncClient();
        client.Dispose();

        await Assert.ThrowsAsync<ObjectDisposedException>(async () =>
            await client.InitAsync());
    }

    [Fact]
    public async Task GitignoreAsync_AfterDispose_ThrowsObjectDisposedException()
    {
        var client = new RulesyncClient();
        client.Dispose();

        await Assert.ThrowsAsync<ObjectDisposedException>(async () =>
            await client.GitignoreAsync());
    }

    [Fact]
    public async Task FetchAsync_AfterDispose_ThrowsObjectDisposedException()
    {
        var client = new RulesyncClient();
        client.Dispose();

        await Assert.ThrowsAsync<ObjectDisposedException>(async () =>
            await client.FetchAsync(new FetchOptions { Source = "test" }));
    }

    [Fact]
    public async Task InstallAsync_AfterDispose_ThrowsObjectDisposedException()
    {
        var client = new RulesyncClient();
        client.Dispose();

        await Assert.ThrowsAsync<ObjectDisposedException>(async () =>
            await client.InstallAsync());
    }

    [Fact]
    public async Task UpdateAsync_AfterDispose_ThrowsObjectDisposedException()
    {
        var client = new RulesyncClient();
        client.Dispose();

        await Assert.ThrowsAsync<ObjectDisposedException>(async () =>
            await client.UpdateAsync());
    }

    #endregion
}
