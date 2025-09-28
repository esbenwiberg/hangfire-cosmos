using Hangfire;
using Microsoft.Azure.Cosmos;

namespace HangfireCosmos.Storage.Extensions;

/// <summary>
/// Extension methods for IGlobalConfiguration to configure Hangfire with Cosmos DB storage.
/// </summary>
public static class HangfireConfigurationExtensions
{
    /// <summary>
    /// Configures Hangfire to use Cosmos DB storage.
    /// </summary>
    /// <param name="configuration">The global configuration.</param>
    /// <param name="connectionString">The Cosmos DB connection string.</param>
    /// <param name="configureOptions">Optional configuration for storage options.</param>
    /// <returns>The global configuration for chaining.</returns>
    public static IGlobalConfiguration UseCosmosStorage(
        this IGlobalConfiguration configuration,
        string connectionString,
        Action<CosmosStorageOptions>? configureOptions = null)
    {
        if (configuration == null) throw new ArgumentNullException(nameof(configuration));
        if (string.IsNullOrEmpty(connectionString)) throw new ArgumentNullException(nameof(connectionString));

        var storage = ServiceCollectionExtensions.CreateCosmosStorage(connectionString, configureOptions);
        return configuration.UseStorage(storage);
    }

    /// <summary>
    /// Configures Hangfire to use Cosmos DB storage with the specified options.
    /// </summary>
    /// <param name="configuration">The global configuration.</param>
    /// <param name="connectionString">The Cosmos DB connection string.</param>
    /// <param name="options">The storage options.</param>
    /// <returns>The global configuration for chaining.</returns>
    public static IGlobalConfiguration UseCosmosStorage(
        this IGlobalConfiguration configuration,
        string connectionString,
        CosmosStorageOptions options)
    {
        if (configuration == null) throw new ArgumentNullException(nameof(configuration));
        if (options == null) throw new ArgumentNullException(nameof(options));

        var storage = ServiceCollectionExtensions.CreateCosmosStorage(connectionString, options);
        return configuration.UseStorage(storage);
    }

    /// <summary>
    /// Configures Hangfire to use Cosmos DB storage with the specified database name.
    /// </summary>
    /// <param name="configuration">The global configuration.</param>
    /// <param name="connectionString">The Cosmos DB connection string.</param>
    /// <param name="databaseName">The database name.</param>
    /// <returns>The global configuration for chaining.</returns>
    public static IGlobalConfiguration UseCosmosStorage(
        this IGlobalConfiguration configuration,
        string connectionString,
        string databaseName)
    {
        if (configuration == null) throw new ArgumentNullException(nameof(configuration));
        if (string.IsNullOrEmpty(databaseName)) throw new ArgumentNullException(nameof(databaseName));

        var storage = ServiceCollectionExtensions.CreateCosmosStorage(connectionString, databaseName);
        return configuration.UseStorage(storage);
    }

    /// <summary>
    /// Configures Hangfire to use Cosmos DB storage with an existing CosmosClient.
    /// </summary>
    /// <param name="configuration">The global configuration.</param>
    /// <param name="cosmosClient">The Cosmos DB client.</param>
    /// <param name="configureOptions">Optional configuration for storage options.</param>
    /// <returns>The global configuration for chaining.</returns>
    public static IGlobalConfiguration UseCosmosStorage(
        this IGlobalConfiguration configuration,
        CosmosClient cosmosClient,
        Action<CosmosStorageOptions>? configureOptions = null)
    {
        if (configuration == null) throw new ArgumentNullException(nameof(configuration));
        if (cosmosClient == null) throw new ArgumentNullException(nameof(cosmosClient));

        var options = new CosmosStorageOptions();
        configureOptions?.Invoke(options);
        options.Validate();

        var storage = new CosmosStorage(cosmosClient, options);
        return configuration.UseStorage(storage);
    }

    /// <summary>
    /// Configures Hangfire to use Cosmos DB storage with an existing CosmosClient and options.
    /// </summary>
    /// <param name="configuration">The global configuration.</param>
    /// <param name="cosmosClient">The Cosmos DB client.</param>
    /// <param name="options">The storage options.</param>
    /// <returns>The global configuration for chaining.</returns>
    public static IGlobalConfiguration UseCosmosStorage(
        this IGlobalConfiguration configuration,
        CosmosClient cosmosClient,
        CosmosStorageOptions options)
    {
        if (configuration == null) throw new ArgumentNullException(nameof(configuration));
        if (cosmosClient == null) throw new ArgumentNullException(nameof(cosmosClient));
        if (options == null) throw new ArgumentNullException(nameof(options));

        var storage = new CosmosStorage(cosmosClient, options);
        return configuration.UseStorage(storage);
    }
}