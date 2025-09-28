using Newtonsoft.Json;

namespace HangfireCosmos.Storage.Documents;

/// <summary>
/// Represents a counter document in the Counters container.
/// </summary>
public class CounterDocument : BaseDocument
{
    /// <summary>
    /// Initializes a new instance of the CounterDocument class.
    /// </summary>
    public CounterDocument()
    {
        DocumentType = "counter";
        PartitionKey = "counters";
    }

    /// <summary>
    /// Gets or sets the counter key.
    /// </summary>
    [JsonProperty("key")]
    public string Key { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the counter value.
    /// </summary>
    [JsonProperty("value")]
    public long Value { get; set; }

    /// <summary>
    /// Gets or sets when the counter was created.
    /// </summary>
    [JsonProperty("createdAt")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets when the counter was last updated.
    /// </summary>
    [JsonProperty("updatedAt")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets additional metadata for the counter.
    /// </summary>
    [JsonProperty("metadata")]
    public Dictionary<string, string> Metadata { get; set; } = new();
}