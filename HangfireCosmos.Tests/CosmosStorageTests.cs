using FluentAssertions;
using HangfireCosmos.Storage;
using Microsoft.Azure.Cosmos;
using Moq;
using Xunit;

namespace HangfireCosmos.Tests;

/// <summary>
/// Unit tests for the CosmosStorage class.
/// </summary>
public class CosmosStorageTests
{
    private readonly Mock<CosmosClient> _mockCosmosClient;
    private readonly CosmosStorageOptions _options;

    public CosmosStorageTests()
    {
        _mockCosmosClient = new Mock<CosmosClient>();
        _options = new CosmosStorageOptions
        {
            DatabaseName = "hangfire-test",
            JobsContainerName = "jobs-test"
        };
    }

    [Fact]
    public void Constructor_WithValidParameters_ShouldCreateInstance()
    {
        // Act
        var storage = new CosmosStorage(_mockCosmosClient.Object, _options);

        // Assert
        storage.Should().NotBeNull();
    }

    [Fact]
    public void Constructor_WithNullCosmosClient_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        var action = () => new CosmosStorage((CosmosClient)null!, _options);
        action.Should().Throw<ArgumentNullException>()
            .WithParameterName("cosmosClient");
    }

    [Fact]
    public void Constructor_WithNullOptions_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        var action = () => new CosmosStorage(_mockCosmosClient.Object, null!);
        action.Should().Throw<ArgumentNullException>()
            .WithParameterName("options");
    }

    [Fact]
    public void Constructor_WithConnectionString_ShouldCreateInstance()
    {
        // Use the Cosmos DB Emulator connection string with a valid key
        var connectionString = "AccountEndpoint=https://localhost:8081/;AccountKey=C2y6yDjf5/R+ob0N8A7Cgv30VRDJIWEHLM+4QDU5DE2nQ9nDuVTqobD4b8mGGyPMbIZnqyMsEcaGQy67XIw/Jw==";
        
        // Disable auto-initialization to avoid connecting to the emulator
        var testOptions = new CosmosStorageOptions
        {
            DatabaseName = "hangfire-test",
            JobsContainerName = "jobs-test",
            AutoCreateDatabase = false,
            AutoCreateContainers = false
        };
        
        // Act
        var storage = new CosmosStorage(connectionString, testOptions);

        // Assert
        storage.Should().NotBeNull();
    }

    [Fact]
    public void GetConnection_ShouldReturnCosmosStorageConnection()
    {
        // Arrange
        var storage = new CosmosStorage(_mockCosmosClient.Object, _options);

        // Act
        var connection = storage.GetConnection();

        // Assert
        connection.Should().NotBeNull();
        connection.Should().BeOfType<HangfireCosmos.Storage.Connection.CosmosStorageConnection>();
    }

    [Fact]
    public void GetMonitoringApi_ShouldReturnCosmosMonitoringApi()
    {
        // Arrange
        var storage = new CosmosStorage(_mockCosmosClient.Object, _options);

        // Act
        var monitoringApi = storage.GetMonitoringApi();

        // Assert
        monitoringApi.Should().NotBeNull();
        monitoringApi.Should().BeOfType<HangfireCosmos.Storage.Monitoring.CosmosMonitoringApi>();
    }
}