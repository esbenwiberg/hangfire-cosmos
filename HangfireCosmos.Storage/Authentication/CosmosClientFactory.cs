using Azure.Core;
using Azure.Identity;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Logging;

namespace HangfireCosmos.Storage.Authentication;

/// <summary>
/// Factory for creating CosmosClient instances with various authentication methods.
/// </summary>
public class CosmosClientFactory : ICosmosClientFactory
{
    private readonly ILogger<CosmosClientFactory>? _logger;

    /// <summary>
    /// Initializes a new instance of the CosmosClientFactory class.
    /// </summary>
    /// <param name="logger">The optional logger instance.</param>
    public CosmosClientFactory(ILogger<CosmosClientFactory>? logger = null)
    {
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<CosmosClient> CreateClientAsync(CosmosStorageOptions options)
    {
        if (options == null)
            throw new ArgumentNullException(nameof(options));

        return options.AuthenticationMode switch
        {
            CosmosAuthenticationMode.ConnectionString => CreateFromConnectionString(options),
            CosmosAuthenticationMode.ManagedIdentity => await CreateFromManagedIdentityAsync(options),
            CosmosAuthenticationMode.ServicePrincipal => await CreateFromServicePrincipalAsync(options),
            _ => throw new ArgumentException($"Unsupported authentication mode: {options.AuthenticationMode}")
        };
    }

    private CosmosClient CreateFromConnectionString(CosmosStorageOptions options)
    {
        if (string.IsNullOrEmpty(options.ConnectionString))
            throw new ArgumentException("ConnectionString is required when using ConnectionString authentication mode");

        _logger?.LogInformation("Creating Cosmos client with connection string authentication");
        return new CosmosClient(options.ConnectionString, CreateCosmosClientOptions(options));
    }

    private async Task<CosmosClient> CreateFromManagedIdentityAsync(CosmosStorageOptions options)
    {
        if (string.IsNullOrEmpty(options.AccountEndpoint))
            throw new ArgumentException("AccountEndpoint is required when using ManagedIdentity authentication mode");

        _logger?.LogInformation("Creating Cosmos client with managed identity authentication. Client ID: {ClientId}",
            options.ManagedIdentityClientId ?? "System-assigned");
        
        var credential = string.IsNullOrEmpty(options.ManagedIdentityClientId)
            ? new DefaultAzureCredential() // System-assigned managed identity
            : new DefaultAzureCredential(new DefaultAzureCredentialOptions
            {
                ManagedIdentityClientId = options.ManagedIdentityClientId
            }); // User-assigned managed identity

        // Test the credential before creating the client
        try
        {
            var tokenRequestContext = new TokenRequestContext(new[] { "https://cosmos.azure.com/.default" });
            var token = await credential.GetTokenAsync(tokenRequestContext);
            _logger?.LogInformation("Successfully obtained access token for Cosmos DB. Token expires at: {ExpiresOn}", token.ExpiresOn);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to obtain access token for Cosmos DB using managed identity");
            throw new InvalidOperationException("Failed to authenticate with Cosmos DB using managed identity. Ensure the managed identity has appropriate permissions.", ex);
        }

        return new CosmosClient(options.AccountEndpoint, credential, CreateCosmosClientOptions(options));
    }

    private async Task<CosmosClient> CreateFromServicePrincipalAsync(CosmosStorageOptions options)
    {
        if (options.ServicePrincipal == null)
            throw new ArgumentException("ServicePrincipal configuration is required when using ServicePrincipal authentication mode");

        if (string.IsNullOrEmpty(options.AccountEndpoint))
            throw new ArgumentException("AccountEndpoint is required when using ServicePrincipal authentication mode");

        _logger?.LogInformation("Creating Cosmos client with service principal authentication. Client ID: {ClientId}",
            options.ServicePrincipal.ClientId);
        
        var credential = new ClientSecretCredential(
            options.ServicePrincipal.TenantId,
            options.ServicePrincipal.ClientId,
            options.ServicePrincipal.ClientSecret);

        // Test the credential before creating the client
        try
        {
            var tokenRequestContext = new TokenRequestContext(new[] { "https://cosmos.azure.com/.default" });
            var token = await credential.GetTokenAsync(tokenRequestContext);
            _logger?.LogInformation("Successfully obtained access token for Cosmos DB using service principal. Token expires at: {ExpiresOn}", token.ExpiresOn);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to obtain access token for Cosmos DB using service principal");
            throw new InvalidOperationException("Failed to authenticate with Cosmos DB using service principal. Verify the tenant ID, client ID, and client secret.", ex);
        }

        return new CosmosClient(options.AccountEndpoint, credential, CreateCosmosClientOptions(options));
    }

    private CosmosClientOptions CreateCosmosClientOptions(CosmosStorageOptions options)
    {
        var clientOptions = new CosmosClientOptions
        {
            ConsistencyLevel = options.ConsistencyLevel,
            ConnectionMode = options.Performance.ConnectionMode,
            MaxRetryAttemptsOnRateLimitedRequests = options.MaxRetryAttempts,
            MaxRetryWaitTimeOnRateLimitedRequests = options.RetryDelay,
            RequestTimeout = options.RequestTimeout,
            MaxRequestsPerTcpConnection = options.MaxConnectionLimit,
            EnableContentResponseOnWrite = options.EnableContentResponseOnWrite,
            SerializerOptions = new CosmosSerializationOptions
            {
                PropertyNamingPolicy = CosmosPropertyNamingPolicy.CamelCase
            }
        };

        // Add preferred regions if specified
        if (options.PreferredRegions?.Count > 0)
        {
            clientOptions.ApplicationPreferredRegions = options.PreferredRegions;
        }

        // Configure bulk execution if enabled
        if (options.BulkExecutionOptions.EnableBulkExecution)
        {
            clientOptions.AllowBulkExecution = true;
        }

        return clientOptions;
    }
}