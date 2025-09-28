using Newtonsoft.Json;

namespace HangfireCosmos.Storage.Documents;

/// <summary>
/// Represents a queue metadata document in the Queues container.
/// </summary>
public class QueueDocument : BaseDocument
{
    /// <summary>
    /// Initializes a new instance of the QueueDocument class.
    /// </summary>
    public QueueDocument()
    {
        DocumentType = "queue";
        PartitionKey = "queues";
    }

    /// <summary>
    /// Gets or sets the queue name.
    /// </summary>
    [JsonProperty("queueName")]
    public string QueueName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the current length of the queue.
    /// </summary>
    [JsonProperty("length")]
    public long Length { get; set; }

    /// <summary>
    /// Gets or sets the number of fetched jobs in the queue.
    /// </summary>
    [JsonProperty("fetched")]
    public long Fetched { get; set; }

    /// <summary>
    /// Gets or sets when the queue statistics were last updated.
    /// </summary>
    [JsonProperty("lastUpdated")]
    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets additional queue metadata.
    /// </summary>
    [JsonProperty("metadata")]
    public Dictionary<string, string> Metadata { get; set; } = new();
}