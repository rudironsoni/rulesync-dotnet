#nullable enable

using System;
using System.IO;
using System.Threading.Tasks;
using Rulesync.Sdk.DotNet.Configuration;
using Rulesync.Sdk.DotNet.Models;
using Xunit;

namespace Rulesync.Sdk.DotNet.Tests;

/// <summary>
/// Test fixture that initializes a rulesync project before running tests.
/// This ensures the .rulesync/ directory exists for tests that require it.
/// </summary>
public class RulesyncTestFixture : IAsyncLifetime
{
    private readonly RulesyncClient _client;
    private string? _testDirectory;
    private bool _isInitialized;

    public RulesyncTestFixture()
    {
        _client = new RulesyncClient();
    }

    /// <summary>
    /// Gets the test directory where the .rulesync/ folder was initialized.
    /// </summary>
    public string TestDirectory => _testDirectory ?? throw new InvalidOperationException("Fixture not initialized");

    /// <summary>
    /// Gets whether the fixture has been successfully initialized.
    /// </summary>
    public bool IsInitialized => _isInitialized;

    /// <summary>
    /// Initializes the test fixture by running 'rulesync init' in a temp directory.
    /// </summary>
    public async Task InitializeAsync()
    {
        // Create a unique test directory
        _testDirectory = Path.Combine(Path.GetTempPath(), $"rulesync-test-{Guid.NewGuid():N}");
        Directory.CreateDirectory(_testDirectory);

        try
        {
            // Create client that works in the test directory
            var client = new RulesyncClient(new RulesyncOptions
            {
                WorkingDirectory = _testDirectory
            });

            // Run init to create .rulesync/ directory
            var initOptions = new InitOptions
            {
                Silent = true
            };

            var result = await client.InitAsync(initOptions);

            if (result.IsFailure)
            {
                // Init failed - clean up and mark as not initialized
                Cleanup();
                _isInitialized = false;
                return;
            }

            _isInitialized = true;
        }
        catch
        {
            // Init threw exception - clean up
            Cleanup();
            _isInitialized = false;
        }
    }

    /// <summary>
    /// Cleans up the test directory after tests complete.
    /// </summary>
    public Task DisposeAsync()
    {
        Cleanup();
        return Task.CompletedTask;
    }

    private void Cleanup()
    {
        try
        {
            if (!string.IsNullOrEmpty(_testDirectory) && Directory.Exists(_testDirectory))
            {
                Directory.Delete(_testDirectory, recursive: true);
            }
        }
        catch
        {
            // Ignore cleanup errors
        }
    }
}

/// <summary>
/// Collection definition for tests that require a rulesync project.
/// </summary>
[CollectionDefinition("RulesyncProject")]
public class RulesyncProjectCollection : ICollectionFixture<RulesyncTestFixture>
{
    // This class has no code, and is never created. Its purpose is simply
    // to be the place to apply [CollectionDefinition] and all the
    // ICollectionFixture<> interfaces.
}
