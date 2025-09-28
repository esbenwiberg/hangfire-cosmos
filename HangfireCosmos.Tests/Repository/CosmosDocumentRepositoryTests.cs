using FluentAssertions;
using HangfireCosmos.Storage;
using HangfireCosmos.Storage.Documents;
using HangfireCosmos.Storage.Repository;
using Microsoft.Azure.Cosmos;
using Moq;
using System.Net;
using Xunit;

namespace HangfireCosmos.Tests.Repository;

/// <summary>
/// Unit tests for the CosmosDocumentRepository class.
/// </summary>
public class CosmosDocumentRepositoryTests
{
    private readonly Mock<CosmosClient> _mockCosmosClient;
    private readonly Mock<Database> _mockDatabase;
    private readonly Mock<Container> _mockContainer;
    private readonly CosmosStorageOptions _options;
    private readonly CosmosDocumentRepository _repository;

    public CosmosDocumentRepositoryTests()
    {
        _mockCosmosClient = new Mock<CosmosClient>();
        _mockDatabase = new Mock<Database>();
        _mockContainer = new Mock<Container>();
        
        _options = new CosmosStorageOptions
        {
            DatabaseName = "test-hangfire",
            JobsContainerName = "test-jobs",
            DefaultThroughput = 400,
            Performance = new PerformanceOptions { QueryPageSize = 100 }
        };

        _mockCosmosClient.Setup(x => x.GetDatabase(_options.DatabaseName))
            .Returns(_mockDatabase.Object);

        _mockDatabase.Setup(x => x.GetContainer(It.IsAny<string>()))
            .Returns(_mockContainer.Object);

        _repository = new CosmosDocumentRepository(_mockCosmosClient.Object, _options);
    }

    [Fact]
    public void Constructor_WithNullCosmosClient_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        var action = () => new CosmosDocumentRepository(null!, _options);
        action.Should().Throw<ArgumentNullException>()
            .WithParameterName("cosmosClient");
    }

    [Fact]
    public void Constructor_WithNullOptions_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        var action = () => new CosmosDocumentRepository(_mockCosmosClient.Object, null!);
        action.Should().Throw<ArgumentNullException>()
            .WithParameterName("options");
    }

    [Fact]
    public void Constructor_WithValidParameters_ShouldCreateInstance()
    {
        // Act
        var repository = new CosmosDocumentRepository(_mockCosmosClient.Object, _options);

        // Assert
        repository.Should().NotBeNull();
    }

    [Fact]
    public async Task GetDocumentAsync_WithExistingDocument_ShouldReturnDocument()
    {
        // Arrange
        var testDocument = new JobDocument
        {
            Id = "job-123",
            PartitionKey = "jobs",
            JobId = "job-123"
        };

        var mockResponse = new Mock<ItemResponse<JobDocument>>();
        mockResponse.Setup(x => x.Resource).Returns(testDocument);

        _mockContainer.Setup(x => x.ReadItemAsync<JobDocument>(
                "job-123",
                new PartitionKey("jobs"),
                null,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockResponse.Object);

        // Act
        var result = await _repository.GetDocumentAsync<JobDocument>("jobs", "job-123", "jobs");

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be("job-123");
        result.JobId.Should().Be("job-123");
    }

    [Fact]
    public async Task GetDocumentAsync_WithNonExistentDocument_ShouldReturnNull()
    {
        // Arrange
        _mockContainer.Setup(x => x.ReadItemAsync<JobDocument>(
                "job-123",
                new PartitionKey("jobs"),
                null,
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new CosmosException("Not found", HttpStatusCode.NotFound, 0, "", 0));

        // Act
        var result = await _repository.GetDocumentAsync<JobDocument>("jobs", "job-123", "jobs");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetDocumentAsync_WithCosmosException_ShouldRethrow()
    {
        // Arrange
        _mockContainer.Setup(x => x.ReadItemAsync<JobDocument>(
                "job-123",
                new PartitionKey("jobs"),
                null,
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new CosmosException("Server error", HttpStatusCode.InternalServerError, 0, "", 0));

        // Act & Assert
        var action = async () => await _repository.GetDocumentAsync<JobDocument>("jobs", "job-123", "jobs");
        await action.Should().ThrowAsync<CosmosException>()
            .Where(ex => ex.StatusCode == HttpStatusCode.InternalServerError);
    }

    [Fact]
    public async Task CreateDocumentAsync_WithValidDocument_ShouldReturnCreatedDocument()
    {
        // Arrange
        var testDocument = new JobDocument
        {
            Id = "job-123",
            PartitionKey = "jobs",
            JobId = "job-123"
        };

        var mockResponse = new Mock<ItemResponse<JobDocument>>();
        mockResponse.Setup(x => x.Resource).Returns(testDocument);

        _mockContainer.Setup(x => x.CreateItemAsync(
                testDocument,
                new PartitionKey("jobs"),
                null,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockResponse.Object);

        // Act
        var result = await _repository.CreateDocumentAsync("jobs", testDocument);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be("job-123");
        result.JobId.Should().Be("job-123");
    }

    [Fact]
    public async Task UpsertDocumentAsync_WithValidDocument_ShouldReturnUpsertedDocument()
    {
        // Arrange
        var testDocument = new JobDocument
        {
            Id = "job-123",
            PartitionKey = "jobs",
            JobId = "job-123"
        };

        var mockResponse = new Mock<ItemResponse<JobDocument>>();
        mockResponse.Setup(x => x.Resource).Returns(testDocument);

        _mockContainer.Setup(x => x.UpsertItemAsync(
                testDocument,
                new PartitionKey("jobs"),
                null,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockResponse.Object);

        // Act
        var result = await _repository.UpsertDocumentAsync("jobs", testDocument);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be("job-123");
        result.JobId.Should().Be("job-123");
    }

    [Fact]
    public async Task UpdateDocumentAsync_WithValidDocument_ShouldReturnUpdatedDocument()
    {
        // Arrange
        var testDocument = new JobDocument
        {
            Id = "job-123",
            PartitionKey = "jobs",
            JobId = "job-123"
        };

        var mockResponse = new Mock<ItemResponse<JobDocument>>();
        mockResponse.Setup(x => x.Resource).Returns(testDocument);

        _mockContainer.Setup(x => x.ReplaceItemAsync(
                testDocument,
                "job-123",
                new PartitionKey("jobs"),
                null,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockResponse.Object);

        // Act
        var result = await _repository.UpdateDocumentAsync("jobs", testDocument);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be("job-123");
        result.JobId.Should().Be("job-123");
    }

    [Fact]
    public async Task DeleteDocumentAsync_WithExistingDocument_ShouldComplete()
    {
        // Arrange
        var mockResponse = new Mock<ItemResponse<BaseDocument>>();
        
        _mockContainer.Setup(x => x.DeleteItemAsync<BaseDocument>(
                "job-123",
                new PartitionKey("jobs"),
                null,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockResponse.Object);

        // Act & Assert
        var action = async () => await _repository.DeleteDocumentAsync("jobs", "job-123", "jobs");
        await action.Should().NotThrowAsync();
    }

    [Fact]
    public async Task DeleteDocumentAsync_WithNonExistentDocument_ShouldNotThrow()
    {
        // Arrange
        _mockContainer.Setup(x => x.DeleteItemAsync<BaseDocument>(
                "job-123",
                new PartitionKey("jobs"),
                null,
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new CosmosException("Not found", HttpStatusCode.NotFound, 0, "", 0));

        // Act & Assert
        var action = async () => await _repository.DeleteDocumentAsync("jobs", "job-123", "jobs");
        await action.Should().NotThrowAsync();
    }

    [Fact]
    public async Task QueryDocumentsAsync_WithStringQuery_ShouldReturnResults()
    {
        // Arrange
        var testDocuments = new List<JobDocument>
        {
            new() { Id = "job-1", JobId = "job-1" },
            new() { Id = "job-2", JobId = "job-2" }
        };

        var mockFeedResponse = new Mock<FeedResponse<JobDocument>>();
        mockFeedResponse.Setup(x => x.GetEnumerator()).Returns(testDocuments.GetEnumerator());

        var mockFeedIterator = new Mock<FeedIterator<JobDocument>>();
        mockFeedIterator.SetupSequence(x => x.HasMoreResults)
            .Returns(true)
            .Returns(false);
        mockFeedIterator.Setup(x => x.ReadNextAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockFeedResponse.Object);

        _mockContainer.Setup(x => x.GetItemQueryIterator<JobDocument>(
                It.IsAny<QueryDefinition>(),
                null,
                It.IsAny<QueryRequestOptions>()))
            .Returns(mockFeedIterator.Object);

        // Act
        var result = await _repository.QueryDocumentsAsync<JobDocument>("jobs", "SELECT * FROM c WHERE c.documentType = 'job'");

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(2);
        result.First().Id.Should().Be("job-1");
        result.Last().Id.Should().Be("job-2");
    }

    [Fact]
    public async Task QueryDocumentsAsync_WithParameters_ShouldAddParametersToQuery()
    {
        // Arrange
        var testDocuments = new List<JobDocument>
        {
            new() { Id = "job-1", JobId = "job-1", State = "Processing" }
        };

        var mockFeedResponse = new Mock<FeedResponse<JobDocument>>();
        mockFeedResponse.Setup(x => x.GetEnumerator()).Returns(testDocuments.GetEnumerator());

        var mockFeedIterator = new Mock<FeedIterator<JobDocument>>();
        mockFeedIterator.SetupSequence(x => x.HasMoreResults)
            .Returns(true)
            .Returns(false);
        mockFeedIterator.Setup(x => x.ReadNextAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockFeedResponse.Object);

        QueryDefinition? capturedQuery = null;
        _mockContainer.Setup(x => x.GetItemQueryIterator<JobDocument>(
                It.IsAny<QueryDefinition>(),
                null,
                It.IsAny<QueryRequestOptions>()))
            .Callback<QueryDefinition, string?, QueryRequestOptions?>((query, token, options) => capturedQuery = query)
            .Returns(mockFeedIterator.Object);

        var parameters = new { State = "Processing" };

        // Act
        var result = await _repository.QueryDocumentsAsync<JobDocument>("jobs", "SELECT * FROM c WHERE c.state = @State", parameters);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(1);
        capturedQuery.Should().NotBeNull();
    }

    [Fact]
    public async Task QueryDocumentsPagedAsync_WithResults_ShouldReturnPagedResult()
    {
        // Arrange
        var testDocuments = new List<JobDocument>
        {
            new() { Id = "job-1", JobId = "job-1" },
            new() { Id = "job-2", JobId = "job-2" }
        };

        var mockFeedResponse = new Mock<FeedResponse<JobDocument>>();
        mockFeedResponse.Setup(x => x.GetEnumerator()).Returns(testDocuments.GetEnumerator());
        mockFeedResponse.Setup(x => x.ContinuationToken).Returns("next-token");
        mockFeedResponse.Setup(x => x.RequestCharge).Returns(2.5);

        var mockFeedIterator = new Mock<FeedIterator<JobDocument>>();
        mockFeedIterator.Setup(x => x.HasMoreResults).Returns(true);
        mockFeedIterator.Setup(x => x.ReadNextAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockFeedResponse.Object);

        _mockContainer.Setup(x => x.GetItemQueryIterator<JobDocument>(
                It.IsAny<QueryDefinition>(),
                It.IsAny<string>(),
                It.IsAny<QueryRequestOptions>()))
            .Returns(mockFeedIterator.Object);

        var queryDefinition = new QueryDefinition("SELECT * FROM c");

        // Act
        var result = await _repository.QueryDocumentsPagedAsync<JobDocument>("jobs", queryDefinition);

        // Assert
        result.Should().NotBeNull();
        result.Items.Should().HaveCount(2);
        result.ContinuationToken.Should().Be("next-token");
        result.RequestCharge.Should().Be(2.5);
        result.HasMoreResults.Should().BeTrue();
    }

    [Fact]
    public async Task QueryDocumentsPagedAsync_WithNoResults_ShouldReturnEmptyPagedResult()
    {
        // Arrange
        var mockFeedIterator = new Mock<FeedIterator<JobDocument>>();
        mockFeedIterator.Setup(x => x.HasMoreResults).Returns(false);

        _mockContainer.Setup(x => x.GetItemQueryIterator<JobDocument>(
                It.IsAny<QueryDefinition>(),
                It.IsAny<string>(),
                It.IsAny<QueryRequestOptions>()))
            .Returns(mockFeedIterator.Object);

        var queryDefinition = new QueryDefinition("SELECT * FROM c");

        // Act
        var result = await _repository.QueryDocumentsPagedAsync<JobDocument>("jobs", queryDefinition);

        // Assert
        result.Should().NotBeNull();
        result.Items.Should().BeEmpty();
        result.ContinuationToken.Should().BeNull();
        result.RequestCharge.Should().Be(0);
        result.HasMoreResults.Should().BeFalse();
    }

    [Fact]
    public void GetContainer_ShouldReturnContainer()
    {
        // Act
        var container = _repository.GetContainer("jobs");

        // Assert
        container.Should().NotBeNull();
        container.Should().Be(_mockContainer.Object);
    }

    [Fact]
    public void GetContainer_CalledMultipleTimes_ShouldReturnSameInstance()
    {
        // Act
        var container1 = _repository.GetContainer("jobs");
        var container2 = _repository.GetContainer("jobs");

        // Assert
        container1.Should().BeSameAs(container2);
    }

    [Fact]
    public async Task InitializeAsync_WithAutoCreateEnabled_ShouldCreateDatabaseAndContainers()
    {
        // Arrange
        var options = new CosmosStorageOptions
        {
            DatabaseName = "test-db",
            AutoCreateDatabase = true,
            AutoCreateContainers = true,
            DefaultThroughput = 400
        };

        var mockDatabaseResponse = new Mock<DatabaseResponse>();
        mockDatabaseResponse.Setup(x => x.Database).Returns(_mockDatabase.Object);

        _mockCosmosClient.Setup(x => x.CreateDatabaseIfNotExistsAsync(
                "test-db",
                400,
                null,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockDatabaseResponse.Object);

        var mockContainerResponse = new Mock<ContainerResponse>();
        mockContainerResponse.Setup(x => x.Container).Returns(_mockContainer.Object);

        _mockDatabase.Setup(x => x.CreateContainerIfNotExistsAsync(
                It.IsAny<ContainerProperties>(),
                400,
                null,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockContainerResponse.Object);

        _mockCosmosClient.Setup(x => x.GetDatabase("test-db"))
            .Returns(_mockDatabase.Object);

        var repository = new CosmosDocumentRepository(_mockCosmosClient.Object, options);

        // Act
        await repository.InitializeAsync();

        // Assert
        _mockCosmosClient.Verify(x => x.CreateDatabaseIfNotExistsAsync(
            "test-db",
            400,
            null,
            It.IsAny<CancellationToken>()), Times.Once);

        _mockDatabase.Verify(x => x.CreateContainerIfNotExistsAsync(
            It.IsAny<ContainerProperties>(),
            400,
            null,
            It.IsAny<CancellationToken>()), Times.AtLeast(8)); // 8 containers should be created
    }

    [Fact]
    public async Task InitializeAsync_WithAutoCreateDisabled_ShouldNotCreateDatabaseOrContainers()
    {
        // Arrange
        var options = new CosmosStorageOptions
        {
            DatabaseName = "test-db",
            AutoCreateDatabase = false,
            AutoCreateContainers = false
        };

        var repository = new CosmosDocumentRepository(_mockCosmosClient.Object, options);

        // Act
        await repository.InitializeAsync();

        // Assert
        _mockCosmosClient.Verify(x => x.CreateDatabaseIfNotExistsAsync(
            It.IsAny<string>(),
            It.IsAny<int>(),
            It.IsAny<RequestOptions>(),
            It.IsAny<CancellationToken>()), Times.Never);

        _mockDatabase.Verify(x => x.CreateContainerIfNotExistsAsync(
            It.IsAny<ContainerProperties>(),
            It.IsAny<int>(),
            It.IsAny<RequestOptions>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task QueryDocumentsAsync_WithPartitionKey_ShouldSetPartitionKeyInOptions()
    {
        // Arrange
        var testDocuments = new List<JobDocument>
        {
            new() { Id = "job-1", JobId = "job-1" }
        };

        var mockFeedResponse = new Mock<FeedResponse<JobDocument>>();
        mockFeedResponse.Setup(x => x.GetEnumerator()).Returns(testDocuments.GetEnumerator());

        var mockFeedIterator = new Mock<FeedIterator<JobDocument>>();
        mockFeedIterator.SetupSequence(x => x.HasMoreResults)
            .Returns(true)
            .Returns(false);
        mockFeedIterator.Setup(x => x.ReadNextAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockFeedResponse.Object);

        QueryRequestOptions? capturedOptions = null;
        _mockContainer.Setup(x => x.GetItemQueryIterator<JobDocument>(
                It.IsAny<QueryDefinition>(),
                null,
                It.IsAny<QueryRequestOptions>()))
            .Callback<QueryDefinition, string?, QueryRequestOptions?>((query, token, options) => capturedOptions = options)
            .Returns(mockFeedIterator.Object);

        var queryDefinition = new QueryDefinition("SELECT * FROM c");

        // Act
        await _repository.QueryDocumentsAsync<JobDocument>("jobs", queryDefinition, "test-partition");

        // Assert
        capturedOptions.Should().NotBeNull();
        capturedOptions!.PartitionKey.Should().Be(new PartitionKey("test-partition"));
    }

    [Fact]
    public async Task QueryDocumentsPagedAsync_WithCustomMaxItemCount_ShouldUseCustomValue()
    {
        // Arrange
        var mockFeedIterator = new Mock<FeedIterator<JobDocument>>();
        mockFeedIterator.Setup(x => x.HasMoreResults).Returns(false);

        QueryRequestOptions? capturedOptions = null;
        _mockContainer.Setup(x => x.GetItemQueryIterator<JobDocument>(
                It.IsAny<QueryDefinition>(),
                It.IsAny<string>(),
                It.IsAny<QueryRequestOptions>()))
            .Callback<QueryDefinition, string?, QueryRequestOptions?>((query, token, options) => capturedOptions = options)
            .Returns(mockFeedIterator.Object);

        var queryDefinition = new QueryDefinition("SELECT * FROM c");

        // Act
        await _repository.QueryDocumentsPagedAsync<JobDocument>("jobs", queryDefinition, maxItemCount: 50);

        // Assert
        capturedOptions.Should().NotBeNull();
        capturedOptions!.MaxItemCount.Should().Be(50);
    }

    [Fact]
    public async Task QueryDocumentsPagedAsync_WithContinuationToken_ShouldPassToken()
    {
        // Arrange
        var mockFeedIterator = new Mock<FeedIterator<JobDocument>>();
        mockFeedIterator.Setup(x => x.HasMoreResults).Returns(false);

        string? capturedToken = null;
        _mockContainer.Setup(x => x.GetItemQueryIterator<JobDocument>(
                It.IsAny<QueryDefinition>(),
                It.IsAny<string>(),
                It.IsAny<QueryRequestOptions>()))
            .Callback<QueryDefinition, string?, QueryRequestOptions?>((query, token, options) => capturedToken = token)
            .Returns(mockFeedIterator.Object);

        var queryDefinition = new QueryDefinition("SELECT * FROM c");

        // Act
        await _repository.QueryDocumentsPagedAsync<JobDocument>("jobs", queryDefinition, "continuation-token");

        // Assert
        capturedToken.Should().Be("continuation-token");
    }
}

/// <summary>
/// Unit tests for the PagedResult class.
/// </summary>
public class PagedResultTests
{
    [Fact]
    public void Constructor_ShouldInitializeWithDefaults()
    {
        // Act
        var result = new PagedResult<JobDocument>();

        // Assert
        result.Items.Should().NotBeNull().And.BeEmpty();
        result.ContinuationToken.Should().BeNull();
        result.RequestCharge.Should().Be(0);
        result.HasMoreResults.Should().BeFalse();
    }

    [Fact]
    public void HasMoreResults_WithContinuationToken_ShouldReturnTrue()
    {
        // Arrange
        var result = new PagedResult<JobDocument>
        {
            ContinuationToken = "next-token"
        };

        // Act & Assert
        result.HasMoreResults.Should().BeTrue();
    }

    [Fact]
    public void HasMoreResults_WithNullContinuationToken_ShouldReturnFalse()
    {
        // Arrange
        var result = new PagedResult<JobDocument>
        {
            ContinuationToken = null
        };

        // Act & Assert
        result.HasMoreResults.Should().BeFalse();
    }

    [Fact]
    public void HasMoreResults_WithEmptyContinuationToken_ShouldReturnFalse()
    {
        // Arrange
        var result = new PagedResult<JobDocument>
        {
            ContinuationToken = string.Empty
        };

        // Act & Assert
        result.HasMoreResults.Should().BeFalse();
    }

    [Fact]
    public void Items_ShouldSetAndGetCorrectly()
    {
        // Arrange
        var result = new PagedResult<JobDocument>();
        var items = new List<JobDocument>
        {
            new() { Id = "job-1" },
            new() { Id = "job-2" }
        };

        // Act
        result.Items = items;

        // Assert
        result.Items.Should().BeEquivalentTo(items);
    }

    [Fact]
    public void RequestCharge_ShouldSetAndGetCorrectly()
    {
        // Arrange
        var result = new PagedResult<JobDocument>();
        const double expectedCharge = 2.5;

        // Act
        result.RequestCharge = expectedCharge;

        // Assert
        result.RequestCharge.Should().Be(expectedCharge);
    }
}