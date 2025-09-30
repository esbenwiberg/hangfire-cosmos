using Hangfire;
using Microsoft.Azure.Cosmos;
using HangfireCosmos.Storage.Authentication;
using HangfireCosmos.Storage.Resilience;

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

    /// <summary>
    /// Configures Hangfire to use Cosmos DB storage with managed identity authentication.
    /// </summary>
    /// <param name="configuration">The Hangfire global configuration.</param>
    /// <param name="accountEndpoint">The Cosmos DB account endpoint.</param>
    /// <param name="configureOptions">Optional action to configure storage options.</param>
    /// <param name="managedIdentityClientId">Optional client ID for user-assigned managed identity.</param>
    /// <returns>The updated global configuration.</returns>
    public static IGlobalConfiguration UseCosmosStorageWithManagedIdentity(
        this IGlobalConfiguration configuration,
        string accountEndpoint,
        Action<CosmosStorageOptions>? configureOptions = null,
        string? managedIdentityClientId = null)
    {
        if (configuration == null) throw new ArgumentNullException(nameof(configuration));
        if (string.IsNullOrWhiteSpace(accountEndpoint)) throw new ArgumentException("Account endpoint cannot be null or empty.", nameof(accountEndpoint));

        var options = new CosmosStorageOptions
        {
            AuthenticationMode = CosmosAuthenticationMode.ManagedIdentity,
            AccountEndpoint = accountEndpoint,
            ManagedIdentityClientId = managedIdentityClientId
        };
        
        configureOptions?.Invoke(options);
        options.Validate();

        // Create client factory without logger for extension methods
        var clientFactory = new CosmosClientFactory();
        
        var cosmosClient = clientFactory.CreateClientAsync(options).GetAwaiter().GetResult();
        var storage = new CosmosStorage(cosmosClient, options);
        
        return configuration.UseStorage(storage);
    }

    /// <summary>
    /// Configures Hangfire to use Cosmos DB storage with service principal authentication.
    /// </summary>
    /// <param name="configuration">The Hangfire global configuration.</param>
    /// <param name="accountEndpoint">The Cosmos DB account endpoint.</param>
    /// <param name="tenantId">The Azure AD tenant ID.</param>
    /// <param name="clientId">The Azure AD application (client) ID.</param>
    /// <param name="clientSecret">The Azure AD application client secret.</param>
    /// <param name="configureOptions">Optional action to configure storage options.</param>
    /// <returns>The updated global configuration.</returns>
    public static IGlobalConfiguration UseCosmosStorageWithServicePrincipal(
        this IGlobalConfiguration configuration,
        string accountEndpoint,
        string tenantId,
        string clientId,
        string clientSecret,
        Action<CosmosStorageOptions>? configureOptions = null)
    {
        if (configuration == null) throw new ArgumentNullException(nameof(configuration));
        if (string.IsNullOrWhiteSpace(accountEndpoint)) throw new ArgumentException("Account endpoint cannot be null or empty.", nameof(accountEndpoint));
        if (string.IsNullOrWhiteSpace(tenantId)) throw new ArgumentException("Tenant ID cannot be null or empty.", nameof(tenantId));
        if (string.IsNullOrWhiteSpace(clientId)) throw new ArgumentException("Client ID cannot be null or empty.", nameof(clientId));
        if (string.IsNullOrWhiteSpace(clientSecret)) throw new ArgumentException("Client secret cannot be null or empty.", nameof(clientSecret));

        var options = new CosmosStorageOptions
        {
            AuthenticationMode = CosmosAuthenticationMode.ServicePrincipal,
            AccountEndpoint = accountEndpoint,
            ServicePrincipal = new ServicePrincipalOptions
            {
                TenantId = tenantId,
                ClientId = clientId,
                ClientSecret = clientSecret
            }
        };
        
        configureOptions?.Invoke(options);
        options.Validate();

        // Create client factory without logger for extension methods
        var clientFactory = new CosmosClientFactory();
        
        var cosmosClient = clientFactory.CreateClientAsync(options).GetAwaiter().GetResult();
        var storage = new CosmosStorage(cosmosClient, options);
        
        return configuration.UseStorage(storage);
    }
}