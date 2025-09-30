using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;

namespace HangfireCosmos.Storage.Resilience;

/// <summary>
/// Circuit breaker implementation for Cosmos DB operations to provide fault tolerance and prevent cascade failures.
/// </summary>
public class CosmosCircuitBreaker
{
    private readonly CircuitBreakerOptions _options;
    private readonly ILogger<CosmosCircuitBreaker>? _logger;
    private readonly object _lock = new();
    
    private CircuitBreakerState _state = CircuitBreakerState.Closed;
    private int _failureCount = 0;
    private int _successCount = 0;
    private DateTime _lastFailureTime = DateTime.MinValue;
    private readonly ConcurrentDictionary<string, int> _operationFailureCounts = new();

    /// <summary>
    /// Initializes a new instance of the CosmosCircuitBreaker class.
    /// </summary>
    /// <param name="options">The circuit breaker configuration options.</param>
    /// <param name="logger">The optional logger instance.</param>
    public CosmosCircuitBreaker(CircuitBreakerOptions options, ILogger<CosmosCircuitBreaker>? logger = null)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _logger = logger;
        
        _options.Validate();
    }

    /// <summary>
    /// Executes an operation with circuit breaker protection.
    /// </summary>
    /// <typeparam name="T">The return type of the operation.</typeparam>
    /// <param name="operation">The operation to execute.</param>
    /// <param name="operationName">The name of the operation for logging and monitoring.</param>
    /// <returns>The result of the operation.</returns>
    /// <exception cref="CircuitBreakerOpenException">Thrown when the circuit breaker is open.</exception>
    public async Task<T> ExecuteAsync<T>(Func<Task<T>> operation, string operationName = "Unknown")
    {
        if (!_options.Enabled)
        {
            return await operation().ConfigureAwait(false);
        }

        if (ShouldRejectRequest(operationName))
        {
            var retryAfter = _lastFailureTime.Add(_options.OpenTimeout);
            _logger?.LogWarning("Circuit breaker is OPEN for operation: {OperationName}. Retry after: {RetryAfter}",
                operationName, retryAfter);
            
            throw new CircuitBreakerOpenException($"Circuit breaker is open for operation: {operationName}")
            {
                OperationName = operationName,
                RetryAfter = retryAfter
            };
        }

        try
        {
            using var cts = new CancellationTokenSource(_options.OperationTimeout);
            var result = await operation().ConfigureAwait(false);
            
            OnSuccess(operationName);
            return result;
        }
        catch (OperationCanceledException ex)
        {
            var timeoutException = new TimeoutException($"Operation '{operationName}' timed out after {_options.OperationTimeout}", ex);
            OnFailure(timeoutException, operationName);
            throw timeoutException;
        }
        catch (Exception ex)
        {
            OnFailure(ex, operationName);
            throw;
        }
    }

    /// <summary>
    /// Executes an operation with circuit breaker protection (void return).
    /// </summary>
    /// <param name="operation">The operation to execute.</param>
    /// <param name="operationName">The name of the operation for logging and monitoring.</param>
    /// <exception cref="CircuitBreakerOpenException">Thrown when the circuit breaker is open.</exception>
    public async Task ExecuteAsync(Func<Task> operation, string operationName = "Unknown")
    {
        await ExecuteAsync(async () =>
        {
            await operation().ConfigureAwait(false);
            return true; // Dummy return value
        }, operationName).ConfigureAwait(false);
    }

    private bool ShouldRejectRequest(string operationName)
    {
        lock (_lock)
        {
            switch (_state)
            {
                case CircuitBreakerState.Closed:
                    return false;
                    
                case CircuitBreakerState.Open:
                    if (DateTime.UtcNow - _lastFailureTime >= _options.OpenTimeout)
                    {
                        _state = CircuitBreakerState.HalfOpen;
                        _successCount = 0;
                        _logger?.LogInformation("Circuit breaker transitioning to HALF-OPEN state for operation: {OperationName}", operationName);
                        return false;
                    }
                    return true;
                    
                case CircuitBreakerState.HalfOpen:
                    return false;
                    
                default:
                    return false;
            }
        }
    }

    private void OnSuccess(string operationName)
    {
        lock (_lock)
        {
            _failureCount = 0;
            _operationFailureCounts.TryRemove(operationName, out _);
            
            if (_state == CircuitBreakerState.HalfOpen)
            {
                _successCount++;
                _logger?.LogDebug("Circuit breaker success count: {SuccessCount}/{SuccessThreshold} for operation: {OperationName}",
                    _successCount, _options.SuccessThreshold, operationName);
                
                if (_successCount >= _options.SuccessThreshold)
                {
                    _state = CircuitBreakerState.Closed;
                    _successCount = 0;
                    _logger?.LogInformation("Circuit breaker CLOSED after successful recovery for operation: {OperationName}", operationName);
                }
            }
        }
    }

    private void OnFailure(Exception exception, string operationName)
    {
        lock (_lock)
        {
            _failureCount++;
            _lastFailureTime = DateTime.UtcNow;
            
            // Track per-operation failure counts for detailed monitoring
            _operationFailureCounts.AddOrUpdate(operationName, 1, (key, count) => count + 1);
            
            _logger?.LogWarning(exception, "Circuit breaker failure {FailureCount}/{FailureThreshold} for operation: {OperationName}. Exception: {ExceptionType}",
                _failureCount, _options.FailureThreshold, operationName, exception.GetType().Name);

            if (_state == CircuitBreakerState.HalfOpen || _failureCount >= _options.FailureThreshold)
            {
                _state = CircuitBreakerState.Open;
                _logger?.LogError("Circuit breaker OPENED due to failures for operation: {OperationName}. Total failures: {FailureCount}",
                    operationName, _failureCount);
            }
        }
    }

    /// <summary>
    /// Gets the current state of the circuit breaker.
    /// </summary>
    public CircuitBreakerState State
    {
        get
        {
            lock (_lock)
            {
                return _state;
            }
        }
    }

    /// <summary>
    /// Gets the current failure count.
    /// </summary>
    public int FailureCount
    {
        get
        {
            lock (_lock)
            {
                return _failureCount;
            }
        }
    }

    /// <summary>
    /// Gets the current success count (only relevant in half-open state).
    /// </summary>
    public int SuccessCount
    {
        get
        {
            lock (_lock)
            {
                return _successCount;
            }
        }
    }

    /// <summary>
    /// Gets the time of the last failure.
    /// </summary>
    public DateTime LastFailureTime
    {
        get
        {
            lock (_lock)
            {
                return _lastFailureTime;
            }
        }
    }

    /// <summary>
    /// Gets the failure counts per operation for monitoring purposes.
    /// </summary>
    public IReadOnlyDictionary<string, int> OperationFailureCounts => _operationFailureCounts.ToArray().ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

    /// <summary>
    /// Manually resets the circuit breaker to closed state.
    /// </summary>
    public void Reset()
    {
        lock (_lock)
        {
            _state = CircuitBreakerState.Closed;
            _failureCount = 0;
            _successCount = 0;
            _lastFailureTime = DateTime.MinValue;
            _operationFailureCounts.Clear();
            
            _logger?.LogInformation("Circuit breaker manually reset to CLOSED state");
        }
    }
}