using HangfireCosmos.Storage.Documents;

namespace HangfireCosmos.Storage;

/// <summary>
/// Resolves container names and partition keys based on the configured collection strategy.
/// </summary>
public class ContainerResolver
{
    private readonly CosmosStorageOptions _options;

    /// <summary>
    /// Initializes a new instance of the ContainerResolver class.
    /// </summary>
    /// <param name="options">The Cosmos storage options.</param>
    public ContainerResolver(CosmosStorageOptions options)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
    }

    /// <summary>
    /// Gets the container name for the specified document type.
    /// </summary>
    /// <param name="documentType">The document type.</param>
    /// <returns>The container name.</returns>
    public string GetContainerName(Type documentType)
    {
        return _options.CollectionStrategy switch
        {
            CollectionStrategy.Dedicated => GetDedicatedContainer(documentType),
            CollectionStrategy.Consolidated => GetConsolidatedContainer(documentType),
            _ => throw new ArgumentException($"Unknown collection strategy: {_options.CollectionStrategy}")
        };
    }

    /// <summary>
    /// Gets the container name for the specified document.
    /// </summary>
    /// <param name="document">The document instance.</param>
    /// <returns>The container name.</returns>
    public string GetContainerName(BaseDocument document)
    {
        return GetContainerName(document.GetType());
    }

    /// <summary>
    /// Gets the partition key for the specified document.
    /// </summary>
    /// <param name="document">The document instance.</param>
    /// <returns>The partition key.</returns>
    public string GetPartitionKey(BaseDocument document)
    {
        return _options.CollectionStrategy switch
        {
            CollectionStrategy.Dedicated => GetDedicatedPartitionKey(document),
            CollectionStrategy.Consolidated => GetConsolidatedPartitionKey(document),
            _ => throw new ArgumentException($"Unknown collection strategy: {_options.CollectionStrategy}")
        };
    }

    /// <summary>
    /// Gets all container names that will be used based on the current strategy.
    /// </summary>
    /// <returns>A collection of container names.</returns>
    public IEnumerable<string> GetAllContainerNames()
    {
        return _options.CollectionStrategy switch
        {
            CollectionStrategy.Dedicated => GetDedicatedContainerNames(),
            CollectionStrategy.Consolidated => GetConsolidatedContainerNames(),
            _ => throw new ArgumentException($"Unknown collection strategy: {_options.CollectionStrategy}")
        };
    }

    private string GetDedicatedContainer(Type documentType)
    {
        return documentType.Name switch
        {
            nameof(JobDocument) => _options.JobsContainerName,
            nameof(ServerDocument) => _options.ServersContainerName,
            nameof(LockDocument) => _options.LocksContainerName,
            nameof(QueueDocument) => _options.QueuesContainerName,
            nameof(SetDocument) => _options.SetsContainerName,
            nameof(HashDocument) => _options.HashesContainerName,
            nameof(ListDocument) => _options.ListsContainerName,
            nameof(CounterDocument) => _options.CountersContainerName,
            _ => throw new ArgumentException($"Unknown document type: {documentType.Name}")
        };
    }

    private string GetConsolidatedContainer(Type documentType)
    {
        return documentType.Name switch
        {
            nameof(JobDocument) => _options.JobsContainerName, // Jobs stay separate for performance
            nameof(ServerDocument) or nameof(LockDocument) or 
            nameof(QueueDocument) or nameof(CounterDocument) => _options.MetadataContainerName,
            nameof(SetDocument) or nameof(HashDocument) or 
            nameof(ListDocument) => _options.CollectionsContainerName,
            _ => throw new ArgumentException($"Unknown document type: {documentType.Name}")
        };
    }

    private string GetDedicatedPartitionKey(BaseDocument document)
    {
        return document switch
        {
            JobDocument job => $"job:{job.QueueName}",
            ServerDocument => "servers",
            LockDocument => "locks",
            QueueDocument => "queues",
            CounterDocument => "counters",
            SetDocument set => $"set:{set.Key}",
            HashDocument hash => $"hash:{hash.Key}",
            ListDocument list => $"list:{list.Key}",
            _ => document.DocumentType
        };
    }

    private string GetConsolidatedPartitionKey(BaseDocument document)
    {
        return document switch
        {
            // Jobs container - same partitioning as dedicated
            JobDocument job => $"job:{job.QueueName}",
            
            // Metadata container - partition by document type for even distribution
            ServerDocument => "servers",
            LockDocument => "locks",
            QueueDocument => "queues",
            CounterDocument => "counters",
            
            // Collections container - partition by key for related data locality
            SetDocument set => $"set:{set.Key}",
            HashDocument hash => $"hash:{hash.Key}",
            ListDocument list => $"list:{list.Key}",
            
            _ => document.DocumentType
        };
    }

    private IEnumerable<string> GetDedicatedContainerNames()
    {
        return new[]
        {
            _options.JobsContainerName,
            _options.ServersContainerName,
            _options.LocksContainerName,
            _options.QueuesContainerName,
            _options.SetsContainerName,
            _options.HashesContainerName,
            _options.ListsContainerName,
            _options.CountersContainerName
        };
    }

    private IEnumerable<string> GetConsolidatedContainerNames()
    {
        return new[]
        {
            _options.JobsContainerName,
            _options.MetadataContainerName,
            _options.CollectionsContainerName
        };
    }
}