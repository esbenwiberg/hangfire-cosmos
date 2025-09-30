using Microsoft.Azure.Cosmos;
using HangfireCosmos.Storage.Resilience;

namespace HangfireCosmos.Storage;

/// <summary>
/// Defines the collection organization strategy.
/// </summary>
public enum CollectionStrategy
{
    /// <summary>
    /// Each document type gets its own dedicated container (8 containers total).
    /// Best for high-volume, performance-critical deployments.
    /// </summary>
    Dedicated,
    
    /// <summary>
    /// Documents are consolidated into 3 logical containers.
    /// Best for cost-effective, smaller deployments.
    /// </summary>
    Consolidated
}

/// <summary>
/// Authentication strategy for Cosmos DB connections.
/// </summary>
public enum CosmosAuthenticationMode
{
    /// <summary>
    /// Use connection string for authentication (default).
    /// </summary>
    ConnectionString,
    
    /// <summary>
    /// Use Azure Managed Identity for authentication.
    /// </summary>
    ManagedIdentity,
    
    /// <summary>
    /// Use Azure Service Principal for authentication.
    /// </summary>
    ServicePrincipal
}

/// <summary>
/// Configuration options for the Cosmos DB storage provider.
/// </summary>
public class CosmosStorageOptions
{
    /// <summary>
    /// Gets or sets the name of the Cosmos database.
    /// </summary>
    public string DatabaseName { get; set; } = "hangfire";

    /// <summary>
    /// Gets or sets the collection strategy for container organization.
    /// </summary>
    public CollectionStrategy CollectionStrategy { get; set; } = CollectionStrategy.Dedicated;

    /// <summary>
    /// Gets or sets the authentication mode for Cosmos DB connection.
    /// </summary>
    public CosmosAuthenticationMode AuthenticationMode { get; set; } = CosmosAuthenticationMode.ConnectionString;

    /// <summary>
    /// Gets or sets the Cosmos DB connection string (required for ConnectionString authentication mode).
    /// </summary>
    public string? ConnectionString { get; set; }

    /// <summary>
    /// Gets or sets the Cosmos DB account endpoint (required for ManagedIdentity and ServicePrincipal authentication modes).
    /// </summary>
    public string? AccountEndpoint { get; set; }

    /// <summary>
    /// Gets or sets the client ID for user-assigned managed identity (optional for ManagedIdentity authentication mode).
    /// If not specified, system-assigned managed identity will be used.
    /// </summary>
    public string? ManagedIdentityClientId { get; set; }

    /// <summary>
    /// Gets or sets the service principal configuration (required for ServicePrincipal authentication mode).
    /// </summary>
    public ServicePrincipalOptions? ServicePrincipal { get; set; }

    /// <summary>
    /// Gets or sets the name of the Jobs container.
    /// </summary>
    public string JobsContainerName { get; set; } = "jobs";

    /// <summary>
    /// Gets or sets the name of the Servers container.
    /// </summary>
    public string ServersContainerName { get; set; } = "servers";

    /// <summary>
    /// Gets or sets the name of the Locks container.
    /// </summary>
    public string LocksContainerName { get; set; } = "locks";

    /// <summary>
    /// Gets or sets the name of the Queues container.
    /// </summary>
    public string QueuesContainerName { get; set; } = "queues";

    /// <summary>
    /// Gets or sets the name of the Sets container.
    /// </summary>
    public string SetsContainerName { get; set; } = "sets";

    /// <summary>
    /// Gets or sets the name of the Hashes container.
    /// </summary>
    public string HashesContainerName { get; set; } = "hashes";

    /// <summary>
    /// Gets or sets the name of the Lists container.
    /// </summary>
    public string ListsContainerName { get; set; } = "lists";

    /// <summary>
    /// Gets or sets the name of the Counters container.
    /// </summary>
    public string CountersContainerName { get; set; } = "counters";

    /// <summary>
    /// Gets or sets the name of the consolidated metadata container (used in Consolidated strategy).
    /// Contains servers, locks, queues, and counters documents.
    /// </summary>
    public string MetadataContainerName { get; set; } = "metadata";

    /// <summary>
    /// Gets or sets the name of the consolidated collections container (used in Consolidated strategy).
    /// Contains sets, hashes, and lists documents.
    /// </summary>
    public string CollectionsContainerName { get; set; } = "collections";

    /// <summary>
    /// Gets or sets the default throughput (RU/s) for containers or database (when using shared throughput).
    /// </summary>
    public int DefaultThroughput { get; set; } = 400;

    /// <summary>
    /// Gets or sets a value indicating whether to use shared throughput at the database level.
    /// When true, throughput is provisioned at the database level and shared across all containers.
    /// When false, each container gets its own dedicated throughput allocation.
    /// </summary>
    public bool UseSharedThroughput { get; set; } = false;

    /// <summary>
    /// Gets or sets the consistency level for Cosmos DB operations.
    /// </summary>
    public ConsistencyLevel ConsistencyLevel { get; set; } = ConsistencyLevel.Session;

    /// <summary>
    /// Gets or sets the default job expiration time.
    /// </summary>
    public TimeSpan DefaultJobExpiration { get; set; } = TimeSpan.FromDays(7);

    /// <summary>
    /// Gets or sets the server timeout for heartbeat operations.
    /// </summary>
    public TimeSpan ServerTimeout { get; set; } = TimeSpan.FromMinutes(5);

    /// <summary>
    /// Gets or sets the lock timeout for distributed locking.
    /// </summary>
    public TimeSpan LockTimeout { get; set; } = TimeSpan.FromMinutes(1);

    /// <summary>
    /// Gets or sets a value indicating whether caching is enabled.
    /// </summary>
    public bool EnableCaching { get; set; } = true;

    /// <summary>
    /// Gets or sets the cache expiration time.
    /// </summary>
    public TimeSpan CacheExpiration { get; set; } = TimeSpan.FromMinutes(5);

    /// <summary>
    /// Gets or sets the maximum number of retry attempts for failed operations.
    /// </summary>
    public int MaxRetryAttempts { get; set; } = 5;

    /// <summary>
    /// Gets or sets the initial retry delay.
    /// </summary>
    public TimeSpan RetryDelay { get; set; } = TimeSpan.FromMilliseconds(100);

    /// <summary>
    /// Gets or sets a value indicating whether to automatically create the database if it doesn't exist.
    /// </summary>
    public bool AutoCreateDatabase { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether to automatically create containers if they don't exist.
    /// </summary>
    public bool AutoCreateContainers { get; set; } = true;

    /// <summary>
    /// Gets or sets the request timeout for Cosmos DB operations.
    /// </summary>
    public TimeSpan RequestTimeout { get; set; } = TimeSpan.FromSeconds(30);

    /// <summary>
    /// Gets or sets the maximum number of concurrent connections.
    /// </summary>
    public int MaxConnectionLimit { get; set; } = 50;

    /// <summary>
    /// Gets or sets the preferred regions for multi-region deployments.
    /// </summary>
    public List<string> PreferredRegions { get; set; } = new();

    /// <summary>
    /// Gets or sets a value indicating whether to enable content response on write operations.
    /// </summary>
    public bool EnableContentResponseOnWrite { get; set; } = false;

    /// <summary>
    /// Gets or sets the bulk execution options for batch operations.
    /// </summary>
    public BulkExecutionOptions BulkExecutionOptions { get; set; } = new();

    /// <summary>
    /// Gets or sets custom indexing policies for containers.
    /// </summary>
    public Dictionary<string, IndexingPolicy> CustomIndexingPolicies { get; set; } = new();

    /// <summary>
    /// Gets or sets the TTL (Time To Live) settings for different document types.
    /// </summary>
    public TtlSettings TtlSettings { get; set; } = new();

    /// <summary>
    /// Gets or sets performance tuning options.
    /// </summary>
    public PerformanceOptions Performance { get; set; } = new();

    /// <summary>
    /// Gets or sets circuit breaker configuration options.
    /// </summary>
    public CircuitBreakerOptions CircuitBreaker { get; set; } = new();

    /// <summary>
    /// Validates the configuration options.
    /// </summary>
    /// <exception cref="ArgumentException">Thrown when configuration is invalid.</exception>
    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(DatabaseName))
            throw new ArgumentException("DatabaseName cannot be null or empty.", nameof(DatabaseName));

        if (string.IsNullOrWhiteSpace(JobsContainerName))
            throw new ArgumentException("JobsContainerName cannot be null or empty.", nameof(JobsContainerName));

        // Validate authentication configuration
        ValidateAuthenticationConfiguration();

        // Validate container names based on collection strategy
        if (CollectionStrategy == CollectionStrategy.Dedicated)
        {
            ValidateDedicatedContainerNames();
        }
        else if (CollectionStrategy == CollectionStrategy.Consolidated)
        {
            ValidateConsolidatedContainerNames();
        }

        if (DefaultThroughput < 400)
            throw new ArgumentException("DefaultThroughput must be at least 400 RU/s.", nameof(DefaultThroughput));

        // Validate shared throughput configuration
        if (UseSharedThroughput && DefaultThroughput < 400)
            throw new ArgumentException("When using shared throughput, DefaultThroughput must be at least 400 RU/s for the database.", nameof(DefaultThroughput));

        if (MaxRetryAttempts < 0)
            throw new ArgumentException("MaxRetryAttempts cannot be negative.", nameof(MaxRetryAttempts));

        if (RetryDelay < TimeSpan.Zero)
            throw new ArgumentException("RetryDelay cannot be negative.", nameof(RetryDelay));

        if (RequestTimeout <= TimeSpan.Zero)
            throw new ArgumentException("RequestTimeout must be positive.", nameof(RequestTimeout));

        if (MaxConnectionLimit <= 0)
            throw new ArgumentException("MaxConnectionLimit must be positive.", nameof(MaxConnectionLimit));
    }

    private void ValidateAuthenticationConfiguration()
    {
        switch (AuthenticationMode)
        {
            case CosmosAuthenticationMode.ConnectionString:
                if (string.IsNullOrWhiteSpace(ConnectionString))
                    throw new ArgumentException("ConnectionString is required when using ConnectionString authentication mode.", nameof(ConnectionString));
                break;

            case CosmosAuthenticationMode.ManagedIdentity:
                if (string.IsNullOrWhiteSpace(AccountEndpoint))
                    throw new ArgumentException("AccountEndpoint is required when using ManagedIdentity authentication mode.", nameof(AccountEndpoint));
                break;

            case CosmosAuthenticationMode.ServicePrincipal:
                if (string.IsNullOrWhiteSpace(AccountEndpoint))
                    throw new ArgumentException("AccountEndpoint is required when using ServicePrincipal authentication mode.", nameof(AccountEndpoint));
                
                if (ServicePrincipal == null)
                    throw new ArgumentException("ServicePrincipal configuration is required when using ServicePrincipal authentication mode.", nameof(ServicePrincipal));
                
                ServicePrincipal.Validate();
                break;

            default:
                throw new ArgumentException($"Unknown authentication mode: {AuthenticationMode}", nameof(AuthenticationMode));
        }
    }

    private void ValidateDedicatedContainerNames()
    {
        if (string.IsNullOrWhiteSpace(ServersContainerName))
            throw new ArgumentException("ServersContainerName cannot be null or empty when using Dedicated strategy.", nameof(ServersContainerName));
        
        if (string.IsNullOrWhiteSpace(LocksContainerName))
            throw new ArgumentException("LocksContainerName cannot be null or empty when using Dedicated strategy.", nameof(LocksContainerName));
        
        if (string.IsNullOrWhiteSpace(QueuesContainerName))
            throw new ArgumentException("QueuesContainerName cannot be null or empty when using Dedicated strategy.", nameof(QueuesContainerName));
        
        if (string.IsNullOrWhiteSpace(SetsContainerName))
            throw new ArgumentException("SetsContainerName cannot be null or empty when using Dedicated strategy.", nameof(SetsContainerName));
        
        if (string.IsNullOrWhiteSpace(HashesContainerName))
            throw new ArgumentException("HashesContainerName cannot be null or empty when using Dedicated strategy.", nameof(HashesContainerName));
        
        if (string.IsNullOrWhiteSpace(ListsContainerName))
            throw new ArgumentException("ListsContainerName cannot be null or empty when using Dedicated strategy.", nameof(ListsContainerName));
        
        if (string.IsNullOrWhiteSpace(CountersContainerName))
            throw new ArgumentException("CountersContainerName cannot be null or empty when using Dedicated strategy.", nameof(CountersContainerName));
    }

    private void ValidateConsolidatedContainerNames()
    {
        if (string.IsNullOrWhiteSpace(MetadataContainerName))
            throw new ArgumentException("MetadataContainerName cannot be null or empty when using Consolidated strategy.", nameof(MetadataContainerName));
        
        if (string.IsNullOrWhiteSpace(CollectionsContainerName))
            throw new ArgumentException("CollectionsContainerName cannot be null or empty when using Consolidated strategy.", nameof(CollectionsContainerName));
    }
}

/// <summary>
/// Bulk execution options for batch operations.
/// </summary>
public class BulkExecutionOptions
{
    /// <summary>
    /// Gets or sets the maximum batch size for bulk operations.
    /// </summary>
    public int MaxBatchSize { get; set; } = 100;

    /// <summary>
    /// Gets or sets the maximum degree of parallelism for bulk operations.
    /// </summary>
    public int MaxDegreeOfParallelism { get; set; } = Environment.ProcessorCount;

    /// <summary>
    /// Gets or sets a value indicating whether to enable bulk execution.
    /// </summary>
    public bool EnableBulkExecution { get; set; } = true;
}

/// <summary>
/// TTL (Time To Live) settings for different document types.
/// </summary>
public class TtlSettings
{
    /// <summary>
    /// Gets or sets the TTL for job documents in seconds.
    /// </summary>
    public int? JobDocumentTtl { get; set; } = (int)TimeSpan.FromDays(30).TotalSeconds;

    /// <summary>
    /// Gets or sets the TTL for server documents in seconds.
    /// </summary>
    public int? ServerDocumentTtl { get; set; } = (int)TimeSpan.FromMinutes(10).TotalSeconds;

    /// <summary>
    /// Gets or sets the TTL for lock documents in seconds.
    /// </summary>
    public int? LockDocumentTtl { get; set; } = (int)TimeSpan.FromMinutes(5).TotalSeconds;

    /// <summary>
    /// Gets or sets the TTL for counter documents in seconds.
    /// </summary>
    public int? CounterDocumentTtl { get; set; } = (int)TimeSpan.FromDays(7).TotalSeconds;
}

/// <summary>
/// Performance tuning options.
/// </summary>
public class PerformanceOptions
{
    /// <summary>
    /// Gets or sets the query page size for large result sets.
    /// </summary>
    public int QueryPageSize { get; set; } = 100;

    /// <summary>
    /// Gets or sets a value indicating whether to enable query metrics.
    /// </summary>
    public bool EnableQueryMetrics { get; set; } = false;

    /// <summary>
    /// Gets or sets the connection mode for the Cosmos client.
    /// </summary>
    public ConnectionMode ConnectionMode { get; set; } = ConnectionMode.Direct;

    /// <summary>
    /// Gets or sets a value indicating whether to enable TCP connection endpoint rediscovery.
    /// </summary>
    public bool EnableTcpConnectionEndpointRediscovery { get; set; } = true;
}

/// <summary>
/// Service principal configuration for Azure AD authentication.
/// </summary>
public class ServicePrincipalOptions
{
    /// <summary>
    /// Gets or sets the Azure AD tenant ID.
    /// </summary>
    public string TenantId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the Azure AD application (client) ID.
    /// </summary>
    public string ClientId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the Azure AD application client secret.
    /// </summary>
    public string ClientSecret { get; set; } = string.Empty;

    /// <summary>
    /// Validates the service principal configuration.
    /// </summary>
    /// <exception cref="ArgumentException">Thrown when configuration is invalid.</exception>
    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(TenantId))
            throw new ArgumentException("TenantId cannot be null or empty.", nameof(TenantId));

        if (string.IsNullOrWhiteSpace(ClientId))
            throw new ArgumentException("ClientId cannot be null or empty.", nameof(ClientId));

        if (string.IsNullOrWhiteSpace(ClientSecret))
            throw new ArgumentException("ClientSecret cannot be null or empty.", nameof(ClientSecret));
    }
}