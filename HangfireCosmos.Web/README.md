# Hangfire Cosmos Storage Provider - Web Application

A comprehensive web application for testing and debugging the Hangfire Cosmos DB storage provider. This application provides a full-featured testing environment with monitoring, health checks, and various job scenarios.

## Features

### 🎯 Core Features
- **Comprehensive Job Testing**: Multiple job types and scenarios for thorough testing
- **Real-time Monitoring**: Live dashboard with job statistics and system health
- **Health Checks**: Automated health monitoring for Cosmos DB and Hangfire
- **Configuration Management**: Environment-specific configuration with validation
- **Error Handling**: Global exception handling with detailed error responses
- **Structured Logging**: Comprehensive logging with ILogger
- **API Documentation**: Swagger/OpenAPI documentation for all endpoints

### 🔧 Job Testing Scenarios
- **Simple Jobs**: Basic fire-and-forget jobs
- **Delayed Jobs**: Jobs scheduled for future execution
- **Recurring Jobs**: Periodic jobs with cron expressions
- **Long-Running Jobs**: Jobs that take time to complete for monitoring testing
- **Failing Jobs**: Jobs that intentionally fail for error handling testing
- **Batch Jobs**: Multiple related jobs for concurrency testing
- **Continuation Jobs**: Jobs that run after other jobs complete
- **I/O Intensive Jobs**: Jobs that perform file operations
- **Memory Intensive Jobs**: Jobs that test memory usage patterns
- **Progress Reporting Jobs**: Jobs with progress tracking

## Quick Start

### Prerequisites
- .NET 9.0 SDK
- Azure Cosmos DB Emulator (for local development) or Azure Cosmos DB account
- Visual Studio 2022 or VS Code

### Running the Application

1. **Start Cosmos DB Emulator** (for local development):
   ```bash
   # Download and install Azure Cosmos DB Emulator
   # Start the emulator with default settings
   ```

2. **Configure Connection String** (optional):
   ```json
   // appsettings.Development.json
   {
     "ConnectionStrings": {
       "CosmosDb": "AccountEndpoint=https://localhost:8081/;AccountKey=C2y6yDjf5/R+ob0N8A7Cgv30VRDJIWEHLM+4QDU5DE2nQ9nDuVTqobD4b8mGGyPMbIZnqyMsEcaGQy67XIw/Jw=="
     }
   }
   ```

3. **Run the Application**:
   ```bash
   cd HangfireCosmos.Web
   dotnet run
   ```

4. **Access the Application**:
   - **Main UI**: http://localhost:5000
   - **Hangfire Dashboard**: http://localhost:5000/hangfire
   - **API Documentation**: http://localhost:5000/swagger
   - **Health Checks**: http://localhost:5000/health

## Application Structure

### Controllers

#### JobController (`/api/job`)
Comprehensive job testing endpoints:
- `POST /api/job/simple` - Enqueue simple jobs
- `POST /api/job/delayed` - Schedule delayed jobs
- `POST /api/job/recurring` - Create recurring jobs
- `POST /api/job/long-running` - Start long-running jobs
- `POST /api/job/failing` - Create failing jobs
- `POST /api/job/batch` - Create batch jobs
- `POST /api/job/continuation` - Create continuation jobs
- `POST /api/job/data-processing` - Data processing jobs
- `POST /api/job/io-intensive` - I/O intensive jobs
- `POST /api/job/memory-intensive` - Memory intensive jobs
- `DELETE /api/job/{jobId}` - Delete jobs
- `POST /api/job/{jobId}/requeue` - Requeue failed jobs

#### HealthController (`/api/health`)
System monitoring and health checks:
- `GET /api/health` - Overall health status
- `GET /api/health/system` - Detailed system information
- `GET /api/health/hangfire` - Hangfire statistics
- `GET /api/health/cosmos` - Cosmos DB connection status
- `POST /api/health/cosmos/test` - Test Cosmos DB operations
- `GET /api/health/metrics` - Application metrics

#### ConfigController (`/api/config`)
Configuration management:
- `GET /api/config` - Current configuration
- `GET /api/config/connections` - Connection strings (masked)
- `GET /api/config/storage` - Storage information
- `GET /api/config/application` - Application information
- `GET /api/config/validate` - Configuration validation
- `GET /api/config/environment` - Environment variables
- `GET /api/config/features` - Feature flags

### Services

#### JobTestingService
Implements various job scenarios:
- Simple and complex job operations
- Error simulation and handling
- Progress reporting and cancellation
- Resource-intensive operations
- Batch processing scenarios

### Middleware

#### GlobalExceptionMiddleware
Comprehensive error handling:
- Cosmos DB specific error handling
- Structured error responses
- Development vs production error details
- Correlation ID tracking
- Appropriate HTTP status codes

### Configuration

#### Development Configuration
```json
{
  "HangfireCosmosOptions": {
    "DatabaseName": "hangfire-dev",
    "DefaultThroughput": 400,
    "AutoCreateDatabase": true,
    "AutoCreateContainers": true,
    "DefaultJobExpiration": "1.00:00:00",
    "ServerTimeout": "00:02:00",
    "LockTimeout": "00:00:30",
    "MaxRetryAttempts": 5,
    "RetryDelay": "00:00:00.200"
  },
  "JobTesting": {
    "EnableTestJobs": true,
    "MaxConcurrentJobs": 5,
    "DefaultJobTimeout": "00:10:00",
    "EnableFailureSimulation": true,
    "EnableLongRunningJobs": true
  }
}
```

#### Production Configuration
```json
{
  "HangfireCosmosOptions": {
    "DatabaseName": "hangfire-prod",
    "DefaultThroughput": 1000,
    "AutoCreateDatabase": false,
    "AutoCreateContainers": false,
    "DefaultJobExpiration": "30.00:00:00",
    "ServerTimeout": "00:10:00",
    "LockTimeout": "00:02:00",
    "MaxRetryAttempts": 3,
    "RetryDelay": "00:00:01.000"
  },
  "JobTesting": {
    "EnableTestJobs": false,
    "MaxConcurrentJobs": 20,
    "DefaultJobTimeout": "01:00:00"
  }
}
```

## Testing Scenarios

### Basic Job Testing
```bash
# Enqueue a simple job
curl -X POST "http://localhost:5000/api/job/simple" \
  -H "Content-Type: application/json" \
  -d '{"message": "Hello from API!"}'

# Schedule a delayed job
curl -X POST "http://localhost:5000/api/job/delayed" \
  -H "Content-Type: application/json" \
  -d '{"message": "Delayed job", "delayInSeconds": 30}'

# Create a recurring job
curl -X POST "http://localhost:5000/api/job/recurring" \
  -H "Content-Type: application/json" \
  -d '{"jobId": "test-recurring", "cronExpression": "*/5 * * * *"}'
```

### Batch Job Testing
```bash
# Create a batch of jobs
curl -X POST "http://localhost:5000/api/job/batch" \
  -H "Content-Type: application/json" \
  -d '{"dataPrefix": "TestItem", "itemCount": 10}'
```

### Error Testing
```bash
# Create a failing job
curl -X POST "http://localhost:5000/api/job/failing" \
  -H "Content-Type: application/json" \
  -d '{"errorMessage": "Test error for debugging"}'
```

### Performance Testing
```bash
# Memory intensive job
curl -X POST "http://localhost:5000/api/job/memory-intensive" \
  -H "Content-Type: application/json" \
  -d '{"memorySizeMB": 100}'

# I/O intensive job
curl -X POST "http://localhost:5000/api/job/io-intensive" \
  -H "Content-Type: application/json" \
  -d '{"fileName": "test-file", "operationCount": 1000}'
```

## Monitoring and Debugging

### Health Checks
The application provides comprehensive health checks:
- **Application Health**: Basic application status
- **Cosmos DB Health**: Database connectivity and operations
- **Hangfire Health**: Job processing status and statistics

### Logging
Structured logging with ILogger:
- **Console Logging**: Real-time log output
- **File Logging**: Persistent log files with rotation
- **Correlation IDs**: Request tracking across components
- **Structured Data**: JSON-formatted log entries

### Metrics
Application metrics available through `/api/health/metrics`:
- Process information (CPU, memory, threads)
- Garbage collection statistics
- Thread pool information
- Custom application metrics

## Troubleshooting

### Common Issues

#### Cosmos DB Connection Issues
1. **Emulator not running**: Start Azure Cosmos DB Emulator
2. **Connection string**: Verify connection string in configuration
3. **Firewall**: Check Windows Firewall settings for emulator

#### Job Execution Issues
1. **Check Hangfire Dashboard**: Monitor job status and errors
2. **Review Logs**: Check application logs for detailed error information
3. **Health Checks**: Use health endpoints to verify system status

#### Performance Issues
1. **Cosmos DB Throughput**: Increase RU/s allocation if needed
2. **Worker Count**: Adjust Hangfire server worker count
3. **Memory Usage**: Monitor memory-intensive jobs

### Debug Mode
Enable detailed debugging in development:
```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Debug",
      "HangfireCosmos": "Debug",
      "Hangfire": "Debug"
    }
  }
}
```

## Production Deployment

### Configuration
1. **Connection Strings**: Use Azure Cosmos DB connection string
2. **Throughput**: Set appropriate RU/s for production workload
3. **Security**: Implement proper authentication for Hangfire Dashboard
4. **Logging**: Configure appropriate log levels and retention

### Security Considerations
- **Dashboard Access**: Implement proper authorization filters
- **API Security**: Add authentication/authorization as needed
- **Connection Strings**: Use Azure Key Vault or secure configuration
- **CORS**: Configure appropriate CORS policies

### Monitoring
- **Application Insights**: Integrate for production monitoring
- **Health Checks**: Set up monitoring alerts
- **Log Analytics**: Configure log aggregation and analysis

## API Reference

Complete API documentation is available at `/swagger` when running in development mode.

### Authentication
Currently configured for development/testing with no authentication. Implement proper authentication for production use.

### Rate Limiting
No rate limiting is currently implemented. Consider adding rate limiting for production deployments.

### Versioning
API versioning is not currently implemented but can be added using ASP.NET Core API versioning.

## Contributing

1. Follow the existing code structure and patterns
2. Add comprehensive logging for new features
3. Include appropriate error handling
4. Update documentation for new endpoints or features
5. Add health checks for new dependencies

## License

This project is part of the Hangfire Cosmos Storage Provider and follows the same licensing terms.