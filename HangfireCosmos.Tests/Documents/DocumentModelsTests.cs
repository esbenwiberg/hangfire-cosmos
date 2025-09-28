using FluentAssertions;
using HangfireCosmos.Storage.Documents;
using Newtonsoft.Json;
using Xunit;

namespace HangfireCosmos.Tests.Documents;

/// <summary>
/// Unit tests for LockDocument class.
/// </summary>
public class LockDocumentTests
{
    [Fact]
    public void Constructor_ShouldInitializeWithDefaults()
    {
        // Act
        var lockDocument = new LockDocument();

        // Assert
        lockDocument.DocumentType.Should().Be("lock");
        lockDocument.PartitionKey.Should().Be("locks");
        lockDocument.Resource.Should().Be(string.Empty);
        lockDocument.Owner.Should().Be(string.Empty);
        lockDocument.AcquiredAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        lockDocument.Timeout.Should().Be(TimeSpan.Zero);
        lockDocument.Metadata.Should().NotBeNull().And.BeEmpty();
    }

    [Fact]
    public void Resource_ShouldSetAndGetCorrectly()
    {
        // Arrange
        var lockDocument = new LockDocument();
        const string expectedResource = "job:123";

        // Act
        lockDocument.Resource = expectedResource;

        // Assert
        lockDocument.Resource.Should().Be(expectedResource);
    }

    [Fact]
    public void Owner_ShouldSetAndGetCorrectly()
    {
        // Arrange
        var lockDocument = new LockDocument();
        const string expectedOwner = "server-123";

        // Act
        lockDocument.Owner = expectedOwner;

        // Assert
        lockDocument.Owner.Should().Be(expectedOwner);
    }

    [Fact]
    public void Timeout_ShouldSetAndGetCorrectly()
    {
        // Arrange
        var lockDocument = new LockDocument();
        var expectedTimeout = TimeSpan.FromMinutes(5);

        // Act
        lockDocument.Timeout = expectedTimeout;

        // Assert
        lockDocument.Timeout.Should().Be(expectedTimeout);
    }

    [Fact]
    public void JsonSerialization_ShouldSerializeAllProperties()
    {
        // Arrange
        var lockDocument = new LockDocument
        {
            Id = "lock-1",
            Resource = "job:123",
            Owner = "server-123",
            AcquiredAt = new DateTime(2023, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            Timeout = TimeSpan.FromMinutes(5),
            Metadata = new Dictionary<string, string> { { "Priority", "High" } }
        };

        // Act
        var json = JsonConvert.SerializeObject(lockDocument);
        var deserializedDocument = JsonConvert.DeserializeObject<LockDocument>(json);

        // Assert
        deserializedDocument.Should().NotBeNull();
        deserializedDocument!.Should().BeEquivalentTo(lockDocument);
    }
}

/// <summary>
/// Unit tests for QueueDocument class.
/// </summary>
public class QueueDocumentTests
{
    [Fact]
    public void Constructor_ShouldInitializeWithDefaults()
    {
        // Act
        var queueDocument = new QueueDocument();

        // Assert
        queueDocument.DocumentType.Should().Be("queue");
        queueDocument.PartitionKey.Should().Be("queues");
        queueDocument.QueueName.Should().Be(string.Empty);
        queueDocument.Length.Should().Be(0);
        queueDocument.Fetched.Should().Be(0);
        queueDocument.LastUpdated.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        queueDocument.Metadata.Should().NotBeNull().And.BeEmpty();
    }

    [Fact]
    public void QueueName_ShouldSetAndGetCorrectly()
    {
        // Arrange
        var queueDocument = new QueueDocument();
        const string expectedQueueName = "critical";

        // Act
        queueDocument.QueueName = expectedQueueName;

        // Assert
        queueDocument.QueueName.Should().Be(expectedQueueName);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(100)]
    [InlineData(1000)]
    public void Length_ShouldSetAndGetCorrectly(long length)
    {
        // Arrange
        var queueDocument = new QueueDocument();

        // Act
        queueDocument.Length = length;

        // Assert
        queueDocument.Length.Should().Be(length);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(5)]
    [InlineData(50)]
    public void Fetched_ShouldSetAndGetCorrectly(long fetched)
    {
        // Arrange
        var queueDocument = new QueueDocument();

        // Act
        queueDocument.Fetched = fetched;

        // Assert
        queueDocument.Fetched.Should().Be(fetched);
    }

    [Fact]
    public void JsonSerialization_ShouldSerializeAllProperties()
    {
        // Arrange
        var queueDocument = new QueueDocument
        {
            Id = "queue-1",
            QueueName = "critical",
            Length = 100,
            Fetched = 5,
            LastUpdated = new DateTime(2023, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            Metadata = new Dictionary<string, string> { { "Priority", "High" } }
        };

        // Act
        var json = JsonConvert.SerializeObject(queueDocument);
        var deserializedDocument = JsonConvert.DeserializeObject<QueueDocument>(json);

        // Assert
        deserializedDocument.Should().NotBeNull();
        deserializedDocument!.Should().BeEquivalentTo(queueDocument);
    }
}

/// <summary>
/// Unit tests for SetDocument class.
/// </summary>
public class SetDocumentTests
{
    [Fact]
    public void Constructor_ShouldInitializeWithDefaults()
    {
        // Act
        var setDocument = new SetDocument();

        // Assert
        setDocument.DocumentType.Should().Be("set");
        setDocument.Key.Should().Be(string.Empty);
        setDocument.Value.Should().Be(string.Empty);
        setDocument.Score.Should().Be(0);
        setDocument.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        setDocument.Metadata.Should().NotBeNull().And.BeEmpty();
    }

    [Fact]
    public void Key_ShouldSetAndGetCorrectly()
    {
        // Arrange
        var setDocument = new SetDocument();
        const string expectedKey = "scheduled";

        // Act
        setDocument.Key = expectedKey;

        // Assert
        setDocument.Key.Should().Be(expectedKey);
    }

    [Fact]
    public void Value_ShouldSetAndGetCorrectly()
    {
        // Arrange
        var setDocument = new SetDocument();
        const string expectedValue = "job:123";

        // Act
        setDocument.Value = expectedValue;

        // Assert
        setDocument.Value.Should().Be(expectedValue);
    }

    [Theory]
    [InlineData(0.0)]
    [InlineData(1.5)]
    [InlineData(-1.0)]
    [InlineData(1640995200.0)] // Unix timestamp
    public void Score_ShouldSetAndGetCorrectly(double score)
    {
        // Arrange
        var setDocument = new SetDocument();

        // Act
        setDocument.Score = score;

        // Assert
        setDocument.Score.Should().Be(score);
    }

    [Fact]
    public void SetPartitionKey_ShouldSetCorrectPartitionKey()
    {
        // Arrange
        var setDocument = new SetDocument();
        const string key = "scheduled";

        // Act
        setDocument.SetPartitionKey(key);

        // Assert
        setDocument.PartitionKey.Should().Be("set:scheduled");
    }

    [Fact]
    public void JsonSerialization_ShouldSerializeAllProperties()
    {
        // Arrange
        var setDocument = new SetDocument
        {
            Id = "set-1",
            Key = "scheduled",
            Value = "job:123",
            Score = 1640995200.0,
            CreatedAt = new DateTime(2023, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            Metadata = new Dictionary<string, string> { { "Type", "Scheduled" } }
        };
        setDocument.SetPartitionKey(setDocument.Key);

        // Act
        var json = JsonConvert.SerializeObject(setDocument);
        var deserializedDocument = JsonConvert.DeserializeObject<SetDocument>(json);

        // Assert
        deserializedDocument.Should().NotBeNull();
        deserializedDocument!.Should().BeEquivalentTo(setDocument);
    }
}

/// <summary>
/// Unit tests for HashDocument class.
/// </summary>
public class HashDocumentTests
{
    [Fact]
    public void Constructor_ShouldInitializeWithDefaults()
    {
        // Act
        var hashDocument = new HashDocument();

        // Assert
        hashDocument.DocumentType.Should().Be("hash");
        hashDocument.Key.Should().Be(string.Empty);
        hashDocument.Field.Should().Be(string.Empty);
        hashDocument.Value.Should().Be(string.Empty);
        hashDocument.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        hashDocument.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void Key_ShouldSetAndGetCorrectly()
    {
        // Arrange
        var hashDocument = new HashDocument();
        const string expectedKey = "job:123";

        // Act
        hashDocument.Key = expectedKey;

        // Assert
        hashDocument.Key.Should().Be(expectedKey);
    }

    [Fact]
    public void Field_ShouldSetAndGetCorrectly()
    {
        // Arrange
        var hashDocument = new HashDocument();
        const string expectedField = "State";

        // Act
        hashDocument.Field = expectedField;

        // Assert
        hashDocument.Field.Should().Be(expectedField);
    }

    [Fact]
    public void Value_ShouldSetAndGetCorrectly()
    {
        // Arrange
        var hashDocument = new HashDocument();
        const string expectedValue = "Processing";

        // Act
        hashDocument.Value = expectedValue;

        // Assert
        hashDocument.Value.Should().Be(expectedValue);
    }

    [Fact]
    public void SetPartitionKey_ShouldSetCorrectPartitionKey()
    {
        // Arrange
        var hashDocument = new HashDocument();
        const string key = "job:123";

        // Act
        hashDocument.SetPartitionKey(key);

        // Assert
        hashDocument.PartitionKey.Should().Be("hash:job:123");
    }

    [Fact]
    public void JsonSerialization_ShouldSerializeAllProperties()
    {
        // Arrange
        var hashDocument = new HashDocument
        {
            Id = "hash-1",
            Key = "job:123",
            Field = "State",
            Value = "Processing",
            CreatedAt = new DateTime(2023, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            UpdatedAt = new DateTime(2023, 1, 1, 12, 0, 0, DateTimeKind.Utc)
        };
        hashDocument.SetPartitionKey(hashDocument.Key);

        // Act
        var json = JsonConvert.SerializeObject(hashDocument);
        var deserializedDocument = JsonConvert.DeserializeObject<HashDocument>(json);

        // Assert
        deserializedDocument.Should().NotBeNull();
        deserializedDocument!.Should().BeEquivalentTo(hashDocument);
    }
}

/// <summary>
/// Unit tests for ListDocument class.
/// </summary>
public class ListDocumentTests
{
    [Fact]
    public void Constructor_ShouldInitializeWithDefaults()
    {
        // Act
        var listDocument = new ListDocument();

        // Assert
        listDocument.DocumentType.Should().Be("list");
        listDocument.Key.Should().Be(string.Empty);
        listDocument.Index.Should().Be(0);
        listDocument.Value.Should().Be(string.Empty);
        listDocument.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        listDocument.Metadata.Should().NotBeNull().And.BeEmpty();
    }

    [Fact]
    public void Key_ShouldSetAndGetCorrectly()
    {
        // Arrange
        var listDocument = new ListDocument();
        const string expectedKey = "queue:default";

        // Act
        listDocument.Key = expectedKey;

        // Assert
        listDocument.Key.Should().Be(expectedKey);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(100)]
    [InlineData(-1)] // For right push operations
    public void Index_ShouldSetAndGetCorrectly(long index)
    {
        // Arrange
        var listDocument = new ListDocument();

        // Act
        listDocument.Index = index;

        // Assert
        listDocument.Index.Should().Be(index);
    }

    [Fact]
    public void Value_ShouldSetAndGetCorrectly()
    {
        // Arrange
        var listDocument = new ListDocument();
        const string expectedValue = "job:123";

        // Act
        listDocument.Value = expectedValue;

        // Assert
        listDocument.Value.Should().Be(expectedValue);
    }

    [Fact]
    public void SetPartitionKey_ShouldSetCorrectPartitionKey()
    {
        // Arrange
        var listDocument = new ListDocument();
        const string key = "queue:default";

        // Act
        listDocument.SetPartitionKey(key);

        // Assert
        listDocument.PartitionKey.Should().Be("list:queue:default");
    }

    [Fact]
    public void JsonSerialization_ShouldSerializeAllProperties()
    {
        // Arrange
        var listDocument = new ListDocument
        {
            Id = "list-1",
            Key = "queue:default",
            Index = 0,
            Value = "job:123",
            CreatedAt = new DateTime(2023, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            Metadata = new Dictionary<string, string> { { "Priority", "Normal" } }
        };
        listDocument.SetPartitionKey(listDocument.Key);

        // Act
        var json = JsonConvert.SerializeObject(listDocument);
        var deserializedDocument = JsonConvert.DeserializeObject<ListDocument>(json);

        // Assert
        deserializedDocument.Should().NotBeNull();
        deserializedDocument!.Should().BeEquivalentTo(listDocument);
    }
}

/// <summary>
/// Unit tests for CounterDocument class.
/// </summary>
public class CounterDocumentTests
{
    [Fact]
    public void Constructor_ShouldInitializeWithDefaults()
    {
        // Act
        var counterDocument = new CounterDocument();

        // Assert
        counterDocument.DocumentType.Should().Be("counter");
        counterDocument.PartitionKey.Should().Be("counters");
        counterDocument.Key.Should().Be(string.Empty);
        counterDocument.Value.Should().Be(0);
        counterDocument.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        counterDocument.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        counterDocument.Metadata.Should().NotBeNull().And.BeEmpty();
    }

    [Fact]
    public void Key_ShouldSetAndGetCorrectly()
    {
        // Arrange
        var counterDocument = new CounterDocument();
        const string expectedKey = "stats:succeeded";

        // Act
        counterDocument.Key = expectedKey;

        // Assert
        counterDocument.Key.Should().Be(expectedKey);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(100)]
    [InlineData(-5)]
    [InlineData(long.MaxValue)]
    [InlineData(long.MinValue)]
    public void Value_ShouldSetAndGetCorrectly(long value)
    {
        // Arrange
        var counterDocument = new CounterDocument();

        // Act
        counterDocument.Value = value;

        // Assert
        counterDocument.Value.Should().Be(value);
    }

    [Fact]
    public void CreatedAt_ShouldSetAndGetCorrectly()
    {
        // Arrange
        var counterDocument = new CounterDocument();
        var expectedCreatedAt = new DateTime(2023, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        // Act
        counterDocument.CreatedAt = expectedCreatedAt;

        // Assert
        counterDocument.CreatedAt.Should().Be(expectedCreatedAt);
    }

    [Fact]
    public void UpdatedAt_ShouldSetAndGetCorrectly()
    {
        // Arrange
        var counterDocument = new CounterDocument();
        var expectedUpdatedAt = new DateTime(2023, 1, 1, 12, 0, 0, DateTimeKind.Utc);

        // Act
        counterDocument.UpdatedAt = expectedUpdatedAt;

        // Assert
        counterDocument.UpdatedAt.Should().Be(expectedUpdatedAt);
    }

    [Fact]
    public void Metadata_ShouldSetAndGetCorrectly()
    {
        // Arrange
        var counterDocument = new CounterDocument();
        var expectedMetadata = new Dictionary<string, string>
        {
            { "Type", "JobStatistic" },
            { "Category", "Success" }
        };

        // Act
        counterDocument.Metadata = expectedMetadata;

        // Assert
        counterDocument.Metadata.Should().BeEquivalentTo(expectedMetadata);
    }

    [Fact]
    public void JsonSerialization_ShouldSerializeAllProperties()
    {
        // Arrange
        var counterDocument = new CounterDocument
        {
            Id = "counter-1",
            Key = "stats:succeeded",
            Value = 100,
            CreatedAt = new DateTime(2023, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            UpdatedAt = new DateTime(2023, 1, 1, 12, 0, 0, DateTimeKind.Utc),
            Metadata = new Dictionary<string, string> { { "Type", "JobStatistic" } }
        };

        // Act
        var json = JsonConvert.SerializeObject(counterDocument);
        var deserializedDocument = JsonConvert.DeserializeObject<CounterDocument>(json);

        // Assert
        deserializedDocument.Should().NotBeNull();
        deserializedDocument!.Should().BeEquivalentTo(counterDocument);
    }

    [Fact]
    public void JsonSerialization_ShouldUseCorrectPropertyNames()
    {
        // Arrange
        var counterDocument = new CounterDocument
        {
            Key = "stats:succeeded",
            Value = 100
        };

        // Act
        var json = JsonConvert.SerializeObject(counterDocument);

        // Assert
        json.Should().Contain("\"key\":");
        json.Should().Contain("\"value\":");
        json.Should().Contain("\"createdAt\":");
        json.Should().Contain("\"updatedAt\":");
        json.Should().Contain("\"metadata\":");
    }

    [Fact]
    public void PartitionKey_ShouldBeSetToCountersInConstructor()
    {
        // Act
        var counterDocument = new CounterDocument();

        // Assert
        counterDocument.PartitionKey.Should().Be("counters");
    }
}