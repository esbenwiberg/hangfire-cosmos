using HangfireCosmos.Storage.Documents;
using HangfireCosmos.Storage.Repository;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Logging;

namespace HangfireCosmos.Storage.Resilience;

/// <summary>
/// Enhanced Cosmos document repository with circuit breaker protection for improved resilience.
/// </summary>
public class ResilientCosmosDocumentRepository : ICosmosDocumentRepository
{
    private readonly ICosmosDocumentRepository _innerRepository;
    private readonly CosmosCircuitBreaker _circuitBreaker;
    private readonly ILogger<ResilientCosmosDocumentRepository>? _logger;

    /// <summary>
    /// Initializes a new instance of the ResilientCosmosDocumentRepository class.
    /// </summary>
    /// <param name="innerRepository">The inner repository to wrap with circuit breaker protection.</param>
    /// <param name="circuitBreaker">The circuit breaker instance.</param>
    /// <param name="logger">The optional logger instance.</param>
    public ResilientCosmosDocumentRepository(
        ICosmosDocumentRepository innerRepository,
        CosmosCircuitBreaker circuitBreaker,
        ILogger<ResilientCosmosDocumentRepository>? logger = null)
    {
        _innerRepository = innerRepository ?? throw new ArgumentNullException(nameof(innerRepository));
        _circuitBreaker = circuitBreaker ?? throw new ArgumentNullException(nameof(circuitBreaker));
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<T?> GetDocumentAsync<T>(string containerName, string id, string partitionKey, CancellationToken cancellationToken = default) 
        where T : BaseDocument
    {
        return await _circuitBreaker.ExecuteAsync(
            () => _innerRepository.GetDocumentAsync<T>(containerName, id, partitionKey, cancellationToken),
            $"GetDocument-{typeof(T).Name}");
    }

    /// <inheritdoc />
    public async Task<T> CreateDocumentAsync<T>(string containerName, T document, CancellationToken cancellationToken = default) 
        where T : BaseDocument
    {
        return await _circuitBreaker.ExecuteAsync(
            () => _innerRepository.CreateDocumentAsync(containerName, document, cancellationToken),
            $"CreateDocument-{typeof(T).Name}");
    }

    /// <inheritdoc />
    public async Task<T> UpsertDocumentAsync<T>(string containerName, T document, CancellationToken cancellationToken = default) 
        where T : BaseDocument
    {
        return await _circuitBreaker.ExecuteAsync(
            () => _innerRepository.UpsertDocumentAsync(containerName, document, cancellationToken),
            $"UpsertDocument-{typeof(T).Name}");
    }

    /// <inheritdoc />
    public async Task<T> UpdateDocumentAsync<T>(string containerName, T document, CancellationToken cancellationToken = default) 
        where T : BaseDocument
    {
        return await _circuitBreaker.ExecuteAsync(
            () => _innerRepository.UpdateDocumentAsync(containerName, document, cancellationToken),
            $"UpdateDocument-{typeof(T).Name}");
    }

    /// <inheritdoc />
    public async Task DeleteDocumentAsync(string containerName, string id, string partitionKey, CancellationToken cancellationToken = default)
    {
        await _circuitBreaker.ExecuteAsync(
            () => _innerRepository.DeleteDocumentAsync(containerName, id, partitionKey, cancellationToken),
            "DeleteDocument");
    }

    /// <inheritdoc />
    public async Task<IEnumerable<T>> QueryDocumentsAsync<T>(string containerName, string query, object? parameters = null, string? partitionKey = null, CancellationToken cancellationToken = default) 
        where T : BaseDocument
    {
        return await _circuitBreaker.ExecuteAsync(
            () => _innerRepository.QueryDocumentsAsync<T>(containerName, query, parameters, partitionKey, cancellationToken),
            $"QueryDocuments-{typeof(T).Name}");
    }

    /// <inheritdoc />
    public async Task<IEnumerable<T>> QueryDocumentsAsync<T>(string containerName, QueryDefinition queryDefinition, string? partitionKey = null, CancellationToken cancellationToken = default) 
        where T : BaseDocument
    {
        return await _circuitBreaker.ExecuteAsync(
            () => _innerRepository.QueryDocumentsAsync<T>(containerName, queryDefinition, partitionKey, cancellationToken),
            $"QueryDocuments-{typeof(T).Name}");
    }

    /// <inheritdoc />
    public async Task<PagedResult<T>> QueryDocumentsPagedAsync<T>(string containerName, QueryDefinition queryDefinition, string? continuationToken = null, int? maxItemCount = null, string? partitionKey = null, CancellationToken cancellationToken = default) 
        where T : BaseDocument
    {
        return await _circuitBreaker.ExecuteAsync(
            () => _innerRepository.QueryDocumentsPagedAsync<T>(containerName, queryDefinition, continuationToken, maxItemCount, partitionKey, cancellationToken),
            $"QueryDocumentsPaged-{typeof(T).Name}");
    }

    /// <inheritdoc />
    public Container GetContainer(string containerName)
    {
        // Container retrieval is typically a local operation and doesn't need circuit breaker protection
        return _innerRepository.GetContainer(containerName);
    }

    /// <inheritdoc />
    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        await _circuitBreaker.ExecuteAsync(
            () => _innerRepository.InitializeAsync(cancellationToken),
            "Initialize");
    }

    /// <summary>
    /// Gets the current circuit breaker state for monitoring purposes.
    /// </summary>
    public CircuitBreakerState CircuitBreakerState => _circuitBreaker.State;

    /// <summary>
    /// Gets the current failure count for monitoring purposes.
    /// </summary>
    public int FailureCount => _circuitBreaker.FailureCount;

    /// <summary>
    /// Gets the operation failure counts for detailed monitoring.
    /// </summary>
    public IReadOnlyDictionary<string, int> OperationFailureCounts => _circuitBreaker.OperationFailureCounts;

    /// <summary>
    /// Manually resets the circuit breaker to closed state.
    /// </summary>
    public void ResetCircuitBreaker()
    {
        _logger?.LogInformation("Manually resetting circuit breaker");
        _circuitBreaker.Reset();
    }
}