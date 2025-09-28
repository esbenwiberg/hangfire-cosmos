using Microsoft.Azure.Cosmos;

namespace HangfireCosmos.Storage;

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
    /// Gets or sets the default throughput (RU/s) for containers.
    /// </summary>
    public int DefaultThroughput { get; set; } = 400;

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
    /// Validates the configuration options.
    /// </summary>
    /// <exception cref="ArgumentException">Thrown when configuration is invalid.</exception>
    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(DatabaseName))
            throw new ArgumentException("DatabaseName cannot be null or empty.", nameof(DatabaseName));

        if (string.IsNullOrWhiteSpace(JobsContainerName))
            throw new ArgumentException("JobsContainerName cannot be null or empty.", nameof(JobsContainerName));

        if (DefaultThroughput < 400)
            throw new ArgumentException("DefaultThroughput must be at least 400 RU/s.", nameof(DefaultThroughput));

        if (MaxRetryAttempts < 0)
            throw new ArgumentException("MaxRetryAttempts cannot be negative.", nameof(MaxRetryAttempts));

        if (RetryDelay < TimeSpan.Zero)
            throw new ArgumentException("RetryDelay cannot be negative.", nameof(RetryDelay));

        if (RequestTimeout <= TimeSpan.Zero)
            throw new ArgumentException("RequestTimeout must be positive.", nameof(RequestTimeout));

        if (MaxConnectionLimit <= 0)
            throw new ArgumentException("MaxConnectionLimit must be positive.", nameof(MaxConnectionLimit));
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