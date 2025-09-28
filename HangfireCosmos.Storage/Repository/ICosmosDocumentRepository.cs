using Microsoft.Azure.Cosmos;
using HangfireCosmos.Storage.Documents;

namespace HangfireCosmos.Storage.Repository;

/// <summary>
/// Interface for Cosmos DB document repository operations.
/// </summary>
public interface ICosmosDocumentRepository
{
    /// <summary>
    /// Gets a document by ID and partition key.
    /// </summary>
    /// <typeparam name="T">The document type.</typeparam>
    /// <param name="containerName">The container name.</param>
    /// <param name="id">The document ID.</param>
    /// <param name="partitionKey">The partition key.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The document or null if not found.</returns>
    Task<T?> GetDocumentAsync<T>(string containerName, string id, string partitionKey, CancellationToken cancellationToken = default) where T : BaseDocument;

    /// <summary>
    /// Creates a new document.
    /// </summary>
    /// <typeparam name="T">The document type.</typeparam>
    /// <param name="containerName">The container name.</param>
    /// <param name="document">The document to create.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The created document.</returns>
    Task<T> CreateDocumentAsync<T>(string containerName, T document, CancellationToken cancellationToken = default) where T : BaseDocument;

    /// <summary>
    /// Upserts a document (creates or updates).
    /// </summary>
    /// <typeparam name="T">The document type.</typeparam>
    /// <param name="containerName">The container name.</param>
    /// <param name="document">The document to upsert.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The upserted document.</returns>
    Task<T> UpsertDocumentAsync<T>(string containerName, T document, CancellationToken cancellationToken = default) where T : BaseDocument;

    /// <summary>
    /// Updates an existing document.
    /// </summary>
    /// <typeparam name="T">The document type.</typeparam>
    /// <param name="containerName">The container name.</param>
    /// <param name="document">The document to update.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The updated document.</returns>
    Task<T> UpdateDocumentAsync<T>(string containerName, T document, CancellationToken cancellationToken = default) where T : BaseDocument;

    /// <summary>
    /// Deletes a document by ID and partition key.
    /// </summary>
    /// <param name="containerName">The container name.</param>
    /// <param name="id">The document ID.</param>
    /// <param name="partitionKey">The partition key.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task representing the operation.</returns>
    Task DeleteDocumentAsync(string containerName, string id, string partitionKey, CancellationToken cancellationToken = default);

    /// <summary>
    /// Queries documents using a SQL query string.
    /// </summary>
    /// <typeparam name="T">The document type.</typeparam>
    /// <param name="containerName">The container name.</param>
    /// <param name="query">The SQL query string.</param>
    /// <param name="parameters">The query parameters.</param>
    /// <param name="partitionKey">The partition key (optional for cross-partition queries).</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The query results.</returns>
    Task<IEnumerable<T>> QueryDocumentsAsync<T>(string containerName, string query, object? parameters = null, string? partitionKey = null, CancellationToken cancellationToken = default) where T : BaseDocument;

    /// <summary>
    /// Queries documents using a QueryDefinition.
    /// </summary>
    /// <typeparam name="T">The document type.</typeparam>
    /// <param name="containerName">The container name.</param>
    /// <param name="queryDefinition">The query definition.</param>
    /// <param name="partitionKey">The partition key (optional for cross-partition queries).</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The query results.</returns>
    Task<IEnumerable<T>> QueryDocumentsAsync<T>(string containerName, QueryDefinition queryDefinition, string? partitionKey = null, CancellationToken cancellationToken = default) where T : BaseDocument;

    /// <summary>
    /// Queries documents with paging support.
    /// </summary>
    /// <typeparam name="T">The document type.</typeparam>
    /// <param name="containerName">The container name.</param>
    /// <param name="queryDefinition">The query definition.</param>
    /// <param name="continuationToken">The continuation token for paging.</param>
    /// <param name="maxItemCount">The maximum number of items to return.</param>
    /// <param name="partitionKey">The partition key (optional for cross-partition queries).</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The paged query results.</returns>
    Task<PagedResult<T>> QueryDocumentsPagedAsync<T>(string containerName, QueryDefinition queryDefinition, string? continuationToken = null, int? maxItemCount = null, string? partitionKey = null, CancellationToken cancellationToken = default) where T : BaseDocument;

    // Note: Batch operations will be implemented later when proper SDK support is available

    /// <summary>
    /// Gets the container for the specified name.
    /// </summary>
    /// <param name="containerName">The container name.</param>
    /// <returns>The container instance.</returns>
    Container GetContainer(string containerName);

    /// <summary>
    /// Initializes the database and containers if they don't exist.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task representing the operation.</returns>
    Task InitializeAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Represents a paged result from a query.
/// </summary>
/// <typeparam name="T">The document type.</typeparam>
public class PagedResult<T> where T : BaseDocument
{
    /// <summary>
    /// Gets or sets the items in this page.
    /// </summary>
    public IEnumerable<T> Items { get; set; } = Enumerable.Empty<T>();

    /// <summary>
    /// Gets or sets the continuation token for the next page.
    /// </summary>
    public string? ContinuationToken { get; set; }

    /// <summary>
    /// Gets or sets the request charge for this query.
    /// </summary>
    public double RequestCharge { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether there are more results.
    /// </summary>
    public bool HasMoreResults => !string.IsNullOrEmpty(ContinuationToken);
}