using Newtonsoft.Json;

namespace HangfireCosmos.Storage.Documents;

/// <summary>
/// Base class for all Cosmos DB documents in the Hangfire storage provider.
/// </summary>
public abstract class BaseDocument
{
    /// <summary>
    /// Gets or sets the document ID.
    /// </summary>
    [JsonProperty("id")]
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the partition key for the document.
    /// </summary>
    [JsonProperty("partitionKey")]
    public string PartitionKey { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the document type identifier.
    /// </summary>
    [JsonProperty("documentType")]
    public string DocumentType { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the document timestamp (managed by Cosmos DB).
    /// </summary>
    [JsonProperty("_ts")]
    public long Timestamp { get; set; }

    /// <summary>
    /// Gets or sets the document ETag (managed by Cosmos DB).
    /// </summary>
    [JsonProperty("_etag")]
    public string? ETag { get; set; }

    /// <summary>
    /// Gets or sets the expiration time for the document (TTL).
    /// </summary>
    [JsonProperty("expireAt")]
    public DateTime? ExpireAt { get; set; }
}