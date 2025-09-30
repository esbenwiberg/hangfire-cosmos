using Microsoft.Azure.Cosmos;

namespace HangfireCosmos.Storage.Authentication;

/// <summary>
/// Factory interface for creating CosmosClient instances with various authentication methods.
/// </summary>
public interface ICosmosClientFactory
{
    /// <summary>
    /// Creates a CosmosClient instance based on the provided options.
    /// </summary>
    /// <param name="options">The Cosmos storage options containing authentication configuration.</param>
    /// <returns>A configured CosmosClient instance.</returns>
    Task<CosmosClient> CreateClientAsync(CosmosStorageOptions options);
}