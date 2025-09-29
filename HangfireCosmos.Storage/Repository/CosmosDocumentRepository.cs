using Microsoft.Azure.Cosmos;
using HangfireCosmos.Storage.Documents;
using System.Collections.Concurrent;
using System.Net;

namespace HangfireCosmos.Storage.Repository;

/// <summary>
/// Implementation of Cosmos DB document repository operations.
/// </summary>
public class CosmosDocumentRepository : ICosmosDocumentRepository
{
    private readonly CosmosClient _cosmosClient;
    private readonly CosmosStorageOptions _options;
    private readonly ConcurrentDictionary<string, Container> _containers;
    private Database? _database;

    /// <summary>
    /// Initializes a new instance of the CosmosDocumentRepository class.
    /// </summary>
    /// <param name="cosmosClient">The Cosmos client.</param>
    /// <param name="options">The storage options.</param>
    public CosmosDocumentRepository(
        CosmosClient cosmosClient,
        CosmosStorageOptions options)
    {
        _cosmosClient = cosmosClient ?? throw new ArgumentNullException(nameof(cosmosClient));
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _containers = new ConcurrentDictionary<string, Container>();
    }

    /// <inheritdoc />
    public async Task<T?> GetDocumentAsync<T>(string containerName, string id, string partitionKey, CancellationToken cancellationToken = default) where T : BaseDocument
    {
        try
        {
            var container = GetContainer(containerName);
            var response = await container.ReadItemAsync<T>(id, new PartitionKey(partitionKey), cancellationToken: cancellationToken);
            return response.Resource;
        }
        catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }
    }

    /// <inheritdoc />
    public async Task<T> CreateDocumentAsync<T>(string containerName, T document, CancellationToken cancellationToken = default) where T : BaseDocument
    {
        var container = GetContainer(containerName);
        var response = await container.CreateItemAsync(document, new PartitionKey(document.PartitionKey), cancellationToken: cancellationToken);
        return response.Resource;
    }

    /// <inheritdoc />
    public async Task<T> UpsertDocumentAsync<T>(string containerName, T document, CancellationToken cancellationToken = default) where T : BaseDocument
    {
        var container = GetContainer(containerName);
        var response = await container.UpsertItemAsync(document, new PartitionKey(document.PartitionKey), cancellationToken: cancellationToken);
        return response.Resource;
    }

    /// <inheritdoc />
    public async Task<T> UpdateDocumentAsync<T>(string containerName, T document, CancellationToken cancellationToken = default) where T : BaseDocument
    {
        var container = GetContainer(containerName);
        var response = await container.ReplaceItemAsync(document, document.Id, new PartitionKey(document.PartitionKey), cancellationToken: cancellationToken);
        return response.Resource;
    }

    /// <inheritdoc />
    public async Task DeleteDocumentAsync(string containerName, string id, string partitionKey, CancellationToken cancellationToken = default)
    {
        try
        {
            var container = GetContainer(containerName);
            await container.DeleteItemAsync<BaseDocument>(id, new PartitionKey(partitionKey), cancellationToken: cancellationToken);
        }
        catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            // Don't throw for not found during deletion
        }
    }

    /// <inheritdoc />
    public async Task<IEnumerable<T>> QueryDocumentsAsync<T>(string containerName, string query, object? parameters = null, string? partitionKey = null, CancellationToken cancellationToken = default) where T : BaseDocument
    {
        var queryDefinition = new QueryDefinition(query);
        
        if (parameters != null)
        {
            var properties = parameters.GetType().GetProperties();
            foreach (var property in properties)
            {
                queryDefinition = queryDefinition.WithParameter($"@{property.Name}", property.GetValue(parameters));
            }
        }

        return await QueryDocumentsAsync<T>(containerName, queryDefinition, partitionKey, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<T>> QueryDocumentsAsync<T>(string containerName, QueryDefinition queryDefinition, string? partitionKey = null, CancellationToken cancellationToken = default) where T : BaseDocument
    {
        var container = GetContainer(containerName);
        var queryRequestOptions = new QueryRequestOptions
        {
            MaxItemCount = _options.Performance.QueryPageSize
        };

        if (!string.IsNullOrEmpty(partitionKey))
        {
            queryRequestOptions.PartitionKey = new PartitionKey(partitionKey);
        }

        var results = new List<T>();

        using var feedIterator = container.GetItemQueryIterator<T>(queryDefinition, requestOptions: queryRequestOptions);
        
        while (feedIterator.HasMoreResults)
        {
            var response = await feedIterator.ReadNextAsync(cancellationToken);
            results.AddRange(response);
        }

        return results;
    }

    /// <inheritdoc />
    public async Task<PagedResult<T>> QueryDocumentsPagedAsync<T>(string containerName, QueryDefinition queryDefinition, string? continuationToken = null, int? maxItemCount = null, string? partitionKey = null, CancellationToken cancellationToken = default) where T : BaseDocument
    {
        var container = GetContainer(containerName);
        var queryRequestOptions = new QueryRequestOptions
        {
            MaxItemCount = maxItemCount ?? _options.Performance.QueryPageSize
        };

        if (!string.IsNullOrEmpty(partitionKey))
        {
            queryRequestOptions.PartitionKey = new PartitionKey(partitionKey);
        }

        using var feedIterator = container.GetItemQueryIterator<T>(queryDefinition, continuationToken, queryRequestOptions);
        
        if (feedIterator.HasMoreResults)
        {
            var response = await feedIterator.ReadNextAsync(cancellationToken);
            
            return new PagedResult<T>
            {
                Items = response,
                ContinuationToken = response.ContinuationToken,
                RequestCharge = response.RequestCharge
            };
        }

        return new PagedResult<T>
        {
            Items = Enumerable.Empty<T>(),
            ContinuationToken = null,
            RequestCharge = 0
        };
    }

    // Note: Batch operations will be implemented later when proper SDK support is available

    /// <inheritdoc />
    public Container GetContainer(string containerName)
    {
        return _containers.GetOrAdd(containerName, name =>
        {
            EnsureDatabaseExists();
            return _database!.GetContainer(name);
        });
    }

    /// <inheritdoc />
    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        await EnsureDatabaseExistsAsync(cancellationToken);
        await EnsureContainersExistAsync(cancellationToken);
    }

    private void EnsureDatabaseExists()
    {
        if (_database == null)
        {
            _database = _cosmosClient.GetDatabase(_options.DatabaseName);
        }
    }

    private async Task EnsureDatabaseExistsAsync(CancellationToken cancellationToken = default)
    {
        if (_options.AutoCreateDatabase)
        {
            if (_options.UseSharedThroughput)
            {
                // When using shared throughput, provision throughput at database level
                await _cosmosClient.CreateDatabaseIfNotExistsAsync(
                    _options.DatabaseName,
                    _options.DefaultThroughput,
                    cancellationToken: cancellationToken);
            }
            else
            {
                // When using dedicated throughput, create database without throughput
                // (throughput will be provisioned at container level)
                await _cosmosClient.CreateDatabaseIfNotExistsAsync(
                    _options.DatabaseName,
                    cancellationToken: cancellationToken);
            }
        }

        _database = _cosmosClient.GetDatabase(_options.DatabaseName);
    }

    private async Task EnsureContainersExistAsync(CancellationToken cancellationToken = default)
    {
        if (!_options.AutoCreateContainers || _database == null)
            return;

        var containerDefinitions = GetContainerDefinitions();

        foreach (var (containerName, containerProperties) in containerDefinitions)
        {
            if (_options.UseSharedThroughput)
            {
                // When using shared throughput, create containers without dedicated throughput
                // They will share the database-level throughput
                await _database.CreateContainerIfNotExistsAsync(
                    containerProperties,
                    cancellationToken: cancellationToken);
            }
            else
            {
                // When using dedicated throughput, provision throughput per container
                await _database.CreateContainerIfNotExistsAsync(
                    containerProperties,
                    _options.DefaultThroughput,
                    cancellationToken: cancellationToken);
            }
        }
    }

    private Dictionary<string, ContainerProperties> GetContainerDefinitions()
    {
        var ttl = _options.TtlSettings;
        
        return _options.CollectionStrategy switch
        {
            CollectionStrategy.Dedicated => GetDedicatedContainerDefinitions(ttl),
            CollectionStrategy.Consolidated => GetConsolidatedContainerDefinitions(ttl),
            _ => throw new ArgumentException($"Unknown collection strategy: {_options.CollectionStrategy}")
        };
    }

    private Dictionary<string, ContainerProperties> GetDedicatedContainerDefinitions(TtlSettings ttl)
    {
        return new Dictionary<string, ContainerProperties>
        {
            [_options.JobsContainerName] = new ContainerProperties(_options.JobsContainerName, "/partitionKey")
            {
                DefaultTimeToLive = ttl.JobDocumentTtl,
                IndexingPolicy = GetJobsIndexingPolicy()
            },
            [_options.ServersContainerName] = new ContainerProperties(_options.ServersContainerName, "/partitionKey")
            {
                DefaultTimeToLive = ttl.ServerDocumentTtl,
                IndexingPolicy = GetServersIndexingPolicy()
            },
            [_options.LocksContainerName] = new ContainerProperties(_options.LocksContainerName, "/partitionKey")
            {
                DefaultTimeToLive = ttl.LockDocumentTtl,
                IndexingPolicy = GetLocksIndexingPolicy()
            },
            [_options.QueuesContainerName] = new ContainerProperties(_options.QueuesContainerName, "/partitionKey")
            {
                IndexingPolicy = GetQueuesIndexingPolicy()
            },
            [_options.SetsContainerName] = new ContainerProperties(_options.SetsContainerName, "/partitionKey")
            {
                IndexingPolicy = GetSetsIndexingPolicy()
            },
            [_options.HashesContainerName] = new ContainerProperties(_options.HashesContainerName, "/partitionKey")
            {
                IndexingPolicy = GetHashesIndexingPolicy()
            },
            [_options.ListsContainerName] = new ContainerProperties(_options.ListsContainerName, "/partitionKey")
            {
                IndexingPolicy = GetListsIndexingPolicy()
            },
            [_options.CountersContainerName] = new ContainerProperties(_options.CountersContainerName, "/partitionKey")
            {
                DefaultTimeToLive = ttl.CounterDocumentTtl,
                IndexingPolicy = GetCountersIndexingPolicy()
            }
        };
    }

    private Dictionary<string, ContainerProperties> GetConsolidatedContainerDefinitions(TtlSettings ttl)
    {
        return new Dictionary<string, ContainerProperties>
        {
            [_options.JobsContainerName] = new ContainerProperties(_options.JobsContainerName, "/partitionKey")
            {
                DefaultTimeToLive = ttl.JobDocumentTtl,
                IndexingPolicy = GetJobsIndexingPolicy()
            },
            [_options.MetadataContainerName] = new ContainerProperties(_options.MetadataContainerName, "/partitionKey")
            {
                DefaultTimeToLive = ttl.ServerDocumentTtl, // Use shortest TTL for mixed content
                IndexingPolicy = GetMetadataIndexingPolicy()
            },
            [_options.CollectionsContainerName] = new ContainerProperties(_options.CollectionsContainerName, "/partitionKey")
            {
                IndexingPolicy = GetCollectionsIndexingPolicy()
            }
        };
    }

    private IndexingPolicy GetJobsIndexingPolicy()
    {
        return new IndexingPolicy
        {
            IndexingMode = IndexingMode.Consistent,
            Automatic = true,
            IncludedPaths =
            {
                new IncludedPath { Path = "/*" },
                new IncludedPath { Path = "/documentType/?" },
                new IncludedPath { Path = "/state/?" },
                new IncludedPath { Path = "/queueName/?" },
                new IncludedPath { Path = "/expireAt/?" },
                new IncludedPath { Path = "/createdAt/?" },
                new IncludedPath { Path = "/updatedAt/?" }
            },
            ExcludedPaths =
            {
                new ExcludedPath { Path = "/invocationData/*" },
                new ExcludedPath { Path = "/stateHistory/*" },
                new ExcludedPath { Path = "/parameters/*" }
            }
        };
    }

    private IndexingPolicy GetServersIndexingPolicy()
    {
        return new IndexingPolicy
        {
            IndexingMode = IndexingMode.Consistent,
            Automatic = true,
            IncludedPaths =
            {
                new IncludedPath { Path = "/*" },
                new IncludedPath { Path = "/documentType/?" },
                new IncludedPath { Path = "/serverId/?" },
                new IncludedPath { Path = "/lastHeartbeat/?" },
                new IncludedPath { Path = "/expireAt/?" }
            }
        };
    }

    private IndexingPolicy GetLocksIndexingPolicy()
    {
        return new IndexingPolicy
        {
            IndexingMode = IndexingMode.Consistent,
            Automatic = true,
            IncludedPaths =
            {
                new IncludedPath { Path = "/*" },
                new IncludedPath { Path = "/documentType/?" },
                new IncludedPath { Path = "/resource/?" },
                new IncludedPath { Path = "/owner/?" },
                new IncludedPath { Path = "/expireAt/?" }
            }
        };
    }

    private IndexingPolicy GetQueuesIndexingPolicy()
    {
        return new IndexingPolicy
        {
            IndexingMode = IndexingMode.Consistent,
            Automatic = true,
            IncludedPaths =
            {
                new IncludedPath { Path = "/*" },
                new IncludedPath { Path = "/documentType/?" },
                new IncludedPath { Path = "/queueName/?" },
                new IncludedPath { Path = "/lastUpdated/?" }
            }
        };
    }

    private IndexingPolicy GetSetsIndexingPolicy()
    {
        return new IndexingPolicy
        {
            IndexingMode = IndexingMode.Consistent,
            Automatic = true,
            IncludedPaths =
            {
                new IncludedPath { Path = "/*" },
                new IncludedPath { Path = "/documentType/?" },
                new IncludedPath { Path = "/key/?" },
                new IncludedPath { Path = "/value/?" },
                new IncludedPath { Path = "/score/?" },
                new IncludedPath { Path = "/createdAt/?" }
            }
        };
    }

    private IndexingPolicy GetHashesIndexingPolicy()
    {
        return new IndexingPolicy
        {
            IndexingMode = IndexingMode.Consistent,
            Automatic = true,
            IncludedPaths =
            {
                new IncludedPath { Path = "/*" },
                new IncludedPath { Path = "/documentType/?" },
                new IncludedPath { Path = "/key/?" },
                new IncludedPath { Path = "/field/?" },
                new IncludedPath { Path = "/createdAt/?" }
            }
        };
    }

    private IndexingPolicy GetListsIndexingPolicy()
    {
        return new IndexingPolicy
        {
            IndexingMode = IndexingMode.Consistent,
            Automatic = true,
            IncludedPaths =
            {
                new IncludedPath { Path = "/*" },
                new IncludedPath { Path = "/documentType/?" },
                new IncludedPath { Path = "/key/?" },
                new IncludedPath { Path = "/index/?" },
                new IncludedPath { Path = "/createdAt/?" }
            }
        };
    }

    private IndexingPolicy GetCountersIndexingPolicy()
    {
        return new IndexingPolicy
        {
            IndexingMode = IndexingMode.Consistent,
            Automatic = true,
            IncludedPaths =
            {
                new IncludedPath { Path = "/*" },
                new IncludedPath { Path = "/documentType/?" },
                new IncludedPath { Path = "/key/?" },
                new IncludedPath { Path = "/expireAt/?" }
            }
        };
    }

    private IndexingPolicy GetMetadataIndexingPolicy()
    {
        return new IndexingPolicy
        {
            IndexingMode = IndexingMode.Consistent,
            Automatic = true,
            IncludedPaths =
            {
                new IncludedPath { Path = "/*" },
                // Common paths for all metadata document types
                new IncludedPath { Path = "/documentType/?" },
                new IncludedPath { Path = "/expireAt/?" },
                // Server document paths
                new IncludedPath { Path = "/serverId/?" },
                new IncludedPath { Path = "/lastHeartbeat/?" },
                // Lock document paths
                new IncludedPath { Path = "/resource/?" },
                new IncludedPath { Path = "/owner/?" },
                // Queue document paths
                new IncludedPath { Path = "/queueName/?" },
                new IncludedPath { Path = "/lastUpdated/?" },
                // Counter document paths
                new IncludedPath { Path = "/key/?" }
            }
        };
    }

    private IndexingPolicy GetCollectionsIndexingPolicy()
    {
        return new IndexingPolicy
        {
            IndexingMode = IndexingMode.Consistent,
            Automatic = true,
            IncludedPaths =
            {
                new IncludedPath { Path = "/*" },
                // Common paths for all collection document types
                new IncludedPath { Path = "/documentType/?" },
                new IncludedPath { Path = "/key/?" },
                new IncludedPath { Path = "/createdAt/?" },
                // Set document paths
                new IncludedPath { Path = "/value/?" },
                new IncludedPath { Path = "/score/?" },
                // Hash document paths
                new IncludedPath { Path = "/field/?" },
                // List document paths
                new IncludedPath { Path = "/index/?" }
            }
        };
    }
}