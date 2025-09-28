using HangfireCosmos.Storage.Documents;
using HangfireCosmos.Storage.Repository;

namespace HangfireCosmos.Storage.Connection;

/// <summary>
/// Represents a distributed lock in Cosmos DB storage.
/// </summary>
public class CosmosDistributedLock : IDisposable
{
    private readonly ICosmosDocumentRepository _repository;
    private readonly CosmosStorageOptions _options;
    private readonly LockDocument _lockDocument;
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the CosmosDistributedLock class.
    /// </summary>
    /// <param name="repository">The document repository.</param>
    /// <param name="options">The storage options.</param>
    /// <param name="lockDocument">The lock document.</param>
    public CosmosDistributedLock(ICosmosDocumentRepository repository, CosmosStorageOptions options, LockDocument lockDocument)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _lockDocument = lockDocument ?? throw new ArgumentNullException(nameof(lockDocument));
    }

    /// <inheritdoc />
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Disposes the distributed lock.
    /// </summary>
    /// <param name="disposing">True if disposing managed resources.</param>
    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed && disposing)
        {
            try
            {
                // Release the lock by deleting the document
                _repository.DeleteDocumentAsync(_options.LocksContainerName, _lockDocument.Id, _lockDocument.PartitionKey).GetAwaiter().GetResult();
            }
            catch
            {
                // Ignore errors when releasing the lock
            }
            _disposed = true;
        }
    }
}