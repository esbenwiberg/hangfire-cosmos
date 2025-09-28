using Hangfire;
using Hangfire.Common;
using Hangfire.Server;
using Hangfire.States;
using Hangfire.Storage;
using HangfireCosmos.Storage.Documents;
using HangfireCosmos.Storage.Repository;
using Microsoft.Azure.Cosmos;
using Newtonsoft.Json;

namespace HangfireCosmos.Storage.Connection;

/// <summary>
/// Implements IStorageConnection for Cosmos DB storage operations.
/// </summary>
public class CosmosStorageConnection : IStorageConnection
{
    private readonly ICosmosDocumentRepository _repository;
    private readonly CosmosStorageOptions _options;
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the CosmosStorageConnection class.
    /// </summary>
    /// <param name="repository">The document repository.</param>
    /// <param name="options">The storage options.</param>
    public CosmosStorageConnection(ICosmosDocumentRepository repository, CosmosStorageOptions options)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _options = options ?? throw new ArgumentNullException(nameof(options));
    }

    /// <inheritdoc />
    public string CreateExpiredJob(Job job, IDictionary<string, string> parameters, TimeSpan expireIn)
    {
        if (job == null) throw new ArgumentNullException(nameof(job));
        if (parameters == null) throw new ArgumentNullException(nameof(parameters));

        var jobId = Guid.NewGuid().ToString("N");
        var queueName = job.Queue ?? "default";
        
        var jobDocument = new JobDocument
        {
            Id = $"job:{jobId}",
            PartitionKey = $"job:{queueName}",
            JobId = jobId,
            QueueName = queueName,
            State = "created",
            InvocationData = SerializeJob(job),
            Parameters = parameters.ToDictionary(kvp => kvp.Key, kvp => kvp.Value),
            ExpireAt = DateTime.UtcNow.Add(expireIn),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _repository.CreateDocumentAsync(_options.JobsContainerName, jobDocument).GetAwaiter().GetResult();
        
        return jobId;
    }

    /// <inheritdoc />
    public string CreateExpiredJob(Job job, IDictionary<string, string> parameters, DateTime createdAt, TimeSpan expireIn)
    {
        if (job == null) throw new ArgumentNullException(nameof(job));
        if (parameters == null) throw new ArgumentNullException(nameof(parameters));

        var jobId = Guid.NewGuid().ToString("N");
        var queueName = job.Queue ?? "default";
        
        var jobDocument = new JobDocument
        {
            Id = $"job:{jobId}",
            PartitionKey = $"job:{queueName}",
            JobId = jobId,
            QueueName = queueName,
            State = "created",
            InvocationData = SerializeJob(job),
            Parameters = parameters.ToDictionary(kvp => kvp.Key, kvp => kvp.Value),
            ExpireAt = DateTime.UtcNow.Add(expireIn),
            CreatedAt = createdAt,
            UpdatedAt = DateTime.UtcNow
        };

        _repository.CreateDocumentAsync(_options.JobsContainerName, jobDocument).GetAwaiter().GetResult();
        
        return jobId;
    }

    /// <inheritdoc />
    public IWriteOnlyTransaction CreateWriteTransaction()
    {
        return new CosmosWriteOnlyTransaction(_repository, _options);
    }

    /// <inheritdoc />
    public IFetchedJob FetchNextJob(string[] queues, CancellationToken cancellationToken)
    {
        if (queues == null || queues.Length == 0)
            throw new ArgumentException("Queues cannot be null or empty.", nameof(queues));

        // This is a simplified implementation - in production, you'd want more sophisticated job fetching
        foreach (var queue in queues)
        {
            var query = new QueryDefinition(
                "SELECT * FROM c WHERE c.documentType = 'job' AND c.queueName = @queueName AND c.state = 'enqueued' ORDER BY c.createdAt")
                .WithParameter("@queueName", queue);

            var jobs = _repository.QueryDocumentsAsync<JobDocument>(
                _options.JobsContainerName, query, $"job:{queue}", cancellationToken).GetAwaiter().GetResult();

            var job = jobs.FirstOrDefault();
            if (job != null)
            {
                // Mark job as fetched
                job.State = "processing";
                job.UpdatedAt = DateTime.UtcNow;
                _repository.UpdateDocumentAsync(_options.JobsContainerName, job, cancellationToken).GetAwaiter().GetResult();

                return new CosmosFetchedJob(_repository, _options, job);
            }
        }

        return null!; // No jobs available
    }

    /// <inheritdoc />
    public string GetFirstByLowestScoreFromSet(string key, double fromScore, double toScore)
    {
        if (string.IsNullOrEmpty(key)) throw new ArgumentNullException(nameof(key));

        var query = new QueryDefinition(
            "SELECT * FROM c WHERE c.documentType = 'set' AND c.key = @key AND c.score >= @fromScore AND c.score <= @toScore ORDER BY c.score")
            .WithParameter("@key", key)
            .WithParameter("@fromScore", fromScore)
            .WithParameter("@toScore", toScore);

        var sets = _repository.QueryDocumentsAsync<SetDocument>(
            _options.SetsContainerName, query, $"set:{key}").GetAwaiter().GetResult();

        return sets.FirstOrDefault()?.Value ?? string.Empty;
    }

    /// <inheritdoc />
    public JobData GetJobData(string jobId)
    {
        if (string.IsNullOrEmpty(jobId)) throw new ArgumentNullException(nameof(jobId));

        var job = GetJobDocument(jobId);
        if (job == null) return null!;

        return new JobData
        {
            Job = DeserializeJob(job.InvocationData),
            State = job.State,
            CreatedAt = job.CreatedAt,
            LoadException = null // TODO: Handle load exceptions
        };
    }

    /// <inheritdoc />
    public StateData GetStateData(string jobId)
    {
        if (string.IsNullOrEmpty(jobId)) throw new ArgumentNullException(nameof(jobId));

        var job = GetJobDocument(jobId);
        if (job == null) return null!;

        return new StateData
        {
            Name = job.State,
            Reason = job.StateHistory.LastOrDefault()?.Reason,
            Data = job.StateData
        };
    }

    /// <inheritdoc />
    public List<string> GetRangeFromSet(string key, int startingFrom, int endingAt)
    {
        if (string.IsNullOrEmpty(key)) throw new ArgumentNullException(nameof(key));

        var query = new QueryDefinition(
            "SELECT * FROM c WHERE c.documentType = 'set' AND c.key = @key ORDER BY c.score OFFSET @offset LIMIT @limit")
            .WithParameter("@key", key)
            .WithParameter("@offset", startingFrom)
            .WithParameter("@limit", endingAt - startingFrom + 1);

        var sets = _repository.QueryDocumentsAsync<SetDocument>(
            _options.SetsContainerName, query, $"set:{key}").GetAwaiter().GetResult();

        return sets.Select(s => s.Value).ToList();
    }

    /// <inheritdoc />
    public List<string> GetRangeFromList(string key, int startingFrom, int endingAt)
    {
        if (string.IsNullOrEmpty(key)) throw new ArgumentNullException(nameof(key));

        var query = new QueryDefinition(
            "SELECT * FROM c WHERE c.documentType = 'list' AND c.key = @key ORDER BY c.index OFFSET @offset LIMIT @limit")
            .WithParameter("@key", key)
            .WithParameter("@offset", startingFrom)
            .WithParameter("@limit", endingAt - startingFrom + 1);

        var lists = _repository.QueryDocumentsAsync<ListDocument>(
            _options.ListsContainerName, query, $"list:{key}").GetAwaiter().GetResult();

        return lists.Select(l => l.Value).ToList();
    }

    /// <inheritdoc />
    public Dictionary<string, string> GetAllEntriesFromHash(string key)
    {
        if (string.IsNullOrEmpty(key)) throw new ArgumentNullException(nameof(key));

        var query = new QueryDefinition(
            "SELECT * FROM c WHERE c.documentType = 'hash' AND c.key = @key")
            .WithParameter("@key", key);

        var hashes = _repository.QueryDocumentsAsync<HashDocument>(
            _options.HashesContainerName, query, $"hash:{key}").GetAwaiter().GetResult();

        return hashes.ToDictionary(h => h.Field, h => h.Value);
    }

    /// <inheritdoc />
    public long GetCounter(string key)
    {
        if (string.IsNullOrEmpty(key)) throw new ArgumentNullException(nameof(key));

        var counter = _repository.GetDocumentAsync<CounterDocument>(
            _options.CountersContainerName, $"counter:{key}", "counters").GetAwaiter().GetResult();

        return counter?.Value ?? 0;
    }

    /// <inheritdoc />
    public long GetHashCount(string key)
    {
        if (string.IsNullOrEmpty(key)) throw new ArgumentNullException(nameof(key));

        var query = new QueryDefinition(
            "SELECT * FROM c WHERE c.documentType = 'hash' AND c.key = @key")
            .WithParameter("@key", key);

        var result = _repository.QueryDocumentsAsync<HashDocument>(
            _options.HashesContainerName, query, $"hash:{key}").GetAwaiter().GetResult();

        return result.Count();
    }

    /// <inheritdoc />
    public long GetListCount(string key)
    {
        if (string.IsNullOrEmpty(key)) throw new ArgumentNullException(nameof(key));

        var query = new QueryDefinition(
            "SELECT * FROM c WHERE c.documentType = 'list' AND c.key = @key")
            .WithParameter("@key", key);

        var result = _repository.QueryDocumentsAsync<ListDocument>(
            _options.ListsContainerName, query, $"list:{key}").GetAwaiter().GetResult();

        return result.Count();
    }

    /// <inheritdoc />
    public long GetSetCount(string key)
    {
        if (string.IsNullOrEmpty(key)) throw new ArgumentNullException(nameof(key));

        var query = new QueryDefinition(
            "SELECT * FROM c WHERE c.documentType = 'set' AND c.key = @key")
            .WithParameter("@key", key);

        var result = _repository.QueryDocumentsAsync<SetDocument>(
            _options.SetsContainerName, query, $"set:{key}").GetAwaiter().GetResult();

        return result.Count();
    }

    /// <inheritdoc />
    public HashSet<string> GetAllItemsFromSet(string key)
    {
        if (string.IsNullOrEmpty(key)) throw new ArgumentNullException(nameof(key));

        var query = new QueryDefinition(
            "SELECT * FROM c WHERE c.documentType = 'set' AND c.key = @key ORDER BY c.score")
            .WithParameter("@key", key);

        var sets = _repository.QueryDocumentsAsync<SetDocument>(
            _options.SetsContainerName, query, $"set:{key}").GetAwaiter().GetResult();

        return new HashSet<string>(sets.Select(s => s.Value));
    }

    /// <inheritdoc />
    public void SetRangeInHash(string key, IEnumerable<KeyValuePair<string, string>> keyValuePairs)
    {
        if (string.IsNullOrEmpty(key)) throw new ArgumentNullException(nameof(key));
        if (keyValuePairs == null) throw new ArgumentNullException(nameof(keyValuePairs));

        foreach (var kvp in keyValuePairs)
        {
            var hashDocument = new HashDocument
            {
                Id = $"hash:{key}:{kvp.Key}",
                Key = key,
                Field = kvp.Key,
                Value = kvp.Value
            };
            hashDocument.SetPartitionKey(key);

            _repository.UpsertDocumentAsync(_options.HashesContainerName, hashDocument).GetAwaiter().GetResult();
        }
    }

    /// <inheritdoc />
    public string GetJobParameter(string id, string name)
    {
        if (string.IsNullOrEmpty(id)) throw new ArgumentNullException(nameof(id));
        if (string.IsNullOrEmpty(name)) throw new ArgumentNullException(nameof(name));

        var job = GetJobDocument(id);
        return job?.Parameters.TryGetValue(name, out var value) == true ? value : null!;
    }

    /// <inheritdoc />
    public void SetJobParameter(string id, string name, string value)
    {
        if (string.IsNullOrEmpty(id)) throw new ArgumentNullException(nameof(id));
        if (string.IsNullOrEmpty(name)) throw new ArgumentNullException(nameof(name));

        var job = GetJobDocument(id);
        if (job != null)
        {
            job.Parameters[name] = value ?? string.Empty;
            job.UpdatedAt = DateTime.UtcNow;
            _repository.UpdateDocumentAsync(_options.JobsContainerName, job).GetAwaiter().GetResult();
        }
    }

    /// <inheritdoc />
    public void AnnounceServer(string serverId, ServerContext context)
    {
        if (string.IsNullOrEmpty(serverId)) throw new ArgumentNullException(nameof(serverId));
        if (context == null) throw new ArgumentNullException(nameof(context));

        var serverDocument = new ServerDocument
        {
            Id = $"server:{serverId}",
            ServerId = serverId,
            Data = new ServerData
            {
                WorkerCount = context.WorkerCount,
                Queues = context.Queues.ToArray(),
                StartedAt = DateTime.UtcNow,
                Name = Environment.MachineName
            },
            LastHeartbeat = DateTime.UtcNow,
            StartedAt = DateTime.UtcNow,
            ExpireAt = DateTime.UtcNow.Add(_options.ServerTimeout)
        };

        _repository.UpsertDocumentAsync(_options.ServersContainerName, serverDocument).GetAwaiter().GetResult();
    }

    /// <inheritdoc />
    public void RemoveServer(string serverId)
    {
        if (string.IsNullOrEmpty(serverId)) throw new ArgumentNullException(nameof(serverId));

        _repository.DeleteDocumentAsync(_options.ServersContainerName, $"server:{serverId}", "servers").GetAwaiter().GetResult();
    }

    /// <inheritdoc />
    public void Heartbeat(string serverId)
    {
        if (string.IsNullOrEmpty(serverId)) throw new ArgumentNullException(nameof(serverId));

        var server = _repository.GetDocumentAsync<ServerDocument>(
            _options.ServersContainerName, $"server:{serverId}", "servers").GetAwaiter().GetResult();

        if (server != null)
        {
            server.LastHeartbeat = DateTime.UtcNow;
            server.ExpireAt = DateTime.UtcNow.Add(_options.ServerTimeout);
            _repository.UpdateDocumentAsync(_options.ServersContainerName, server).GetAwaiter().GetResult();
        }
    }

    /// <inheritdoc />
    public int RemoveTimedOutServers(TimeSpan timeOut)
    {
        var cutoff = DateTime.UtcNow.Subtract(timeOut);
        var query = new QueryDefinition(
            "SELECT * FROM c WHERE c.documentType = 'server' AND c.lastHeartbeat < @cutoff")
            .WithParameter("@cutoff", cutoff);

        var timedOutServers = _repository.QueryDocumentsAsync<ServerDocument>(
            _options.ServersContainerName, query, "servers").GetAwaiter().GetResult();

        var count = 0;
        foreach (var server in timedOutServers)
        {
            _repository.DeleteDocumentAsync(_options.ServersContainerName, server.Id, server.PartitionKey).GetAwaiter().GetResult();
            count++;
        }

        return count;
    }

    /// <inheritdoc />
    public IDisposable AcquireDistributedLock(string resource, TimeSpan timeout)
    {
        if (string.IsNullOrEmpty(resource)) throw new ArgumentNullException(nameof(resource));

        // Simplified lock implementation - in production, you'd want a more robust distributed lock
        var lockDocument = new LockDocument
        {
            Id = $"lock:{resource}",
            Resource = resource,
            Owner = Environment.MachineName,
            AcquiredAt = DateTime.UtcNow,
            Timeout = timeout,
            ExpireAt = DateTime.UtcNow.Add(timeout)
        };

        try
        {
            _repository.CreateDocumentAsync(_options.LocksContainerName, lockDocument).GetAwaiter().GetResult();
            return new CosmosDistributedLock(_repository, _options, lockDocument);
        }
        catch
        {
            throw new InvalidOperationException($"Could not acquire lock for resource '{resource}'.");
        }
    }

    /// <summary>
    /// Gets a job document by job ID.
    /// </summary>
    /// <param name="jobId">The job ID.</param>
    /// <returns>The job document or null if not found.</returns>
    private JobDocument? GetJobDocument(string jobId)
    {
        // We need to query across all partitions since we don't know the queue name
        var query = new QueryDefinition(
            "SELECT * FROM c WHERE c.documentType = 'job' AND c.jobId = @jobId")
            .WithParameter("@jobId", jobId);

        var jobs = _repository.QueryDocumentsAsync<JobDocument>(
            _options.JobsContainerName, query).GetAwaiter().GetResult();

        return jobs.FirstOrDefault();
    }

    /// <summary>
    /// Serializes a job to invocation data.
    /// </summary>
    /// <param name="job">The job to serialize.</param>
    /// <returns>The invocation data.</returns>
    private Documents.InvocationData SerializeJob(Job job)
    {
        return new Documents.InvocationData
        {
            Type = job.Type.AssemblyQualifiedName ?? job.Type.FullName ?? string.Empty,
            Method = job.Method.Name,
            ParameterTypes = job.Method.GetParameters().Select(p => p.ParameterType.AssemblyQualifiedName ?? p.ParameterType.FullName ?? string.Empty).ToArray(),
            Arguments = job.Args?.Select(arg => JsonConvert.SerializeObject(arg)).ToArray() ?? Array.Empty<string>(),
            GenericArguments = job.Method.IsGenericMethod ? job.Method.GetGenericArguments().Select(t => t.AssemblyQualifiedName ?? t.FullName ?? string.Empty).ToArray() : null
        };
    }

    /// <summary>
    /// Deserializes invocation data to a job.
    /// </summary>
    /// <param name="invocationData">The invocation data.</param>
    /// <returns>The deserialized job.</returns>
    private Job DeserializeJob(Documents.InvocationData invocationData)
    {
        var type = Type.GetType(invocationData.Type);
        if (type == null) throw new InvalidOperationException($"Could not load type '{invocationData.Type}'.");

        var parameterTypes = invocationData.ParameterTypes.Select(Type.GetType).ToArray();
        var method = type.GetMethod(invocationData.Method, parameterTypes!);
        if (method == null) throw new InvalidOperationException($"Could not find method '{invocationData.Method}' on type '{type.FullName}'.");

        var arguments = invocationData.Arguments.Select(arg => JsonConvert.DeserializeObject(arg)).ToArray();

        return new Job(method, arguments);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Disposes the connection.
    /// </summary>
    /// <param name="disposing">True if disposing managed resources.</param>
    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed && disposing)
        {
            // Dispose managed resources if needed
            _disposed = true;
        }
    }
}