using Microsoft.Extensions.DependencyInjection;
using Microsoft.Azure.Cosmos;
using HangfireCosmos.Storage.Authentication;
using HangfireCosmos.Storage.Repository;
using HangfireCosmos.Storage.Resilience;

namespace HangfireCosmos.Storage.Extensions;

/// <summary>
/// Extension methods for IServiceCollection to register Hangfire Cosmos storage services.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds Hangfire Cosmos storage services to the service collection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configureOptions">Action to configure storage options.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddHangfireCosmosStorage(
        this IServiceCollection services,
        Action<CosmosStorageOptions> configureOptions)
    {
        var options = new CosmosStorageOptions();
        configureOptions(options);
        options.Validate();

        services.AddSingleton(options);
        services.AddSingleton<ICosmosClientFactory, CosmosClientFactory>();
        
        // Register CosmosClient
        services.AddSingleton<CosmosClient>(sp =>
        {
            var factory = sp.GetService<ICosmosClientFactory>() ??
                         new CosmosClientFactory(sp.GetService<Microsoft.Extensions.Logging.ILogger<CosmosClientFactory>>());
            return factory.CreateClientAsync(options).GetAwaiter().GetResult();
        });

        // Register circuit breaker if enabled
        if (options.CircuitBreaker.Enabled)
        {
            services.AddSingleton(options.CircuitBreaker);
            services.AddSingleton<CosmosCircuitBreaker>();
            
            // Register the base repository
            services.AddSingleton<CosmosDocumentRepository>();
            
            // Register the resilient wrapper as the main interface
            services.AddSingleton<ICosmosDocumentRepository>(sp =>
            {
                var baseRepository = sp.GetRequiredService<CosmosDocumentRepository>();
                var circuitBreaker = sp.GetRequiredService<CosmosCircuitBreaker>();
                var logger = sp.GetService<Microsoft.Extensions.Logging.ILogger<ResilientCosmosDocumentRepository>>();
                
                return new ResilientCosmosDocumentRepository(baseRepository, circuitBreaker, logger!);
            });
        }
        else
        {
            // Register the base repository directly
            services.AddSingleton<ICosmosDocumentRepository, CosmosDocumentRepository>();
        }

        services.AddSingleton<CosmosStorage>();
        
        return services;
    }

    /// <summary>
    /// Adds Hangfire Cosmos storage services with managed identity authentication.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="accountEndpoint">The Cosmos DB account endpoint.</param>
    /// <param name="configureOptions">Optional action to configure storage options.</param>
    /// <param name="managedIdentityClientId">Optional client ID for user-assigned managed identity.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddHangfireCosmosStorageWithManagedIdentity(
        this IServiceCollection services,
        string accountEndpoint,
        Action<CosmosStorageOptions>? configureOptions = null,
        string? managedIdentityClientId = null)
    {
        return services.AddHangfireCosmosStorage(options =>
        {
            options.AuthenticationMode = CosmosAuthenticationMode.ManagedIdentity;
            options.AccountEndpoint = accountEndpoint;
            options.ManagedIdentityClientId = managedIdentityClientId;
            
            configureOptions?.Invoke(options);
        });
    }

    /// <summary>
    /// Adds Hangfire Cosmos storage services with service principal authentication.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="accountEndpoint">The Cosmos DB account endpoint.</param>
    /// <param name="tenantId">The Azure AD tenant ID.</param>
    /// <param name="clientId">The Azure AD application (client) ID.</param>
    /// <param name="clientSecret">The Azure AD application client secret.</param>
    /// <param name="configureOptions">Optional action to configure storage options.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddHangfireCosmosStorageWithServicePrincipal(
        this IServiceCollection services,
        string accountEndpoint,
        string tenantId,
        string clientId,
        string clientSecret,
        Action<CosmosStorageOptions>? configureOptions = null)
    {
        return services.AddHangfireCosmosStorage(options =>
        {
            options.AuthenticationMode = CosmosAuthenticationMode.ServicePrincipal;
            options.AccountEndpoint = accountEndpoint;
            options.ServicePrincipal = new ServicePrincipalOptions
            {
                TenantId = tenantId,
                ClientId = clientId,
                ClientSecret = clientSecret
            };
            
            configureOptions?.Invoke(options);
        });
    }

    /// <summary>
    /// Creates a CosmosStorage instance with the specified connection string and options.
    /// </summary>
    /// <param name="connectionString">The Cosmos DB connection string.</param>
    /// <param name="configureOptions">Optional action to configure storage options.</param>
    /// <returns>A configured CosmosStorage instance.</returns>
    internal static CosmosStorage CreateCosmosStorage(string connectionString, Action<CosmosStorageOptions>? configureOptions = null)
    {
        var options = new CosmosStorageOptions
        {
            AuthenticationMode = CosmosAuthenticationMode.ConnectionString,
            ConnectionString = connectionString
        };
        
        configureOptions?.Invoke(options);
        options.Validate();

        var cosmosClient = new CosmosClient(connectionString);
        return new CosmosStorage(cosmosClient, options);
    }

    /// <summary>
    /// Creates a CosmosStorage instance with the specified connection string and options.
    /// </summary>
    /// <param name="connectionString">The Cosmos DB connection string.</param>
    /// <param name="options">The storage options.</param>
    /// <returns>A configured CosmosStorage instance.</returns>
    internal static CosmosStorage CreateCosmosStorage(string connectionString, CosmosStorageOptions options)
    {
        options.AuthenticationMode = CosmosAuthenticationMode.ConnectionString;
        options.ConnectionString = connectionString;
        options.Validate();

        var cosmosClient = new CosmosClient(connectionString);
        return new CosmosStorage(cosmosClient, options);
    }

    /// <summary>
    /// Creates a CosmosStorage instance with the specified connection string and database name.
    /// </summary>
    /// <param name="connectionString">The Cosmos DB connection string.</param>
    /// <param name="databaseName">The database name.</param>
    /// <returns>A configured CosmosStorage instance.</returns>
    internal static CosmosStorage CreateCosmosStorage(string connectionString, string databaseName)
    {
        var options = new CosmosStorageOptions
        {
            AuthenticationMode = CosmosAuthenticationMode.ConnectionString,
            ConnectionString = connectionString,
            DatabaseName = databaseName
        };
        
        options.Validate();

        var cosmosClient = new CosmosClient(connectionString);
        return new CosmosStorage(cosmosClient, options);
    }
}