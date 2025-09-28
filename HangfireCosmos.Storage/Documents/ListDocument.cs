using Newtonsoft.Json;

namespace HangfireCosmos.Storage.Documents;

/// <summary>
/// Represents a list entry document in the Lists container.
/// </summary>
public class ListDocument : BaseDocument
{
    /// <summary>
    /// Initializes a new instance of the ListDocument class.
    /// </summary>
    public ListDocument()
    {
        DocumentType = "list";
    }

    /// <summary>
    /// Gets or sets the list key.
    /// </summary>
    [JsonProperty("key")]
    public string Key { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the index position in the list.
    /// </summary>
    [JsonProperty("index")]
    public long Index { get; set; }

    /// <summary>
    /// Gets or sets the value at this list position.
    /// </summary>
    [JsonProperty("value")]
    public string Value { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets when the list entry was created.
    /// </summary>
    [JsonProperty("createdAt")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets additional metadata for the list entry.
    /// </summary>
    [JsonProperty("metadata")]
    public Dictionary<string, string> Metadata { get; set; } = new();

    /// <summary>
    /// Sets the partition key based on the list key.
    /// </summary>
    /// <param name="key">The list key.</param>
    public void SetPartitionKey(string key)
    {
        PartitionKey = $"list:{key}";
    }
}