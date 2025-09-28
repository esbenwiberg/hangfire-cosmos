using FluentAssertions;
using HangfireCosmos.Storage.Documents;
using Newtonsoft.Json;
using Xunit;

namespace HangfireCosmos.Tests.Documents;

/// <summary>
/// Unit tests for the BaseDocument class.
/// </summary>
public class BaseDocumentTests
{
    /// <summary>
    /// Test implementation of BaseDocument for testing purposes.
    /// </summary>
    private class TestDocument : BaseDocument
    {
        public TestDocument()
        {
            DocumentType = "test";
        }

        [JsonProperty("testProperty")]
        public string TestProperty { get; set; } = string.Empty;
    }

    [Fact]
    public void Constructor_ShouldInitializePropertiesWithDefaults()
    {
        // Act
        var document = new TestDocument();

        // Assert
        document.Id.Should().Be(string.Empty);
        document.PartitionKey.Should().Be(string.Empty);
        document.DocumentType.Should().Be("test");
        document.Timestamp.Should().Be(0);
        document.ETag.Should().BeNull();
        document.ExpireAt.Should().BeNull();
    }

    [Fact]
    public void Id_ShouldSetAndGetCorrectly()
    {
        // Arrange
        var document = new TestDocument();
        const string expectedId = "test-id-123";

        // Act
        document.Id = expectedId;

        // Assert
        document.Id.Should().Be(expectedId);
    }

    [Fact]
    public void PartitionKey_ShouldSetAndGetCorrectly()
    {
        // Arrange
        var document = new TestDocument();
        const string expectedPartitionKey = "test-partition";

        // Act
        document.PartitionKey = expectedPartitionKey;

        // Assert
        document.PartitionKey.Should().Be(expectedPartitionKey);
    }

    [Fact]
    public void DocumentType_ShouldSetAndGetCorrectly()
    {
        // Arrange
        var document = new TestDocument();
        const string expectedDocumentType = "custom-type";

        // Act
        document.DocumentType = expectedDocumentType;

        // Assert
        document.DocumentType.Should().Be(expectedDocumentType);
    }

    [Fact]
    public void Timestamp_ShouldSetAndGetCorrectly()
    {
        // Arrange
        var document = new TestDocument();
        const long expectedTimestamp = 1640995200; // Unix timestamp

        // Act
        document.Timestamp = expectedTimestamp;

        // Assert
        document.Timestamp.Should().Be(expectedTimestamp);
    }

    [Fact]
    public void ETag_ShouldSetAndGetCorrectly()
    {
        // Arrange
        var document = new TestDocument();
        const string expectedETag = "\"0x8D9A1B2C3D4E5F6\"";

        // Act
        document.ETag = expectedETag;

        // Assert
        document.ETag.Should().Be(expectedETag);
    }

    [Fact]
    public void ExpireAt_ShouldSetAndGetCorrectly()
    {
        // Arrange
        var document = new TestDocument();
        var expectedExpireAt = DateTime.UtcNow.AddDays(7);

        // Act
        document.ExpireAt = expectedExpireAt;

        // Assert
        document.ExpireAt.Should().Be(expectedExpireAt);
    }

    [Fact]
    public void JsonSerialization_ShouldSerializeAllProperties()
    {
        // Arrange
        var document = new TestDocument
        {
            Id = "test-id",
            PartitionKey = "test-partition",
            DocumentType = "test-type",
            Timestamp = 1640995200,
            ETag = "\"test-etag\"",
            ExpireAt = new DateTime(2023, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            TestProperty = "test-value"
        };

        // Act
        var json = JsonConvert.SerializeObject(document);
        var deserializedDocument = JsonConvert.DeserializeObject<TestDocument>(json);

        // Assert
        deserializedDocument.Should().NotBeNull();
        deserializedDocument!.Id.Should().Be(document.Id);
        deserializedDocument.PartitionKey.Should().Be(document.PartitionKey);
        deserializedDocument.DocumentType.Should().Be(document.DocumentType);
        deserializedDocument.Timestamp.Should().Be(document.Timestamp);
        deserializedDocument.ETag.Should().Be(document.ETag);
        deserializedDocument.ExpireAt.Should().Be(document.ExpireAt);
        deserializedDocument.TestProperty.Should().Be(document.TestProperty);
    }

    [Fact]
    public void JsonSerialization_ShouldUseCorrectPropertyNames()
    {
        // Arrange
        var document = new TestDocument
        {
            Id = "test-id",
            PartitionKey = "test-partition",
            DocumentType = "test-type",
            Timestamp = 1640995200,
            ETag = "\"test-etag\"",
            ExpireAt = new DateTime(2023, 1, 1, 0, 0, 0, DateTimeKind.Utc)
        };

        // Act
        var json = JsonConvert.SerializeObject(document);

        // Assert
        json.Should().Contain("\"id\":");
        json.Should().Contain("\"partitionKey\":");
        json.Should().Contain("\"documentType\":");
        json.Should().Contain("\"_ts\":");
        json.Should().Contain("\"_etag\":");
        json.Should().Contain("\"expireAt\":");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Id_WithNullOrEmptyValue_ShouldSetCorrectly(string? value)
    {
        // Arrange
        var document = new TestDocument();

        // Act
        document.Id = value ?? string.Empty;

        // Assert
        document.Id.Should().Be(value ?? string.Empty);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void PartitionKey_WithNullOrEmptyValue_ShouldSetCorrectly(string? value)
    {
        // Arrange
        var document = new TestDocument();

        // Act
        document.PartitionKey = value ?? string.Empty;

        // Assert
        document.PartitionKey.Should().Be(value ?? string.Empty);
    }

    [Fact]
    public void ExpireAt_WithNullValue_ShouldSetCorrectly()
    {
        // Arrange
        var document = new TestDocument();

        // Act
        document.ExpireAt = null;

        // Assert
        document.ExpireAt.Should().BeNull();
    }

    [Fact]
    public void JsonDeserialization_WithMissingOptionalProperties_ShouldUseDefaults()
    {
        // Arrange
        const string json = """
        {
            "id": "test-id",
            "partitionKey": "test-partition",
            "documentType": "test-type"
        }
        """;

        // Act
        var document = JsonConvert.DeserializeObject<TestDocument>(json);

        // Assert
        document.Should().NotBeNull();
        document!.Id.Should().Be("test-id");
        document.PartitionKey.Should().Be("test-partition");
        document.DocumentType.Should().Be("test-type");
        document.Timestamp.Should().Be(0);
        document.ETag.Should().BeNull();
        document.ExpireAt.Should().BeNull();
    }

    [Fact]
    public void JsonDeserialization_WithAllProperties_ShouldDeserializeCorrectly()
    {
        // Arrange
        const string json = """
        {
            "id": "test-id",
            "partitionKey": "test-partition",
            "documentType": "test-type",
            "_ts": 1640995200,
            "_etag": "\"test-etag\"",
            "expireAt": "2023-01-01T00:00:00Z",
            "testProperty": "test-value"
        }
        """;

        // Act
        var document = JsonConvert.DeserializeObject<TestDocument>(json);

        // Assert
        document.Should().NotBeNull();
        document!.Id.Should().Be("test-id");
        document.PartitionKey.Should().Be("test-partition");
        document.DocumentType.Should().Be("test-type");
        document.Timestamp.Should().Be(1640995200);
        document.ETag.Should().Be("\"test-etag\"");
        document.ExpireAt.Should().Be(new DateTime(2023, 1, 1, 0, 0, 0, DateTimeKind.Utc));
        document.TestProperty.Should().Be("test-value");
    }
}