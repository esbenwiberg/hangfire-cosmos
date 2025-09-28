using Newtonsoft.Json;

namespace HangfireCosmos.Storage.Documents;

/// <summary>
/// Represents a hash entry document in the Hashes container.
/// </summary>
public class HashDocument : BaseDocument
{
    /// <summary>
    /// Initializes a new instance of the HashDocument class.
    /// </summary>
    public HashDocument()
    {
        DocumentType = "hash";
    }

    /// <summary>
    /// Gets or sets the hash key.
    /// </summary>
    [JsonProperty("key")]
    public string Key { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the field name within the hash.
    /// </summary>
    [JsonProperty("field")]
    public string Field { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the value for the hash field.
    /// </summary>
    [JsonProperty("value")]
    public string Value { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets when the hash entry was created.
    /// </summary>
    [JsonProperty("createdAt")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets when the hash entry was last updated.
    /// </summary>
    [JsonProperty("updatedAt")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Sets the partition key based on the hash key.
    /// </summary>
    /// <param name="key">The hash key.</param>
    public void SetPartitionKey(string key)
    {
        PartitionKey = $"hash:{key}";
    }
}