using Newtonsoft.Json;

namespace HangfireCosmos.Storage.Documents;

/// <summary>
/// Represents a set entry document in the Sets container.
/// </summary>
public class SetDocument : BaseDocument
{
    /// <summary>
    /// Initializes a new instance of the SetDocument class.
    /// </summary>
    public SetDocument()
    {
        DocumentType = "set";
    }

    /// <summary>
    /// Gets or sets the set key.
    /// </summary>
    [JsonProperty("key")]
    public string Key { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the value in the set.
    /// </summary>
    [JsonProperty("value")]
    public string Value { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the score for the set entry (used for sorted sets).
    /// </summary>
    [JsonProperty("score")]
    public double Score { get; set; }

    /// <summary>
    /// Gets or sets when the set entry was created.
    /// </summary>
    [JsonProperty("createdAt")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets additional metadata for the set entry.
    /// </summary>
    [JsonProperty("metadata")]
    public Dictionary<string, string> Metadata { get; set; } = new();

    /// <summary>
    /// Sets the partition key based on the set key.
    /// </summary>
    /// <param name="key">The set key.</param>
    public void SetPartitionKey(string key)
    {
        PartitionKey = $"set:{key}";
    }
}