# Hangfire Azure Cosmos DB Storage Provider - Architecture Specification

## Table of Contents
1. [Executive Summary](#executive-summary)
2. [High-Level Architecture](#high-level-architecture)
3. [Data Model Design](#data-model-design)
4. [Core Classes Architecture](#core-classes-architecture)
5. [Performance Considerations](#performance-considerations)
6. [Reliability & Consistency](#reliability--consistency)
7. [Configuration & Extensibility](#configuration--extensibility)
8. [Implementation Patterns](#implementation-patterns)
9. [Deployment Guidance](#deployment-guidance)

## Executive Summary

This document outlines the architecture for a Hangfire storage provider that uses Azure Cosmos DB as the backend storage system. The design prioritizes a balanced approach focusing on good performance, reliability, reasonable costs, maintainability, and extensibility.

### Key Design Principles
- **Balanced Performance**: Optimize for typical Hangfire workloads while maintaining cost efficiency
- **Reliability**: Ensure job durability and consistency within Cosmos DB's eventual consistency model
- **Maintainability**: Clean, testable code with clear separation of concerns
- **Extensibility**: Pluggable components and configuration options
- **Cost Efficiency**: Minimize RU consumption through smart partitioning and query optimization

## High-Level Architecture

```
┌─────────────────────────────────────────────────────────────────┐
│                    Hangfire Application                         │
├─────────────────────────────────────────────────────────────────┤
│                     Hangfire Core                              │
│  ┌─────────────────┐  ┌─────────────────┐  ┌─────────────────┐ │
│  │   JobStorage    │  │ IStorageConnection│ │ IMonitoringApi  │ │
│  │   (Abstract)    │  │   (Interface)   │  │   (Interface)   │ │
│  └─────────────────┘  └─────────────────┘  └─────────────────┘ │
├─────────────────────────────────────────────────────────────────┤
│                HangfireCosmos.Storage                           │
│  ┌─────────────────┐  ┌─────────────────┐  ┌─────────────────┐ │
│  │  CosmosStorage  │  │CosmosConnection │  │CosmosMonitoring │ │
│  │                 │  │                 │  │      Api        │ │
│  └─────────────────┘  └─────────────────┘  └─────────────────┘ │
│  ┌─────────────────┐  ┌─────────────────┐  ┌─────────────────┐ │
│  │ CosmosDocument  │  │  CosmosQuery    │  │  CosmosLock     │ │
│  │   Repository    │  │    Builder      │  │   Manager       │ │
│  └─────────────────┘  └─────────────────┘  └─────────────────┘ │
├─────────────────────────────────────────────────────────────────┤
│                    Azure Cosmos DB                             │
│  ┌─────────────────────────────────────────────────────────────┐ │
│  │                    Database                                 │ │
│  │  ┌─────────────┐  ┌─────────────┐  ┌─────────────┐        │ │
│  │  │    Jobs     │  │   Servers   │  │   Locks     │        │ │
│  │  │ Container   │  │ Container   │  │ Container   │        │ │
│  │  └─────────────┘  └─────────────┘  └─────────────┘        │ │
│  │  ┌─────────────┐  ┌─────────────┐  ┌─────────────┐        │ │
│  │  │   Queues    │  │    Sets     │  │   Hashes    │        │ │
│  │  │ Container   │  │ Container   │  │ Container   │        │ │
│  │  └─────────────┘  └─────────────┘  └─────────────┘        │ │
│  │  ┌─────────────┐  ┌─────────────┐                         │ │
│  │  │    Lists    │  │  Counters   │                         │ │
│  │  │ Container   │  │ Container   │                         │ │
│  │  └─────────────┘  └─────────────┘                         │ │
│  └─────────────────────────────────────────────────────────────┘ │
└─────────────────────────────────────────────────────────────────┘
```

## Data Model Design

### Container Strategy

The architecture uses a **multi-container approach** to optimize performance and cost:

1. **Jobs Container** - Primary job data and state
2. **Servers Container** - Server heartbeats and metadata
3. **Locks Container** - Distributed locking mechanism
4. **Queues Container** - Queue metadata and statistics
5. **Sets Container** - Hangfire sets (scheduled jobs, etc.)
6. **Hashes Container** - Key-value hash storage
7. **Lists Container** - Ordered list storage
8. **Counters Container** - Atomic counters

### Document Schemas

#### Jobs Container
```json
{
  "id": "job:{jobId}",
  "partitionKey": "job:{queueName}",
  "documentType": "job",
  "jobId": "12345",
  "queueName": "default",
  "state": "enqueued",
  "stateData": {
    "enqueuedAt": "2024-01-01T00:00:00Z",
    "queue": "default"
  },
  "stateHistory": [
    {
      "state": "enqueued",
      "createdAt": "2024-01-01T00:00:00Z",
      "data": {}
    }
  ],
  "invocationData": {
    "type": "MyApp.Jobs.EmailJob",
    "method": "SendEmail",
    "arguments": ["user@example.com", "Welcome!"]
  },
  "parameters": {
    "retryCount": 0,
    "createdAt": "2024-01-01T00:00:00Z"
  },
  "expireAt": "2024-01-02T00:00:00Z",
  "_ts": 1704067200,
  "_etag": "\"abc123\""
}
```

#### Servers Container
```json
{
  "id": "server:{serverId}",
  "partitionKey": "servers",
  "documentType": "server",
  "serverId": "server-001",
  "data": {
    "workerCount": 20,
    "queues": ["default", "critical"],
    "startedAt": "2024-01-01T00:00:00Z"
  },
  "lastHeartbeat": "2024-01-01T00:05:00Z",
  "expireAt": "2024-01-01T00:10:00Z",
  "_ts": 1704067500
}
```

#### Locks Container
```json
{
  "id": "lock:{resource}",
  "partitionKey": "locks",
  "documentType": "lock",
  "resource": "recurring-job:my-job",
  "owner": "server-001",
  "acquiredAt": "2024-01-01T00:00:00Z",
  "expireAt": "2024-01-01T00:01:00Z",
  "_ts": 1704067200
}
```

#### Queues Container
```json
{
  "id": "queue:{queueName}",
  "partitionKey": "queues",
  "documentType": "queue",
  "queueName": "default",
  "length": 150,
  "fetched": 10,
  "lastUpdated": "2024-01-01T00:00:00Z",
  "_ts": 1704067200
}
```

#### Sets Container
```json
{
  "id": "set:{key}:{value}",
  "partitionKey": "set:{key}",
  "documentType": "set",
  "key": "recurring-jobs",
  "value": "my-recurring-job",
  "score": 1704067200.0,
  "_ts": 1704067200
}
```

#### Hashes Container
```json
{
  "id": "hash:{key}:{field}",
  "partitionKey": "hash:{key}",
  "documentType": "hash",
  "key": "job:12345:state",
  "field": "state",
  "value": "succeeded",
  "_ts": 1704067200
}
```

#### Lists Container
```json
{
  "id": "list:{key}:{index}",
  "partitionKey": "list:{key}",
  "documentType": "list",
  "key": "failed-jobs",
  "index": 0,
  "value": "job:12345",
  "_ts": 1704067200
}
```

#### Counters Container
```json
{
  "id": "counter:{key}",
  "partitionKey": "counters",
  "documentType": "counter",
  "key": "stats:succeeded",
  "value": 1000,
  "expireAt": "2024-01-02T00:00:00Z",
  "_ts": 1704067200
}
```

### Partition Key Strategy

**Jobs Container**: `job:{queueName}`
- Distributes jobs across partitions by queue
- Enables efficient queue-specific queries
- Balances load for multi-queue scenarios

**Servers Container**: `servers`
- Single partition for server management
- Low volume, acceptable for single partition

**Locks Container**: `locks`
- Single partition for distributed locking
- Ensures strong consistency for lock operations

**Queues Container**: `queues`
- Single partition for queue metadata
- Low volume, enables cross-queue statistics

**Sets Container**: `set:{key}`
- Partitioned by set key
- Enables efficient set operations

**Hashes Container**: `hash:{key}`
- Partitioned by hash key
- Enables efficient hash operations

**Lists Container**: `list:{key}`
- Partitioned by list key
- Enables efficient list operations

**Counters Container**: `counters`
- Single partition for counter operations
- Enables atomic counter operations

## Core Classes Architecture

### CosmosStorage
**Responsibility**: Main entry point, implements [`JobStorage`](HangfireCosmos.Storage/CosmosStorage.cs:11)

```csharp
public class CosmosStorage : JobStorage
{
    private readonly CosmosClient _cosmosClient;
    private readonly CosmosStorageOptions _options;
    private readonly ICosmosDocumentRepository _documentRepository;
    
    public override IStorageConnection GetConnection();
    public override IMonitoringApi GetMonitoringApi();
}
```

### CosmosStorageConnection
**Responsibility**: Implements [`IStorageConnection`](HangfireCosmos.Storage/CosmosStorage.cs:34), handles job operations

```csharp
public class CosmosStorageConnection : IStorageConnection
{
    // Job Management
    string CreateExpiredJob(Job job, IDictionary<string, string> parameters, TimeSpan expireIn);
    IWriteOnlyTransaction CreateWriteTransaction();
    
    // Fetching
    IFetchedJob FetchNextJob(string[] queues, CancellationToken cancellationToken);
    
    // Querying
    string GetFirstByLowestScoreFromSet(string key, double fromScore, double toScore);
    JobData GetJobData(string jobId);
    StateData GetStateData(string jobId);
    
    // Sets, Lists, Hashes
    List<string> GetRangeFromSet(string key, int startingFrom, int endingAt);
    List<string> GetRangeFromList(string key, int startingFrom, int endingAt);
    Dictionary<string, string> GetAllEntriesFromHash(string key);
    
    // Counters
    long GetCounter(string key);
    long GetHashCount(string key);
    long GetListCount(string key);
    long GetSetCount(string key);
}
```

### CosmosMonitoringApi
**Responsibility**: Implements [`IMonitoringApi`](HangfireCosmos.Storage/CosmosStorage.cs:44), provides dashboard data

```csharp
public class CosmosMonitoringApi : IMonitoringApi
{
    // Statistics
    StatisticsDto GetStatistics();
    
    // Jobs
    JobList<EnqueuedJobDto> EnqueuedJobs(string queue, int from, int perPage);
    JobList<FetchedJobDto> FetchedJobs(string queue, int from, int perPage);
    JobList<ProcessingJobDto> ProcessingJobs(int from, int perPage);
    JobList<ScheduledJobDto> ScheduledJobs(int from, int perPage);
    JobList<SucceededJobDto> SucceededJobs(int from, int perPage);
    JobList<FailedJobDto> FailedJobs(int from, int perPage);
    JobList<DeletedJobDto> DeletedJobs(int from, int perPage);
    
    // Servers
    IList<ServerDto> GetServers();
    
    // Queues
    IList<QueueWithTopEnqueuedJobsDto> Queues();
}
```

### CosmosDocumentRepository
**Responsibility**: Abstraction layer for Cosmos DB operations

```csharp
public interface ICosmosDocumentRepository
{
    Task<T> GetDocumentAsync<T>(string id, string partitionKey);
    Task<T> CreateDocumentAsync<T>(T document, string partitionKey);
    Task<T> UpsertDocumentAsync<T>(T document, string partitionKey);
    Task DeleteDocumentAsync(string id, string partitionKey);
    Task<IEnumerable<T>> QueryDocumentsAsync<T>(string query, object parameters);
    Task<IEnumerable<T>> QueryDocumentsAsync<T>(QueryDefinition queryDefinition);
}

public class CosmosDocumentRepository : ICosmosDocumentRepository
{
    private readonly CosmosClient _cosmosClient;
    private readonly CosmosStorageOptions _options;
    private readonly Dictionary<string, Container> _containers;
}
```

### CosmosWriteOnlyTransaction
**Responsibility**: Implements [`IWriteOnlyTransaction`], batches operations

```csharp
public class CosmosWriteOnlyTransaction : IWriteOnlyTransaction
{
    private readonly List<ITransactionOperation> _operations;
    
    public void ExpireJob(string jobId, TimeSpan expireIn);
    public void PersistJob(string jobId);
    public void SetJobState(string jobId, IState state);
    public void AddJobState(string jobId, IState state);
    public void AddToQueue(string queue, string jobId);
    public void IncrementCounter(string key);
    public void IncrementCounter(string key, TimeSpan expireIn);
    public void DecrementCounter(string key);
    public void DecrementCounter(string key, TimeSpan expireIn);
    public void AddToSet(string key, string value);
    public void AddToSet(string key, string value, double score);
    public void RemoveFromSet(string key, string value);
    public void InsertToList(string key, string value);
    public void RemoveFromList(string key, string value);
    public void TrimList(string key, int keepStartingFrom, int keepEndingAt);
    public void SetRangeInHash(string key, IEnumerable<KeyValuePair<string, string>> keyValuePairs);
    public void RemoveHash(string key);
    
    public void Commit();
}
```

### CosmosLockManager
**Responsibility**: Distributed locking mechanism

```csharp
public interface ICosmosLockManager
{
    Task<IDisposable> AcquireLockAsync(string resource, TimeSpan timeout);
    Task ReleaseLockAsync(string resource, string owner);
}

public class CosmosLockManager : ICosmosLockManager
{
    private readonly ICosmosDocumentRepository _repository;
    private readonly string _serverId;
}
```

### CosmosQueryBuilder
**Responsibility**: Builds optimized Cosmos DB queries

```csharp
public class CosmosQueryBuilder
{
    public QueryDefinition BuildJobsByStateQuery(string state, int skip, int take);
    public QueryDefinition BuildJobsByQueueQuery(string queue, int skip, int take);
    public QueryDefinition BuildExpiredJobsQuery(DateTime expiredBefore);
    public QueryDefinition BuildServerHeartbeatQuery();
    public QueryDefinition BuildQueueStatsQuery();
}
```

## Performance Considerations

### Query Optimization

1. **Partition Key Usage**
   - All queries include partition key when possible
   - Cross-partition queries minimized and cached
   - Use continuation tokens for large result sets

2. **Indexing Strategy**
   ```json
   {
     "indexingPolicy": {
       "indexingMode": "consistent",
       "automatic": true,
       "includedPaths": [
         { "path": "/documentType/?" },
         { "path": "/state/?" },
         { "path": "/queueName/?" },
         { "path": "/expireAt/?" },
         { "path": "/lastHeartbeat/?" }
       ],
       "excludedPaths": [
         { "path": "/invocationData/*" },
         { "path": "/stateHistory/*" }
       ]
     }
   }
   ```

3. **Request Unit (RU) Optimization**
   - Batch operations where possible
   - Use stored procedures for complex operations
   - Implement exponential backoff for throttling
   - Monitor and alert on RU consumption

### Caching Strategy

1. **In-Memory Caching**
   - Cache frequently accessed job data
   - Cache server information
   - Cache queue statistics
   - Use sliding expiration (5-15 minutes)

2. **Connection Pooling**
   - Reuse CosmosClient instances
   - Configure connection pool settings
   - Monitor connection health

### Batch Operations

1. **Transaction Batching**
   - Group related operations in transactions
   - Use Cosmos DB transactional batch API
   - Limit batch size to avoid timeouts

2. **Bulk Operations**
   - Use bulk executor for large data operations
   - Implement parallel processing for independent operations

## Reliability & Consistency

### Consistency Model

1. **Session Consistency**
   - Default consistency level for most operations
   - Provides read-your-writes consistency
   - Balances performance and consistency

2. **Strong Consistency for Critical Operations**
   - Distributed locking operations
   - Counter increments/decrements
   - Server heartbeat updates

### Error Handling

1. **Retry Policies**
   ```csharp
   public class CosmosRetryPolicy
   {
       private static readonly TimeSpan[] RetryDelays = {
           TimeSpan.FromMilliseconds(100),
           TimeSpan.FromMilliseconds(500),
           TimeSpan.FromSeconds(1),
           TimeSpan.FromSeconds(2),
           TimeSpan.FromSeconds(5)
       };
   }
   ```

2. **Circuit Breaker Pattern**
   - Implement circuit breaker for Cosmos DB calls
   - Fail fast when service is unavailable
   - Automatic recovery detection

3. **Graceful Degradation**
   - Continue processing when monitoring API fails
   - Queue jobs locally when Cosmos DB is unavailable
   - Implement health checks

### Data Durability

1. **TTL Management**
   - Automatic cleanup of expired jobs
   - Configurable retention periods
   - Separate TTL for different document types

2. **Backup Strategy**
   - Leverage Cosmos DB automatic backups
   - Implement point-in-time recovery procedures
   - Document recovery processes

### Distributed Locking

1. **Lock Implementation**
   ```csharp
   public class CosmosDistributedLock : IDisposable
   {
       private readonly string _resource;
       private readonly string _owner;
       private readonly ICosmosLockManager _lockManager;
       private readonly Timer _renewalTimer;
   }
   ```

2. **Lock Renewal**
   - Automatic lock renewal for long-running operations
   - Configurable renewal intervals
   - Graceful handling of renewal failures

## Configuration & Extensibility

### Configuration Options

```csharp
public class CosmosStorageOptions
{
    public string DatabaseName { get; set; } = "hangfire";
    public string JobsContainerName { get; set; } = "jobs";
    public string ServersContainerName { get; set; } = "servers";
    public string LocksContainerName { get; set; } = "locks";
    public string QueuesContainerName { get; set; } = "queues";
    public string SetsContainerName { get; set; } = "sets";
    public string HashesContainerName { get; set; } = "hashes";
    public string ListsContainerName { get; set; } = "lists";
    public string CountersContainerName { get; set; } = "counters";
    
    public int DefaultThroughput { get; set; } = 400;
    public ConsistencyLevel ConsistencyLevel { get; set; } = ConsistencyLevel.Session;
    public TimeSpan DefaultJobExpiration { get; set; } = TimeSpan.FromDays(7);
    public TimeSpan ServerTimeout { get; set; } = TimeSpan.FromMinutes(5);
    public TimeSpan LockTimeout { get; set; } = TimeSpan.FromMinutes(1);
    
    public bool EnableCaching { get; set; } = true;
    public TimeSpan CacheExpiration { get; set; } = TimeSpan.FromMinutes(5);
    
    public int MaxRetryAttempts { get; set; } = 5;
    public TimeSpan RetryDelay { get; set; } = TimeSpan.FromMilliseconds(100);
    
    public bool AutoCreateDatabase { get; set; } = true;
    public bool AutoCreateContainers { get; set; } = true;
}
```

### Extension Points

1. **Custom Serialization**
   ```csharp
   public interface ICosmosSerializer
   {
       string Serialize<T>(T obj);
       T Deserialize<T>(string json);
   }
   ```

2. **Custom Retry Policies**
   ```csharp
   public interface ICosmosRetryPolicy
   {
       Task<T> ExecuteAsync<T>(Func<Task<T>> operation);
   }
   ```

3. **Custom Lock Providers**
   ```csharp
   public interface ICosmosLockProvider
   {
       Task<IDisposable> AcquireLockAsync(string resource, TimeSpan timeout);
   }
   ```

### Dependency Injection Setup

```csharp
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddHangfireCosmosStorage(
        this IServiceCollection services,
        string connectionString,
        Action<CosmosStorageOptions> configureOptions = null)
    {
        var options = new CosmosStorageOptions();
        configureOptions?.Invoke(options);
        
        services.AddSingleton(options);
        services.AddSingleton<CosmosClient>(sp => new CosmosClient(connectionString));
        services.AddSingleton<ICosmosDocumentRepository, CosmosDocumentRepository>();
        services.AddSingleton<ICosmosLockManager, CosmosLockManager>();
        services.AddSingleton<CosmosQueryBuilder>();
        services.AddSingleton<CosmosStorage>();
        
        return services;
    }
}
```

## Implementation Patterns

### Repository Pattern

```csharp
public abstract class CosmosRepositoryBase<T> where T : class
{
    protected readonly Container _container;
    protected readonly CosmosStorageOptions _options;
    
    protected virtual async Task<T> GetAsync(string id, string partitionKey)
    {
        try
        {
            var response = await _container.ReadItemAsync<T>(id, new PartitionKey(partitionKey));
            return response.Resource;
        }
        catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }
    }
}
```

### Unit of Work Pattern

```csharp
public class CosmosUnitOfWork : IDisposable
{
    private readonly List<Func<Task>> _operations = new();
    private readonly CosmosClient _cosmosClient;
    
    public void AddOperation(Func<Task> operation)
    {
        _operations.Add(operation);
    }
    
    public async Task CommitAsync()
    {
        // Execute all operations in a transaction batch where possible
        foreach (var operation in _operations)
        {
            await operation();
        }
    }
}
```

### Factory Pattern

```csharp
public interface ICosmosContainerFactory
{
    Container GetContainer(string containerName);
}

public class CosmosContainerFactory : ICosmosContainerFactory
{
    private readonly CosmosClient _cosmosClient;
    private readonly CosmosStorageOptions _options;
    private readonly ConcurrentDictionary<string, Container> _containers = new();
    
    public Container GetContainer(string containerName)
    {
        return _containers.GetOrAdd(containerName, name =>
            _cosmosClient.GetContainer(_options.DatabaseName, name));
    }
}
```

## Deployment Guidance

### Database Setup

1. **Database Creation**
   ```csharp
   public async Task InitializeDatabaseAsync()
   {
       if (_options.AutoCreateDatabase)
       {
           await _cosmosClient.CreateDatabaseIfNotExistsAsync(
               _options.DatabaseName,
               _options.DefaultThroughput);
       }
   }
   ```

2. **Container Creation**
   ```csharp
   private readonly Dictionary<string, ContainerProperties> _containerDefinitions = new()
   {
       ["jobs"] = new ContainerProperties("jobs", "/partitionKey")
       {
           DefaultTimeToLive = (int)TimeSpan.FromDays(30).TotalSeconds
       },
       ["servers"] = new ContainerProperties("servers", "/partitionKey")
       {
           DefaultTimeToLive = (int)TimeSpan.FromMinutes(10).TotalSeconds
       }
       // ... other containers
   };
   ```

### Performance Tuning

1. **Throughput Configuration**
   - Start with 400 RU/s for development
   - Monitor RU consumption in production
   - Use autoscale for variable workloads
   - Consider dedicated throughput for high-volume containers

2. **Monitoring Setup**
   ```csharp
   public class CosmosMetrics
   {
       private readonly IMetricsLogger _metricsLogger;
       
       public void RecordRequestUnits(string operation, double requestCharge)
       {
           _metricsLogger.Histogram("cosmos.request_units")
               .WithTag("operation", operation)
               .Record(requestCharge);
       }
   }
   ```

### Security Considerations

1. **Connection Security**
   - Use managed identity when possible
   - Store connection strings in Key Vault
   - Implement connection string rotation

2. **Access Control**
   - Use resource tokens for fine-grained access
   - Implement role-based access control
   - Audit access patterns

### Scaling Considerations

1. **Horizontal Scaling**
   - Design for multiple Hangfire servers
   - Implement proper distributed locking
   - Handle server failover scenarios

2. **Vertical Scaling**
   - Monitor partition key distribution
   - Implement partition key rotation if needed
   - Use synthetic partition keys for hot partitions

---

## Conclusion

This architecture provides a solid foundation for implementing a Hangfire storage provider using Azure Cosmos DB. The design balances performance, reliability, and cost while maintaining extensibility and testability. The multi-container approach optimizes for Cosmos DB's strengths while the abstraction layers ensure maintainable and testable code.

Key benefits of this architecture:
- **Scalable**: Handles large job volumes through proper partitioning
- **Reliable**: Implements proper error handling and consistency guarantees
- **Cost-Effective**: Optimizes RU consumption through smart query design
- **Maintainable**: Clean separation of concerns and testable components
- **Extensible**: Pluggable components and comprehensive configuration options

The next phase should focus on implementing the core classes, starting with the [`CosmosStorage`](HangfireCosmos.Storage/CosmosStorage.cs:11) class and its dependencies, followed by comprehensive testing and performance validation.