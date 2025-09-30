# Enhanced Features: Managed Identity & Circuit Breaker

This document describes the newly implemented managed identity authentication and circuit breaker resilience features for the Hangfire Cosmos DB Storage Provider.

## 🔐 Managed Identity Authentication

### Overview
The storage provider now supports Azure Managed Identity authentication, eliminating the need to store connection strings and providing enhanced security through Azure AD integration.

### Authentication Modes

#### 1. System-Assigned Managed Identity
```csharp
// Using extension method
builder.Services.AddHangfire(config =>
{
    config.UseCosmosStorageWithManagedIdentity(
        "https://your-cosmos-account.documents.azure.com:443/",
        options =>
        {
            options.DatabaseName = "hangfire-prod";
            options.CollectionStrategy = CollectionStrategy.Dedicated;
        });
});

// Using service collection
builder.Services.AddHangfireCosmosStorageWithManagedIdentity(
    "https://your-cosmos-account.documents.azure.com:443/",
    options =>
    {
        options.DatabaseName = "hangfire-prod";
        options.DefaultThroughput = 1000;
    });
```

#### 2. User-Assigned Managed Identity
```csharp
builder.Services.AddHangfire(config =>
{
    config.UseCosmosStorageWithManagedIdentity(
        "https://your-cosmos-account.documents.azure.com:443/",
        options =>
        {
            options.DatabaseName = "hangfire-prod";
            options.CollectionStrategy = CollectionStrategy.Consolidated;
        },
        managedIdentityClientId: "your-managed-identity-client-id");
});
```

#### 3. Service Principal Authentication
```csharp
builder.Services.AddHangfire(config =>
{
    config.UseCosmosStorageWithServicePrincipal(
        "https://your-cosmos-account.documents.azure.com:443/",
        tenantId: "your-tenant-id",
        clientId: "your-client-id",
        clientSecret: "your-client-secret",
        options =>
        {
            options.DatabaseName = "hangfire-prod";
            options.CollectionStrategy = CollectionStrategy.Dedicated;
        });
});
```

### Configuration Options
```csharp
var options = new CosmosStorageOptions
{
    // Authentication settings
    AuthenticationMode = CosmosAuthenticationMode.ManagedIdentity,
    AccountEndpoint = "https://your-cosmos-account.documents.azure.com:443/",
    ManagedIdentityClientId = "optional-client-id", // For user-assigned MI
    
    // Or for Service Principal
    ServicePrincipal = new ServicePrincipalOptions
    {
        TenantId = "your-tenant-id",
        ClientId = "your-client-id",
        ClientSecret = "your-client-secret"
    },
    
    // Standard options
    DatabaseName = "hangfire",
    CollectionStrategy = CollectionStrategy.Dedicated
};
```

## 🛡️ Circuit Breaker Pattern

### Overview
The circuit breaker pattern provides fault tolerance and prevents cascade failures when Cosmos DB experiences issues. It automatically detects failures and temporarily stops making requests to allow the service to recover.

### Circuit Breaker States
- **Closed**: Normal operation, requests flow through
- **Open**: Service is failing, requests are rejected immediately
- **Half-Open**: Testing recovery, limited requests allowed

### Configuration
```csharp
builder.Services.AddHangfireCosmosStorage(options =>
{
    options.AuthenticationMode = CosmosAuthenticationMode.ManagedIdentity;
    options.AccountEndpoint = "https://your-cosmos-account.documents.azure.com:443/";
    options.DatabaseName = "hangfire-prod";
    
    // Circuit breaker configuration
    options.CircuitBreaker.Enabled = true;
    options.CircuitBreaker.FailureThreshold = 5;        // Open after 5 failures
    options.CircuitBreaker.OpenTimeout = TimeSpan.FromMinutes(2);  // Wait 2 minutes before retry
    options.CircuitBreaker.SuccessThreshold = 3;        // Close after 3 successes
    options.CircuitBreaker.OperationTimeout = TimeSpan.FromSeconds(30); // Individual operation timeout
});
```

### Circuit Breaker Options
```csharp
public class CircuitBreakerOptions
{
    public bool Enabled { get; set; } = true;                    // Enable/disable circuit breaker
    public int FailureThreshold { get; set; } = 5;               // Failures before opening
    public TimeSpan OpenTimeout { get; set; } = TimeSpan.FromMinutes(1);  // Recovery wait time
    public int SuccessThreshold { get; set; } = 3;               // Successes to close circuit
    public TimeSpan OperationTimeout { get; set; } = TimeSpan.FromSeconds(30); // Operation timeout
}
```

### Monitoring Circuit Breaker
```csharp
// Access circuit breaker state through the resilient repository
if (serviceProvider.GetService<ICosmosDocumentRepository>() is ResilientCosmosDocumentRepository resilientRepo)
{
    var state = resilientRepo.CircuitBreakerState;
    var failureCount = resilientRepo.FailureCount;
    var operationFailures = resilientRepo.OperationFailureCounts;
    
    // Log or monitor the circuit breaker state
    logger.LogInformation("Circuit breaker state: {State}, Failures: {FailureCount}", state, failureCount);
}
```

## 🚀 Usage Examples

### Production Setup with Managed Identity and Circuit Breaker
```csharp
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddHangfire(config =>
{
    config.UseCosmosStorageWithManagedIdentity(
        builder.Configuration["CosmosDb:AccountEndpoint"],
        options =>
        {
            // Database configuration
            options.DatabaseName = "hangfire-prod";
            options.CollectionStrategy = CollectionStrategy.Dedicated;
            options.DefaultThroughput = 1000;
            options.UseSharedThroughput = false;
            
            // Circuit breaker configuration
            options.CircuitBreaker.Enabled = true;
            options.CircuitBreaker.FailureThreshold = 3;
            options.CircuitBreaker.OpenTimeout = TimeSpan.FromMinutes(2);
            options.CircuitBreaker.SuccessThreshold = 2;
            
            // Performance tuning
            options.Performance.ConnectionMode = ConnectionMode.Direct;
            options.Performance.QueryPageSize = 100;
            options.MaxRetryAttempts = 3;
            options.RetryDelay = TimeSpan.FromMilliseconds(500);
        });
});

builder.Services.AddHangfireServer();
var app = builder.Build();
app.UseHangfireDashboard();
app.Run();
```

### Development Setup with Connection String
```csharp
builder.Services.AddHangfire(config =>
{
    config.UseCosmosStorage(
        builder.Configuration.GetConnectionString("CosmosDb"),
        options =>
        {
            options.DatabaseName = "hangfire-dev";
            options.CollectionStrategy = CollectionStrategy.Consolidated;
            options.DefaultThroughput = 400;
            options.UseSharedThroughput = true;
            
            // Enable circuit breaker for development testing
            options.CircuitBreaker.Enabled = true;
            options.CircuitBreaker.FailureThreshold = 2;
            options.CircuitBreaker.OpenTimeout = TimeSpan.FromSeconds(30);
        });
});
```

## 🔧 Azure Setup

### 1. Enable Managed Identity
```bash
# Enable system-assigned managed identity
az webapp identity assign --name your-app-name --resource-group your-rg

# Create user-assigned managed identity
az identity create --name your-identity-name --resource-group your-rg
az webapp identity assign --name your-app-name --resource-group your-rg --identities /subscriptions/{subscription-id}/resourcegroups/{rg}/providers/Microsoft.ManagedIdentity/userAssignedIdentities/{identity-name}
```

### 2. Grant Cosmos DB Permissions
```bash
# Get the managed identity principal ID
PRINCIPAL_ID=$(az webapp identity show --name your-app-name --resource-group your-rg --query principalId -o tsv)

# Assign Cosmos DB Data Contributor role
az cosmosdb sql role assignment create \
    --account-name your-cosmos-account \
    --resource-group your-rg \
    --scope "/" \
    --principal-id $PRINCIPAL_ID \
    --role-definition-id 00000000-0000-0000-0000-000000000002
```

## 📊 Benefits

### Security Benefits
- **Zero Secrets**: No connection strings stored in configuration
- **Azure AD Integration**: Leverages Azure Active Directory for authentication
- **Automatic Credential Rotation**: Azure manages credential lifecycle
- **Fine-Grained Access Control**: Use Azure RBAC for precise permissions

### Resilience Benefits
- **Fault Tolerance**: Circuit breaker prevents cascade failures
- **Fast Failure**: Immediate rejection when service is down
- **Automatic Recovery**: Self-healing when service recovers
- **Configurable Behavior**: Tunable failure and recovery thresholds

### Operational Benefits
- **Monitoring Integration**: Circuit breaker state can be monitored
- **Graceful Degradation**: System continues operating during partial failures
- **Performance Protection**: Prevents resource exhaustion during outages
- **Detailed Logging**: Comprehensive logging of authentication and circuit breaker events

## 🔍 Troubleshooting

### Managed Identity Issues
1. **Authentication Failed**: Ensure managed identity is enabled and has proper Cosmos DB permissions
2. **Token Acquisition Failed**: Check Azure AD configuration and network connectivity
3. **Permission Denied**: Verify RBAC role assignments on Cosmos DB account

### Circuit Breaker Issues
1. **Circuit Always Open**: Check failure threshold and operation timeout settings
2. **Slow Recovery**: Adjust open timeout and success threshold values
3. **False Positives**: Review operation timeout and retry configuration

### Common Solutions
```csharp
// Enable detailed logging for troubleshooting
options.Performance.EnableQueryMetrics = true;

// Adjust timeouts for slow networks
options.RequestTimeout = TimeSpan.FromSeconds(60);
options.CircuitBreaker.OperationTimeout = TimeSpan.FromSeconds(45);

// Increase retry attempts for transient failures
options.MaxRetryAttempts = 5;
options.RetryDelay = TimeSpan.FromSeconds(1);
```

This enhanced architecture provides enterprise-grade security and resilience for production Hangfire deployments using Azure Cosmos DB.