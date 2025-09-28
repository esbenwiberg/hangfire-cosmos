using Hangfire;
using Hangfire.Storage;
using Microsoft.Azure.Cosmos;
using HangfireCosmos.Storage.Repository;

namespace HangfireCosmos.Storage;

/// <summary>
/// Hangfire storage implementation for Azure Cosmos DB.
/// Provides a complete storage provider for Hangfire using Azure Cosmos DB as the backend.
/// </summary>
public class CosmosStorage : JobStorage
{
    private readonly CosmosClient _cosmosClient;
    private readonly CosmosStorageOptions _options;
    private readonly ICosmosDocumentRepository _documentRepository;

    /// <summary>
    /// Initializes a new instance of the CosmosStorage class.
    /// </summary>
    /// <param name="cosmosClient">The Cosmos DB client.</param>
    /// <param name="options">The storage configuration options.</param>
    public CosmosStorage(CosmosClient cosmosClient, CosmosStorageOptions options)
    {
        _cosmosClient = cosmosClient ?? throw new ArgumentNullException(nameof(cosmosClient));
        _options = options ?? throw new ArgumentNullException(nameof(options));
        
        // Validate options
        _options.Validate();
        
        // Initialize repository
        _documentRepository = new CosmosDocumentRepository(_cosmosClient, _options);
        
        // Initialize database and containers
        InitializeAsync().GetAwaiter().GetResult();
    }

    /// <summary>
    /// Initializes a new instance of the CosmosStorage class with connection string.
    /// </summary>
    /// <param name="connectionString">The Cosmos DB connection string.</param>
    /// <param name="options">The storage configuration options.</param>
    public CosmosStorage(string connectionString, CosmosStorageOptions? options = null)
        : this(new CosmosClient(connectionString), options ?? new CosmosStorageOptions())
    {
    }

    /// <summary>
    /// Gets the storage connection for job operations.
    /// </summary>
    /// <returns>A storage connection instance.</returns>
    public override IStorageConnection GetConnection()
    {
        return new Connection.CosmosStorageConnection(_documentRepository, _options);
    }

    /// <summary>
    /// Gets the monitoring API for dashboard functionality.
    /// </summary>
    /// <returns>A monitoring API instance.</returns>
    public override IMonitoringApi GetMonitoringApi()
    {
        return new Monitoring.CosmosMonitoringApi(_documentRepository, _options);
    }

    /// <summary>
    /// Gets the storage options.
    /// </summary>
    public CosmosStorageOptions Options => _options;

    /// <summary>
    /// Gets the document repository.
    /// </summary>
    public ICosmosDocumentRepository DocumentRepository => _documentRepository;

    /// <summary>
    /// Gets the Cosmos client.
    /// </summary>
    public CosmosClient CosmosClient => _cosmosClient;

    /// <summary>
    /// Initializes the database and containers asynchronously.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task representing the initialization operation.</returns>
    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        await _documentRepository.InitializeAsync(cancellationToken);
    }

    /// <summary>
    /// Returns a string representation of the storage provider.
    /// </summary>
    /// <returns>A string describing the storage provider.</returns>
    public override string ToString()
    {
        return $"Cosmos DB Storage (Database: {_options.DatabaseName})";
    }

    /// <summary>
    /// Disposes the storage provider and its resources.
    /// </summary>
    /// <param name="disposing">True if disposing managed resources.</param>
    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            // Dispose managed resources
            _cosmosClient?.Dispose();
        }
    }

    /// <summary>
    /// Disposes the storage provider.
    /// </summary>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
}