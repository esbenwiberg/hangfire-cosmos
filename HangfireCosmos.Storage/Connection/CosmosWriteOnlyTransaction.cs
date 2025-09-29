using Hangfire.States;
using Hangfire.Storage;
using HangfireCosmos.Storage.Documents;
using HangfireCosmos.Storage.Repository;
using System.Collections.Generic;

namespace HangfireCosmos.Storage.Connection;

/// <summary>
/// Implements IWriteOnlyTransaction for Cosmos DB storage operations.
/// </summary>
public class CosmosWriteOnlyTransaction : IWriteOnlyTransaction
{
    private readonly ICosmosDocumentRepository _repository;
    private readonly CosmosStorageOptions _options;
    private readonly List<Func<Task>> _operations;
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the CosmosWriteOnlyTransaction class.
    /// </summary>
    /// <param name="repository">The document repository.</param>
    /// <param name="options">The storage options.</param>
    public CosmosWriteOnlyTransaction(ICosmosDocumentRepository repository, CosmosStorageOptions options)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _operations = new List<Func<Task>>();
    }

    /// <inheritdoc />
    public void ExpireJob(string jobId, TimeSpan expireIn)
    {
        if (string.IsNullOrEmpty(jobId)) throw new ArgumentNullException(nameof(jobId));

        _operations.Add(async () =>
        {
            var job = await GetJobDocumentAsync(jobId);
            if (job != null)
            {
                job.ExpireAt = DateTime.UtcNow.Add(expireIn);
                job.UpdatedAt = DateTime.UtcNow;
                await _repository.UpdateDocumentAsync(_options.JobsContainerName, job);
            }
        });
    }

    /// <inheritdoc />
    public void PersistJob(string jobId)
    {
        if (string.IsNullOrEmpty(jobId)) throw new ArgumentNullException(nameof(jobId));

        _operations.Add(async () =>
        {
            var job = await GetJobDocumentAsync(jobId);
            if (job != null)
            {
                job.ExpireAt = null;
                job.UpdatedAt = DateTime.UtcNow;
                await _repository.UpdateDocumentAsync(_options.JobsContainerName, job);
            }
        });
    }

    /// <inheritdoc />
    public void SetJobState(string jobId, IState state)
    {
        if (string.IsNullOrEmpty(jobId)) throw new ArgumentNullException(nameof(jobId));
        if (state == null) throw new ArgumentNullException(nameof(state));

        _operations.Add(async () =>
        {
            var job = await GetJobDocumentAsync(jobId);
            if (job != null)
            {
                job.State = state.Name;
                job.StateData = state.SerializeData();
                job.UpdatedAt = DateTime.UtcNow;

                // Add to state history
                job.StateHistory.Add(new StateHistoryEntry
                {
                    State = state.Name,
                    Reason = state.Reason,
                    CreatedAt = DateTime.UtcNow,
                    Data = state.SerializeData()
                });

                await _repository.UpdateDocumentAsync(_options.JobsContainerName, job);
            }
        });
    }

    /// <inheritdoc />
    public void AddJobState(string jobId, IState state)
    {
        if (string.IsNullOrEmpty(jobId)) throw new ArgumentNullException(nameof(jobId));
        if (state == null) throw new ArgumentNullException(nameof(state));

        _operations.Add(async () =>
        {
            var job = await GetJobDocumentAsync(jobId);
            if (job != null)
            {
                // Add to state history without changing current state
                job.StateHistory.Add(new StateHistoryEntry
                {
                    State = state.Name,
                    Reason = state.Reason,
                    CreatedAt = DateTime.UtcNow,
                    Data = state.SerializeData()
                });

                job.UpdatedAt = DateTime.UtcNow;
                await _repository.UpdateDocumentAsync(_options.JobsContainerName, job);
            }
        });
    }

    /// <inheritdoc />
    public void AddToQueue(string queue, string jobId)
    {
        if (string.IsNullOrEmpty(queue)) throw new ArgumentNullException(nameof(queue));
        if (string.IsNullOrEmpty(jobId)) throw new ArgumentNullException(nameof(jobId));

        _operations.Add(async () =>
        {
            var job = await GetJobDocumentAsync(jobId);
            if (job != null)
            {
                job.QueueName = queue;
                job.PartitionKey = $"job:{queue}";
                job.State = "enqueued";
                
                // Set state data with Queue information for Hangfire dashboard
                job.StateData = new Dictionary<string, string>
                {
                    ["Queue"] = queue,
                    ["EnqueuedAt"] = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffffffZ")
                };
                
                job.UpdatedAt = DateTime.UtcNow;

                // Add to state history
                job.StateHistory.Add(new StateHistoryEntry
                {
                    State = "enqueued",
                    Reason = "Enqueued",
                    CreatedAt = DateTime.UtcNow,
                    Data = job.StateData
                });

                await _repository.UpdateDocumentAsync(_options.JobsContainerName, job);
            }
        });
    }

    /// <inheritdoc />
    public void IncrementCounter(string key)
    {
        if (string.IsNullOrEmpty(key)) throw new ArgumentNullException(nameof(key));

        _operations.Add(async () =>
        {
            var counter = await _repository.GetDocumentAsync<CounterDocument>(
                _options.CountersContainerName, $"counter:{key}", "counters") ?? 
                new CounterDocument { Id = $"counter:{key}", Key = key, Value = 0 };

            counter.Value++;
            counter.UpdatedAt = DateTime.UtcNow;
            await _repository.UpsertDocumentAsync(_options.CountersContainerName, counter);
        });
    }

    /// <inheritdoc />
    public void IncrementCounter(string key, TimeSpan expireIn)
    {
        if (string.IsNullOrEmpty(key)) throw new ArgumentNullException(nameof(key));

        _operations.Add(async () =>
        {
            var counter = await _repository.GetDocumentAsync<CounterDocument>(
                _options.CountersContainerName, $"counter:{key}", "counters") ?? 
                new CounterDocument { Id = $"counter:{key}", Key = key, Value = 0 };

            counter.Value++;
            counter.ExpireAt = DateTime.UtcNow.Add(expireIn);
            counter.UpdatedAt = DateTime.UtcNow;
            await _repository.UpsertDocumentAsync(_options.CountersContainerName, counter);
        });
    }

    /// <inheritdoc />
    public void DecrementCounter(string key)
    {
        if (string.IsNullOrEmpty(key)) throw new ArgumentNullException(nameof(key));

        _operations.Add(async () =>
        {
            var counter = await _repository.GetDocumentAsync<CounterDocument>(
                _options.CountersContainerName, $"counter:{key}", "counters");
            
            if (counter != null)
            {
                counter.Value--;
                counter.UpdatedAt = DateTime.UtcNow;
                await _repository.UpdateDocumentAsync(_options.CountersContainerName, counter);
            }
        });
    }

    /// <inheritdoc />
    public void DecrementCounter(string key, TimeSpan expireIn)
    {
        if (string.IsNullOrEmpty(key)) throw new ArgumentNullException(nameof(key));

        _operations.Add(async () =>
        {
            var counter = await _repository.GetDocumentAsync<CounterDocument>(
                _options.CountersContainerName, $"counter:{key}", "counters");
            
            if (counter != null)
            {
                counter.Value--;
                counter.ExpireAt = DateTime.UtcNow.Add(expireIn);
                counter.UpdatedAt = DateTime.UtcNow;
                await _repository.UpdateDocumentAsync(_options.CountersContainerName, counter);
            }
        });
    }

    /// <inheritdoc />
    public void AddToSet(string key, string value)
    {
        if (string.IsNullOrEmpty(key)) throw new ArgumentNullException(nameof(key));
        if (string.IsNullOrEmpty(value)) throw new ArgumentNullException(nameof(value));

        _operations.Add(async () =>
        {
            var setDocument = new SetDocument
            {
                Id = $"set:{key}:{value}",
                Key = key,
                Value = value,
                Score = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
            };
            setDocument.SetPartitionKey(key);

            await _repository.UpsertDocumentAsync(_options.SetsContainerName, setDocument);
        });
    }

    /// <inheritdoc />
    public void AddToSet(string key, string value, double score)
    {
        if (string.IsNullOrEmpty(key)) throw new ArgumentNullException(nameof(key));
        if (string.IsNullOrEmpty(value)) throw new ArgumentNullException(nameof(value));

        _operations.Add(async () =>
        {
            var setDocument = new SetDocument
            {
                Id = $"set:{key}:{value}",
                Key = key,
                Value = value,
                Score = score
            };
            setDocument.SetPartitionKey(key);

            await _repository.UpsertDocumentAsync(_options.SetsContainerName, setDocument);
        });
    }

    /// <inheritdoc />
    public void RemoveFromSet(string key, string value)
    {
        if (string.IsNullOrEmpty(key)) throw new ArgumentNullException(nameof(key));
        if (string.IsNullOrEmpty(value))
        {
            // Hangfire sometimes passes null/empty values for cleanup operations
            // Just ignore these instead of throwing an exception
            return;
        }

        _operations.Add(async () =>
        {
            await _repository.DeleteDocumentAsync(_options.SetsContainerName, $"set:{key}:{value}", $"set:{key}");
        });
    }

    /// <inheritdoc />
    public void InsertToList(string key, string value)
    {
        if (string.IsNullOrEmpty(key)) throw new ArgumentNullException(nameof(key));
        if (string.IsNullOrEmpty(value)) throw new ArgumentNullException(nameof(value));

        _operations.Add(async () =>
        {
            // Get the current max index for this list by querying all list items
            var query = new Microsoft.Azure.Cosmos.QueryDefinition(
                "SELECT * FROM c WHERE c.documentType = 'list' AND c.key = @key ORDER BY c.index DESC")
                .WithParameter("@key", key);

            var results = await _repository.QueryDocumentsAsync<ListDocument>(
                _options.ListsContainerName, query, $"list:{key}");
            
            var maxIndex = results.FirstOrDefault()?.Index ?? -1;
            var newIndex = maxIndex + 1;

            var listDocument = new ListDocument
            {
                Id = $"list:{key}:{newIndex}",
                Key = key,
                Index = newIndex,
                Value = value
            };
            listDocument.SetPartitionKey(key);

            await _repository.CreateDocumentAsync(_options.ListsContainerName, listDocument);
        });
    }

    /// <inheritdoc />
    public void RemoveFromList(string key, string value)
    {
        if (string.IsNullOrEmpty(key)) throw new ArgumentNullException(nameof(key));
        if (string.IsNullOrEmpty(value)) throw new ArgumentNullException(nameof(value));

        _operations.Add(async () =>
        {
            var query = new Microsoft.Azure.Cosmos.QueryDefinition(
                "SELECT * FROM c WHERE c.documentType = 'list' AND c.key = @key AND c.value = @value")
                .WithParameter("@key", key)
                .WithParameter("@value", value);

            var lists = await _repository.QueryDocumentsAsync<ListDocument>(
                _options.ListsContainerName, query, $"list:{key}");

            foreach (var list in lists)
            {
                await _repository.DeleteDocumentAsync(_options.ListsContainerName, list.Id, list.PartitionKey);
            }
        });
    }

    /// <inheritdoc />
    public void TrimList(string key, int keepStartingFrom, int keepEndingAt)
    {
        if (string.IsNullOrEmpty(key)) throw new ArgumentNullException(nameof(key));

        _operations.Add(async () =>
        {
            var query = new Microsoft.Azure.Cosmos.QueryDefinition(
                "SELECT * FROM c WHERE c.documentType = 'list' AND c.key = @key AND (c.index < @start OR c.index > @end)")
                .WithParameter("@key", key)
                .WithParameter("@start", keepStartingFrom)
                .WithParameter("@end", keepEndingAt);

            var listsToDelete = await _repository.QueryDocumentsAsync<ListDocument>(
                _options.ListsContainerName, query, $"list:{key}");

            foreach (var list in listsToDelete)
            {
                await _repository.DeleteDocumentAsync(_options.ListsContainerName, list.Id, list.PartitionKey);
            }
        });
    }

    /// <inheritdoc />
    public void SetRangeInHash(string key, IEnumerable<KeyValuePair<string, string>> keyValuePairs)
    {
        if (string.IsNullOrEmpty(key)) throw new ArgumentNullException(nameof(key));
        if (keyValuePairs == null) throw new ArgumentNullException(nameof(keyValuePairs));

        _operations.Add(async () =>
        {
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

                await _repository.UpsertDocumentAsync(_options.HashesContainerName, hashDocument);
            }
        });
    }

    /// <inheritdoc />
    public void RemoveHash(string key)
    {
        if (string.IsNullOrEmpty(key)) throw new ArgumentNullException(nameof(key));

        _operations.Add(async () =>
        {
            var query = new Microsoft.Azure.Cosmos.QueryDefinition(
                "SELECT * FROM c WHERE c.documentType = 'hash' AND c.key = @key")
                .WithParameter("@key", key);

            var hashes = await _repository.QueryDocumentsAsync<HashDocument>(
                _options.HashesContainerName, query, $"hash:{key}");

            foreach (var hash in hashes)
            {
                await _repository.DeleteDocumentAsync(_options.HashesContainerName, hash.Id, hash.PartitionKey);
            }
        });
    }

    /// <inheritdoc />
    public void Commit()
    {
        try
        {
            // Execute all operations
            var tasks = _operations.Select(op => op()).ToArray();
            Task.WaitAll(tasks);
        }
        catch (Exception)
        {
            // In a real implementation, you might want to implement rollback logic
            throw;
        }
        finally
        {
            _operations.Clear();
        }
    }

    /// <summary>
    /// Gets a job document by job ID.
    /// </summary>
    /// <param name="jobId">The job ID.</param>
    /// <returns>The job document or null if not found.</returns>
    private async Task<JobDocument?> GetJobDocumentAsync(string jobId)
    {
        var query = new Microsoft.Azure.Cosmos.QueryDefinition(
            "SELECT * FROM c WHERE c.documentType = 'job' AND c.jobId = @jobId")
            .WithParameter("@jobId", jobId);

        var jobs = await _repository.QueryDocumentsAsync<JobDocument>(_options.JobsContainerName, query);
        return jobs.FirstOrDefault();
    }

    /// <inheritdoc />
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Disposes the transaction.
    /// </summary>
    /// <param name="disposing">True if disposing managed resources.</param>
    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed && disposing)
        {
            _operations.Clear();
            _disposed = true;
        }
    }
}