using Microsoft.Azure.Cosmos;
using Moq;
using Xunit;
using FluentAssertions;
using HangfireCosmos.Storage;
using HangfireCosmos.Storage.Repository;

namespace HangfireCosmos.Tests.Repository;

public class CosmosDocumentRepositoryContainerInitializationTests
{
    private readonly Mock<CosmosClient> _mockCosmosClient;
    private readonly Mock<Database> _mockDatabase;
    private readonly Mock<Container> _mockContainer;

    public CosmosDocumentRepositoryContainerInitializationTests()
    {
        _mockCosmosClient = new Mock<CosmosClient>();
        _mockDatabase = new Mock<Database>();
        _mockContainer = new Mock<Container>();

        _mockCosmosClient.Setup(x => x.GetDatabase(It.IsAny<string>()))
            .Returns(_mockDatabase.Object);

        _mockDatabase.Setup(x => x.GetContainer(It.IsAny<string>()))
            .Returns(_mockContainer.Object);
    }

    [Fact]
    public async Task InitializeAsync_WithDedicatedStrategy_ShouldCreateEightContainers()
    {
        // Arrange
        var options = new CosmosStorageOptions
        {
            DatabaseName = "test-db",
            CollectionStrategy = CollectionStrategy.Dedicated,
            AutoCreateDatabase = true,
            AutoCreateContainers = true,
            DefaultThroughput = 400
        };

        var mockDatabaseResponse = new Mock<DatabaseResponse>();
        mockDatabaseResponse.Setup(x => x.Database).Returns(_mockDatabase.Object);

        var mockContainerResponse = new Mock<ContainerResponse>();
        mockContainerResponse.Setup(x => x.Container).Returns(_mockContainer.Object);

        _mockCosmosClient.Setup(x => x.CreateDatabaseIfNotExistsAsync(
                "test-db",
                400,
                null,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockDatabaseResponse.Object);

        _mockDatabase.Setup(x => x.CreateContainerIfNotExistsAsync(
                It.IsAny<ContainerProperties>(),
                400,
                null,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockContainerResponse.Object);

        var repository = new CosmosDocumentRepository(_mockCosmosClient.Object, options);

        // Act
        await repository.InitializeAsync();

        // Assert
        _mockCosmosClient.Verify(x => x.CreateDatabaseIfNotExistsAsync(
            "test-db",
            400,
            null,
            It.IsAny<CancellationToken>()), Times.Once);

        // Verify all 8 containers are created for dedicated strategy
        _mockDatabase.Verify(x => x.CreateContainerIfNotExistsAsync(
            It.Is<ContainerProperties>(cp => cp.Id == "jobs"),
            400,
            null,
            It.IsAny<CancellationToken>()), Times.Once);

        _mockDatabase.Verify(x => x.CreateContainerIfNotExistsAsync(
            It.Is<ContainerProperties>(cp => cp.Id == "servers"),
            400,
            null,
            It.IsAny<CancellationToken>()), Times.Once);

        _mockDatabase.Verify(x => x.CreateContainerIfNotExistsAsync(
            It.Is<ContainerProperties>(cp => cp.Id == "locks"),
            400,
            null,
            It.IsAny<CancellationToken>()), Times.Once);

        _mockDatabase.Verify(x => x.CreateContainerIfNotExistsAsync(
            It.Is<ContainerProperties>(cp => cp.Id == "queues"),
            400,
            null,
            It.IsAny<CancellationToken>()), Times.Once);

        _mockDatabase.Verify(x => x.CreateContainerIfNotExistsAsync(
            It.Is<ContainerProperties>(cp => cp.Id == "sets"),
            400,
            null,
            It.IsAny<CancellationToken>()), Times.Once);

        _mockDatabase.Verify(x => x.CreateContainerIfNotExistsAsync(
            It.Is<ContainerProperties>(cp => cp.Id == "hashes"),
            400,
            null,
            It.IsAny<CancellationToken>()), Times.Once);

        _mockDatabase.Verify(x => x.CreateContainerIfNotExistsAsync(
            It.Is<ContainerProperties>(cp => cp.Id == "lists"),
            400,
            null,
            It.IsAny<CancellationToken>()), Times.Once);

        _mockDatabase.Verify(x => x.CreateContainerIfNotExistsAsync(
            It.Is<ContainerProperties>(cp => cp.Id == "counters"),
            400,
            null,
            It.IsAny<CancellationToken>()), Times.Once);

        // Verify exactly 8 containers were created
        _mockDatabase.Verify(x => x.CreateContainerIfNotExistsAsync(
            It.IsAny<ContainerProperties>(),
            400,
            null,
            It.IsAny<CancellationToken>()), Times.Exactly(8));
    }

    [Fact]
    public async Task InitializeAsync_WithConsolidatedStrategy_ShouldCreateThreeContainers()
    {
        // Arrange
        var options = new CosmosStorageOptions
        {
            DatabaseName = "test-db",
            CollectionStrategy = CollectionStrategy.Consolidated,
            AutoCreateDatabase = true,
            AutoCreateContainers = true,
            DefaultThroughput = 400,
            JobsContainerName = "jobs",
            MetadataContainerName = "metadata",
            CollectionsContainerName = "collections"
        };

        var mockDatabaseResponse = new Mock<DatabaseResponse>();
        mockDatabaseResponse.Setup(x => x.Database).Returns(_mockDatabase.Object);

        var mockContainerResponse = new Mock<ContainerResponse>();
        mockContainerResponse.Setup(x => x.Container).Returns(_mockContainer.Object);

        _mockCosmosClient.Setup(x => x.CreateDatabaseIfNotExistsAsync(
                "test-db",
                400,
                null,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockDatabaseResponse.Object);

        _mockDatabase.Setup(x => x.CreateContainerIfNotExistsAsync(
                It.IsAny<ContainerProperties>(),
                400,
                null,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockContainerResponse.Object);

        var repository = new CosmosDocumentRepository(_mockCosmosClient.Object, options);

        // Act
        await repository.InitializeAsync();

        // Assert
        _mockCosmosClient.Verify(x => x.CreateDatabaseIfNotExistsAsync(
            "test-db",
            400,
            null,
            It.IsAny<CancellationToken>()), Times.Once);

        // Verify only 3 containers are created for consolidated strategy
        _mockDatabase.Verify(x => x.CreateContainerIfNotExistsAsync(
            It.Is<ContainerProperties>(cp => cp.Id == "jobs"),
            400,
            null,
            It.IsAny<CancellationToken>()), Times.Once);

        _mockDatabase.Verify(x => x.CreateContainerIfNotExistsAsync(
            It.Is<ContainerProperties>(cp => cp.Id == "metadata"),
            400,
            null,
            It.IsAny<CancellationToken>()), Times.Once);

        _mockDatabase.Verify(x => x.CreateContainerIfNotExistsAsync(
            It.Is<ContainerProperties>(cp => cp.Id == "collections"),
            400,
            null,
            It.IsAny<CancellationToken>()), Times.Once);

        // Verify exactly 3 containers were created
        _mockDatabase.Verify(x => x.CreateContainerIfNotExistsAsync(
            It.IsAny<ContainerProperties>(),
            400,
            null,
            It.IsAny<CancellationToken>()), Times.Exactly(3));
    }

    [Fact]
    public async Task InitializeAsync_WithConsolidatedStrategy_ShouldSetCorrectIndexingPolicies()
    {
        // Arrange
        var options = new CosmosStorageOptions
        {
            DatabaseName = "test-db",
            CollectionStrategy = CollectionStrategy.Consolidated,
            AutoCreateDatabase = true,
            AutoCreateContainers = true
        };

        var mockDatabaseResponse = new Mock<DatabaseResponse>();
        mockDatabaseResponse.Setup(x => x.Database).Returns(_mockDatabase.Object);

        var mockContainerResponse = new Mock<ContainerResponse>();
        mockContainerResponse.Setup(x => x.Container).Returns(_mockContainer.Object);

        _mockCosmosClient.Setup(x => x.CreateDatabaseIfNotExistsAsync(
                It.IsAny<string>(),
                It.IsAny<int>(),
                It.IsAny<RequestOptions>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockDatabaseResponse.Object);

        _mockDatabase.Setup(x => x.CreateContainerIfNotExistsAsync(
                It.IsAny<ContainerProperties>(),
                It.IsAny<int>(),
                It.IsAny<RequestOptions>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockContainerResponse.Object);

        var repository = new CosmosDocumentRepository(_mockCosmosClient.Object, options);

        // Act
        await repository.InitializeAsync();

        // Assert - Verify metadata container has correct indexing policy
        _mockDatabase.Verify(x => x.CreateContainerIfNotExistsAsync(
            It.Is<ContainerProperties>(cp => 
                cp.Id == "metadata" && 
                cp.IndexingPolicy.IncludedPaths.Any(p => p.Path == "/documentType/?") &&
                cp.IndexingPolicy.IncludedPaths.Any(p => p.Path == "/serverId/?") &&
                cp.IndexingPolicy.IncludedPaths.Any(p => p.Path == "/resource/?") &&
                cp.IndexingPolicy.IncludedPaths.Any(p => p.Path == "/queueName/?")),
            It.IsAny<int>(),
            null,
            It.IsAny<CancellationToken>()), Times.Once);

        // Assert - Verify collections container has correct indexing policy
        _mockDatabase.Verify(x => x.CreateContainerIfNotExistsAsync(
            It.Is<ContainerProperties>(cp => 
                cp.Id == "collections" && 
                cp.IndexingPolicy.IncludedPaths.Any(p => p.Path == "/documentType/?") &&
                cp.IndexingPolicy.IncludedPaths.Any(p => p.Path == "/key/?") &&
                cp.IndexingPolicy.IncludedPaths.Any(p => p.Path == "/field/?") &&
                cp.IndexingPolicy.IncludedPaths.Any(p => p.Path == "/index/?")),
            It.IsAny<int>(),
            null,
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task InitializeAsync_WithAutoCreateDisabled_ShouldNotCreateContainers()
    {
        // Arrange
        var options = new CosmosStorageOptions
        {
            DatabaseName = "test-db",
            CollectionStrategy = CollectionStrategy.Consolidated,
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
    public async Task InitializeAsync_WithCustomContainerNames_ShouldUseCustomNames()
    {
        // Arrange
        var options = new CosmosStorageOptions
        {
            DatabaseName = "custom-db",
            CollectionStrategy = CollectionStrategy.Consolidated,
            AutoCreateDatabase = true,
            AutoCreateContainers = true,
            JobsContainerName = "custom-jobs",
            MetadataContainerName = "custom-metadata",
            CollectionsContainerName = "custom-collections"
        };

        var mockDatabaseResponse = new Mock<DatabaseResponse>();
        mockDatabaseResponse.Setup(x => x.Database).Returns(_mockDatabase.Object);

        var mockContainerResponse = new Mock<ContainerResponse>();
        mockContainerResponse.Setup(x => x.Container).Returns(_mockContainer.Object);

        _mockCosmosClient.Setup(x => x.CreateDatabaseIfNotExistsAsync(
                It.IsAny<string>(),
                It.IsAny<int>(),
                It.IsAny<RequestOptions>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockDatabaseResponse.Object);

        _mockDatabase.Setup(x => x.CreateContainerIfNotExistsAsync(
                It.IsAny<ContainerProperties>(),
                It.IsAny<int>(),
                It.IsAny<RequestOptions>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockContainerResponse.Object);

        var repository = new CosmosDocumentRepository(_mockCosmosClient.Object, options);

        // Act
        await repository.InitializeAsync();

        // Assert
        _mockDatabase.Verify(x => x.CreateContainerIfNotExistsAsync(
            It.Is<ContainerProperties>(cp => cp.Id == "custom-jobs"),
            It.IsAny<int>(),
            null,
            It.IsAny<CancellationToken>()), Times.Once);

        _mockDatabase.Verify(x => x.CreateContainerIfNotExistsAsync(
            It.Is<ContainerProperties>(cp => cp.Id == "custom-metadata"),
            It.IsAny<int>(),
            null,
            It.IsAny<CancellationToken>()), Times.Once);

        _mockDatabase.Verify(x => x.CreateContainerIfNotExistsAsync(
            It.Is<ContainerProperties>(cp => cp.Id == "custom-collections"),
            It.IsAny<int>(),
            null,
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task InitializeAsync_WithCustomTtlSettings_ShouldApplyCorrectTtl()
    {
        // Arrange
        var options = new CosmosStorageOptions
        {
            DatabaseName = "test-db",
            CollectionStrategy = CollectionStrategy.Consolidated,
            AutoCreateDatabase = true,
            AutoCreateContainers = true,
            TtlSettings = new TtlSettings
            {
                JobDocumentTtl = 3600, // 1 hour
                ServerDocumentTtl = 300, // 5 minutes
                LockDocumentTtl = 60, // 1 minute
                CounterDocumentTtl = 7200 // 2 hours
            }
        };

        var mockDatabaseResponse = new Mock<DatabaseResponse>();
        mockDatabaseResponse.Setup(x => x.Database).Returns(_mockDatabase.Object);

        var mockContainerResponse = new Mock<ContainerResponse>();
        mockContainerResponse.Setup(x => x.Container).Returns(_mockContainer.Object);

        _mockCosmosClient.Setup(x => x.CreateDatabaseIfNotExistsAsync(
                It.IsAny<string>(),
                It.IsAny<int>(),
                It.IsAny<RequestOptions>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockDatabaseResponse.Object);

        _mockDatabase.Setup(x => x.CreateContainerIfNotExistsAsync(
                It.IsAny<ContainerProperties>(),
                It.IsAny<int>(),
                It.IsAny<RequestOptions>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockContainerResponse.Object);

        var repository = new CosmosDocumentRepository(_mockCosmosClient.Object, options);

        // Act
        await repository.InitializeAsync();

        // Assert
        _mockDatabase.Verify(x => x.CreateContainerIfNotExistsAsync(
            It.Is<ContainerProperties>(cp => cp.Id == "jobs" && cp.DefaultTimeToLive == 3600),
            It.IsAny<int>(),
            null,
            It.IsAny<CancellationToken>()), Times.Once);

        _mockDatabase.Verify(x => x.CreateContainerIfNotExistsAsync(
            It.Is<ContainerProperties>(cp => cp.Id == "metadata" && cp.DefaultTimeToLive == 300),
            It.IsAny<int>(),
            null,
            It.IsAny<CancellationToken>()), Times.Once);

        _mockDatabase.Verify(x => x.CreateContainerIfNotExistsAsync(
            It.Is<ContainerProperties>(cp => cp.Id == "collections" && cp.DefaultTimeToLive == null),
            It.IsAny<int>(),
            null,
            It.IsAny<CancellationToken>()), Times.Once);
    }
}