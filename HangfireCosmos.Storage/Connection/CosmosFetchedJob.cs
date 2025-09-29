using Hangfire.Storage;
using HangfireCosmos.Storage.Documents;
using HangfireCosmos.Storage.Repository;
using System.Collections.Generic;

namespace HangfireCosmos.Storage.Connection;

/// <summary>
/// Represents a fetched job from Cosmos DB storage.
/// </summary>
public class CosmosFetchedJob : IFetchedJob
{
    private readonly ICosmosDocumentRepository _repository;
    private readonly CosmosStorageOptions _options;
    private readonly JobDocument _jobDocument;
    private bool _disposed;
    private bool _removedFromQueue;
    private bool _requeued;

    /// <summary>
    /// Initializes a new instance of the CosmosFetchedJob class.
    /// </summary>
    /// <param name="repository">The document repository.</param>
    /// <param name="options">The storage options.</param>
    /// <param name="jobDocument">The job document.</param>
    public CosmosFetchedJob(ICosmosDocumentRepository repository, CosmosStorageOptions options, JobDocument jobDocument)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _jobDocument = jobDocument ?? throw new ArgumentNullException(nameof(jobDocument));
    }

    /// <inheritdoc />
    public string JobId => _jobDocument.JobId;

    /// <inheritdoc />
    public void RemoveFromQueue()
    {
        if (_removedFromQueue || _requeued) return;

        // Mark the job as processed by updating its state
        _jobDocument.State = "processing";
        _jobDocument.UpdatedAt = DateTime.UtcNow;
        
        _repository.UpdateDocumentAsync(_options.JobsContainerName, _jobDocument).GetAwaiter().GetResult();
        _removedFromQueue = true;
    }

    /// <inheritdoc />
    public void Requeue()
    {
        if (_removedFromQueue || _requeued) return;

        // Requeue the job by setting it back to enqueued state
        _jobDocument.State = "enqueued";
        
        // Set state data with Queue information for Hangfire dashboard
        _jobDocument.StateData = new Dictionary<string, string>
        {
            ["Queue"] = _jobDocument.QueueName ?? "default",
            ["EnqueuedAt"] = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffffffZ")
        };
        
        _jobDocument.UpdatedAt = DateTime.UtcNow;

        // Add to state history
        _jobDocument.StateHistory.Add(new StateHistoryEntry
        {
            State = "enqueued",
            Reason = "Requeued",
            CreatedAt = DateTime.UtcNow,
            Data = _jobDocument.StateData
        });
        
        _repository.UpdateDocumentAsync(_options.JobsContainerName, _jobDocument).GetAwaiter().GetResult();
        _requeued = true;
    }

    /// <inheritdoc />
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Disposes the fetched job.
    /// </summary>
    /// <param name="disposing">True if disposing managed resources.</param>
    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed && disposing)
        {
            if (!_removedFromQueue && !_requeued)
            {
                Requeue();
            }
            _disposed = true;
        }
    }
}