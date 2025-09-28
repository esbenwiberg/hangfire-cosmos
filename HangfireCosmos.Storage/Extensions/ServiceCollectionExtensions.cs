using Hangfire;
using HangfireCosmos.Storage.Connection;
using HangfireCosmos.Storage.Monitoring;
using HangfireCosmos.Storage.Repository;
using Microsoft.Azure.Cosmos;

namespace HangfireCosmos.Storage.Extensions;

/// <summary>
/// Extension methods for configuring Hangfire Cosmos DB storage.
/// Note: This class provides extension methods that can be used when Microsoft.Extensions.DependencyInjection is available.
/// For basic usage without DI container, use the CosmosStorage class directly.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Creates a new CosmosStorage instance with the specified connection string and options.
    /// </summary>
    /// <param name="connectionString">The Cosmos DB connection string.</param>
    /// <param name="configureOptions">Optional configuration for storage options.</param>
    /// <returns>A configured CosmosStorage instance.</returns>
    public static CosmosStorage CreateCosmosStorage(
        string connectionString,
        Action<CosmosStorageOptions>? configureOptions = null)
    {
        if (string.IsNullOrEmpty(connectionString)) throw new ArgumentNullException(nameof(connectionString));

        // Configure options
        var options = new CosmosStorageOptions();
        configureOptions?.Invoke(options);
        options.Validate();

        // Create Cosmos client
        var clientOptions = new CosmosClientOptions
        {
            SerializerOptions = new CosmosSerializationOptions
            {
                PropertyNamingPolicy = CosmosPropertyNamingPolicy.CamelCase
            },
            ConnectionMode = options.Performance.ConnectionMode,
            ConsistencyLevel = options.ConsistencyLevel,
            RequestTimeout = options.RequestTimeout,
            MaxRetryAttemptsOnRateLimitedRequests = options.MaxRetryAttempts,
            MaxRetryWaitTimeOnRateLimitedRequests = options.RetryDelay
        };

        if (options.PreferredRegions.Any())
        {
            clientOptions.ApplicationPreferredRegions = options.PreferredRegions;
        }

        var cosmosClient = new CosmosClient(connectionString, clientOptions);

        // Create and return storage
        return new CosmosStorage(cosmosClient, options);
    }

    /// <summary>
    /// Creates a new CosmosStorage instance with the specified connection string and options.
    /// </summary>
    /// <param name="connectionString">The Cosmos DB connection string.</param>
    /// <param name="options">The storage options.</param>
    /// <returns>A configured CosmosStorage instance.</returns>
    public static CosmosStorage CreateCosmosStorage(
        string connectionString,
        CosmosStorageOptions options)
    {
        if (options == null) throw new ArgumentNullException(nameof(options));
        
        return CreateCosmosStorage(connectionString, opts =>
        {
            opts.DatabaseName = options.DatabaseName;
            opts.JobsContainerName = options.JobsContainerName;
            opts.ServersContainerName = options.ServersContainerName;
            opts.QueuesContainerName = options.QueuesContainerName;
            opts.LocksContainerName = options.LocksContainerName;
            opts.CountersContainerName = options.CountersContainerName;
            opts.HashesContainerName = options.HashesContainerName;
            opts.ListsContainerName = options.ListsContainerName;
            opts.SetsContainerName = options.SetsContainerName;
            opts.DefaultJobExpiration = options.DefaultJobExpiration;
            opts.LockTimeout = options.LockTimeout;
            opts.RequestTimeout = options.RequestTimeout;
            opts.MaxRetryAttempts = options.MaxRetryAttempts;
            opts.RetryDelay = options.RetryDelay;
            opts.ConsistencyLevel = options.ConsistencyLevel;
            opts.DefaultThroughput = options.DefaultThroughput;
            opts.AutoCreateDatabase = options.AutoCreateDatabase;
            opts.AutoCreateContainers = options.AutoCreateContainers;
            opts.EnableCaching = options.EnableCaching;
            opts.CacheExpiration = options.CacheExpiration;
            opts.ServerTimeout = options.ServerTimeout;
            opts.MaxConnectionLimit = options.MaxConnectionLimit;
            opts.PreferredRegions = options.PreferredRegions;
            opts.EnableContentResponseOnWrite = options.EnableContentResponseOnWrite;
        });
    }

    /// <summary>
    /// Creates a new CosmosStorage instance with the specified connection string and database name.
    /// </summary>
    /// <param name="connectionString">The Cosmos DB connection string.</param>
    /// <param name="databaseName">The database name.</param>
    /// <returns>A configured CosmosStorage instance.</returns>
    public static CosmosStorage CreateCosmosStorage(
        string connectionString,
        string databaseName)
    {
        return CreateCosmosStorage(connectionString, options =>
        {
            options.DatabaseName = databaseName;
        });
    }
}