# Hangfire Cosmos DB Storage Provider

A high-performance, cost-effective Hangfire storage provider implementation using Azure Cosmos DB as the backend storage system. This project provides a complete solution for running Hangfire background jobs with Azure Cosmos DB, featuring configurable collection strategies, comprehensive monitoring, and extensive testing capabilities.

## 🚀 Features

### Core Storage Provider
- **Complete Hangfire Integration**: Full implementation of [`JobStorage`](HangfireCosmos.Storage/CosmosStorage.cs), [`IStorageConnection`](HangfireCosmos.Storage/Connection/CosmosStorageConnection.cs), and [`IMonitoringApi`](HangfireCosmos.Storage/Monitoring/CosmosMonitoringApi.cs)
- **Flexible Collection Strategies**: Choose between dedicated containers (performance-optimized) or consolidated containers (cost-optimized)
- **Distributed Locking**: Robust distributed locking mechanism using Cosmos DB
- **Automatic TTL Management**: Configurable time-to-live for different document types
- **Retry Policies**: Built-in retry logic with exponential backoff for transient failures
- **Connection Pooling**: Efficient connection management and resource utilization

### Performance & Reliability
- **Optimized Partitioning**: Smart partition key design for optimal performance
- **Batch Operations**: Efficient batch processing for related operations
- **Query Optimization**: Optimized queries to minimize RU consumption
- **Error Handling**: Comprehensive error handling with circuit breaker patterns
- **Health Monitoring**: Built-in health checks for Cosmos DB connectivity

### Configuration & Extensibility
- **Flexible Configuration**: Environment-specific configuration with validation
- **Dependency Injection**: Full DI container integration
- **Extensible Architecture**: Pluggable components and interfaces
- **Multiple Consistency Levels**: Support for different Cosmos DB consistency models

## 📁 Project Structure

```
HangfireCosmos/
├── HangfireCosmos.Storage/          # Core storage provider implementation
│   ├── Connection/                  # Storage connection and transaction handling
│   ├── Documents/                   # Cosmos DB document models
│   ├── Extensions/                  # Configuration and DI extensions
│   ├── Monitoring/                  # Monitoring API implementation
│   └── Repository/                  # Data access layer
├── HangfireCosmos.Tests/           # Comprehensive test suite
│   ├── Configuration/              # Configuration and options tests
│   ├── Connection/                 # Connection and transaction tests
│   ├── Documents/                  # Document model tests
│   └── Repository/                 # Repository layer tests
├── HangfireCosmos.Web/             # Web application for testing and monitoring
│   ├── Controllers/                # API controllers for job management
│   ├── Services/                   # Job testing services
│   └── wwwroot/                    # Static web assets
└── Documentation/
    ├── HangfireCosmos-Architecture.md    # Detailed architecture documentation
    └── COLLECTION_STRATEGY_IMPLEMENTATION.md  # Collection strategy guide
```

## 🛠️ Technology Stack

- **.NET 9.0**: Latest .NET framework with modern C# features
- **Hangfire 1.8.21**: Background job processing framework
- **Azure Cosmos DB SDK 3.53.1**: Latest Cosmos DB client library
- **xUnit & FluentAssertions**: Comprehensive testing framework
- **Moq**: Mocking framework for unit tests
- **ASP.NET Core**: Web application framework for testing interface

## 🚀 Quick Start

### Prerequisites

- .NET 9.0 SDK
- Azure Cosmos DB account or Azure Cosmos DB Emulator
- Visual Studio 2022 or VS Code (optional)

### Installation

1. **Clone the repository**:
   ```bash
   git clone <repository-url>
   cd hangfire-cosmos
   ```

2. **Build the solution**:
   ```bash
   dotnet build
   ```

3. **Run tests**:
   ```bash
   dotnet test
   ```

### Basic Usage

#### 1. Install the Package
```bash
dotnet add package HangfireCosmos.Storage
```

#### 2. Configure in Startup/Program.cs
```csharp
using HangfireCosmos.Storage.Extensions;

var builder = WebApplication.CreateBuilder(args);

// Add Hangfire with Cosmos DB storage
builder.Services.AddHangfire(config =>
{
    config.UseCosmosStorage(
        connectionString: "your-cosmos-connection-string",
        options =>
        {
            options.DatabaseName = "hangfire";
            options.CollectionStrategy = CollectionStrategy.Dedicated; // or Consolidated
            options.DefaultThroughput = 400;
            options.AutoCreateDatabase = true;
            options.AutoCreateContainers = true;
        });
});

builder.Services.AddHangfireServer();

var app = builder.Build();

// Add Hangfire dashboard
app.UseHangfireDashboard();

app.Run();
```

#### 3. Enqueue Jobs
```csharp
// Fire-and-forget job
BackgroundJob.Enqueue(() => Console.WriteLine("Hello, Hangfire with Cosmos DB!"));

// Delayed job
BackgroundJob.Schedule(() => ProcessOrder(orderId), TimeSpan.FromMinutes(5));

// Recurring job
RecurringJob.AddOrUpdate("daily-report", () => GenerateReport(), Cron.Daily);
```

## ⚙️ Configuration

### Collection Strategies

Choose between two collection strategies based on your needs:

#### Dedicated Strategy (Performance-Optimized)
```json
{
  "HangfireCosmosOptions": {
    "CollectionStrategy": "Dedicated",
    "DatabaseName": "hangfire-prod",
    "DefaultThroughput": 1000,
    "JobsContainerName": "jobs",
    "ServersContainerName": "servers",
    "LocksContainerName": "locks",
    "QueuesContainerName": "queues",
    "SetsContainerName": "sets",
    "HashesContainerName": "hashes",
    "ListsContainerName": "lists",
    "CountersContainerName": "counters"
  }
}
```

#### Consolidated Strategy (Cost-Optimized)
```json
{
  "HangfireCosmosOptions": {
    "CollectionStrategy": "Consolidated",
    "DatabaseName": "hangfire-dev",
    "DefaultThroughput": 400,
    "JobsContainerName": "jobs",
    "MetadataContainerName": "metadata",
    "CollectionsContainerName": "collections"
  }
}
```

#### Shared Throughput (Maximum Cost Savings)
```json
{
  "HangfireCosmosOptions": {
    "CollectionStrategy": "Consolidated",
    "DatabaseName": "hangfire-shared",
    "DefaultThroughput": 1000,
    "UseSharedThroughput": true,
    "JobsContainerName": "jobs",
    "MetadataContainerName": "metadata",
    "CollectionsContainerName": "collections"
  }
}
```

#### Shared Throughput with Dedicated Strategy
```json
{
  "HangfireCosmosOptions": {
    "CollectionStrategy": "Dedicated",
    "DatabaseName": "hangfire-shared-dedicated",
    "DefaultThroughput": 2000,
    "UseSharedThroughput": true,
    "JobsContainerName": "jobs",
    "ServersContainerName": "servers",
    "LocksContainerName": "locks",
    "QueuesContainerName": "queues",
    "SetsContainerName": "sets",
    "HashesContainerName": "hashes",
    "ListsContainerName": "lists",
    "CountersContainerName": "counters"
  }
}
```

### Advanced Configuration Options

```csharp
services.AddHangfire(config =>
{
    config.UseCosmosStorage(cosmosClient, options =>
    {
        // Database settings
        options.DatabaseName = "hangfire";
        options.CollectionStrategy = CollectionStrategy.Dedicated;
        options.DefaultThroughput = 400;
        options.UseSharedThroughput = true;  // Enable shared throughput for cost savings
        
        // Auto-creation settings
        options.AutoCreateDatabase = true;
        options.AutoCreateContainers = true;
        
        // TTL settings
        options.TtlSettings = new TtlSettings
        {
            JobDocumentTtl = 86400,      // 24 hours
            ServerDocumentTtl = 300,     // 5 minutes
            LockDocumentTtl = 60,        // 1 minute
            CounterDocumentTtl = 3600    // 1 hour
        };
        
        // Performance settings
        options.Performance = new PerformanceOptions
        {
            QueryPageSize = 100,
            MaxRetryAttempts = 3,
            RetryDelay = TimeSpan.FromMilliseconds(500)
        };
        
        // Timeout settings
        options.DefaultJobExpiration = TimeSpan.FromDays(30);
        options.ServerTimeout = TimeSpan.FromMinutes(10);
        options.LockTimeout = TimeSpan.FromMinutes(2);
    });
});
```

## 🧪 Testing & Development

### Running the Test Web Application

The project includes a comprehensive web application for testing and monitoring:

```bash
cd HangfireCosmos.Web
dotnet run
```

Access the application at:
- **Main UI**: http://localhost:5000
- **Hangfire Dashboard**: http://localhost:5000/hangfire
- **API Documentation**: http://localhost:5000/swagger
- **Health Checks**: http://localhost:5000/health

### Test Coverage

The project includes extensive test coverage:
- **Unit Tests**: Core functionality and business logic
- **Integration Tests**: Cosmos DB integration scenarios
- **Configuration Tests**: Options validation and container resolution
- **Performance Tests**: Load testing and benchmarking

Run all tests:
```bash
dotnet test --collect:"XPlat Code Coverage"
```

## 📊 Monitoring & Observability

### Health Checks
Built-in health checks for:
- Cosmos DB connectivity
- Database and container existence
- Hangfire server status
- Job processing metrics

### Logging
Structured logging with:
- Request correlation IDs
- Performance metrics
- Error tracking
- Debug information

### Metrics
Application metrics including:
- Job processing rates
- RU consumption
- Error rates
- Performance counters

## 🏗️ Architecture

The storage provider follows a clean architecture pattern with clear separation of concerns:

- **Storage Layer**: [`CosmosStorage`](HangfireCosmos.Storage/CosmosStorage.cs) - Main entry point implementing Hangfire's [`JobStorage`](HangfireCosmos.Storage/CosmosStorage.cs)
- **Connection Layer**: [`CosmosStorageConnection`](HangfireCosmos.Storage/Connection/CosmosStorageConnection.cs) - Implements [`IStorageConnection`](HangfireCosmos.Storage/Connection/CosmosStorageConnection.cs) for job operations
- **Repository Layer**: [`CosmosDocumentRepository`](HangfireCosmos.Storage/Repository/CosmosDocumentRepository.cs) - Data access abstraction
- **Document Models**: Strongly-typed document models for different job data types
- **Monitoring Layer**: [`CosmosMonitoringApi`](HangfireCosmos.Storage/Monitoring/CosmosMonitoringApi.cs) - Dashboard and statistics

For detailed architecture information, see [`HangfireCosmos-Architecture.md`](HangfireCosmos-Architecture.md).

## 💰 Cost Optimization

### Collection Strategy Comparison

| Strategy | Throughput Mode | Containers | Min RU/s | Use Case |
|----------|----------------|------------|----------|----------|
| **Dedicated** | Dedicated | 8 | 3,200 | High-volume, performance-critical workloads |
| **Dedicated** | **Shared** | 8 | **400+** | **Cost-effective dedicated containers with shared throughput** |
| **Consolidated** | Dedicated | 3 | 1,200 | Cost-effective, smaller workloads |
| **Consolidated** | **Shared** | 3 | **400+** | **Maximum cost savings for smaller workloads** |

**Cost Savings with Shared Throughput**:
- **Dedicated + Shared**: Up to 87% reduction (3,200 → 400+ RU/s)
- **Consolidated + Shared**: Up to 67% reduction (1,200 → 400+ RU/s)
- **Works with existing collections**: Containers share database-level throughput pool

### Best Practices
- **Use shared throughput** for maximum cost savings, especially in development and smaller production environments
- Use dedicated throughput when you need guaranteed performance isolation per container
- Use consolidated + shared strategy for development and small production workloads
- Use dedicated + shared strategy when you need container isolation but want cost savings
- Monitor RU consumption and adjust throughput accordingly
- Implement proper TTL settings to manage storage costs
- Use appropriate partition keys for optimal distribution
- **Shared throughput works seamlessly with other collections** in the same database

## 🤝 Contributing

1. Fork the repository
2. Create a feature branch (`git checkout -b feature/amazing-feature`)
3. Commit your changes (`git commit -m 'Add amazing feature'`)
4. Push to the branch (`git push origin feature/amazing-feature`)
5. Open a Pull Request

### Development Guidelines
- Follow existing code patterns and conventions
- Add comprehensive tests for new features
- Update documentation for API changes
- Ensure all tests pass before submitting PR
- Add appropriate logging and error handling

## 📝 License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## 🆘 Support

- **Documentation**: See [`HangfireCosmos-Architecture.md`](HangfireCosmos-Architecture.md) for detailed technical documentation
- **Issues**: Report bugs and feature requests via GitHub Issues
- **Discussions**: Join the community discussions for questions and support

## 🔄 Changelog

### Latest Changes
- ✅ Implemented configurable collection strategies (Dedicated vs Consolidated)
- ✅ Added comprehensive test suite with 100+ tests
- ✅ Enhanced monitoring and health check capabilities
- ✅ Improved error handling and retry mechanisms
- ✅ Added extensive documentation and examples

For detailed changes, see the commit history and release notes.

---

**Built with ❤️ for the Hangfire and Azure Cosmos DB community**