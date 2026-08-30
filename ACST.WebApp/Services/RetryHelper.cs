using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace ACST.WebApp.Services
{
    /// <summary>
    /// Configuration options for executing operations with retry logic in WebApp.
    /// </summary>
    public sealed record RetryOptions
    {
        /// <summary>
        /// Gets or sets the maximum number of attempts (including initial attempt). Default is 3.
        /// </summary>
        public int MaxRetries { get; init; } = 3;

        /// <summary>
        /// Gets or sets the initial delay before the first retry attempt. Default is 1 second.
        /// </summary>
        public TimeSpan InitialDelay { get; init; } = TimeSpan.FromSeconds(1);

        /// <summary>
        /// Gets or sets the backoff multiplication factor for subsequent retries. Default is 2.0.
        /// </summary>
        public double BackoffMultiplier { get; init; } = 2.0;

        /// <summary>
        /// Gets or sets the maximum delay allowed between retries. Default is 10 seconds.
        /// </summary>
        public TimeSpan MaxDelay { get; init; } = TimeSpan.FromSeconds(10);
    }

    /// <summary>
    /// Utility class providing resilient execution of asynchronous operations with exponential backoff and transient failure handling.
    /// </summary>
    public static class RetryHelper
    {
        /// <summary>
        /// Executes an asynchronous operation with retry logic for transient failures.
        /// </summary>
        /// <typeparam name="T">The return type of the operation.</typeparam>
        /// <param name="action">The asynchronous operation to execute.</param>
        /// <param name="options">Retry configuration options (max retries, backoff, initial delay).</param>
        /// <param name="onRetry">Optional callback invoked before each retry: (currentAttempt, maxRetries, delay, exception).</param>
        /// <param name="isTransient">Optional predicate to determine if an exception is transient.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The result of the operation if successful.</returns>
        public static async Task<T> ExecuteWithRetryAsync<T>(
            Func<CancellationToken, Task<T>> action,
            RetryOptions? options = null,
            Func<int, int, TimeSpan, Exception?, Task>? onRetry = null,
            Func<Exception, bool>? isTransient = null,
            CancellationToken cancellationToken = default)
        {
            var opt = options ?? new RetryOptions();
            var maxAttempts = Math.Max(1, opt.MaxRetries);
            var isTransientPredicate = isTransient ?? IsTransientDefault;

            for (int attempt = 1; ; attempt++)
            {
                cancellationToken.ThrowIfCancellationRequested();

                try
                {
                    return await action(cancellationToken);
                }
                catch (Exception ex) when (!cancellationToken.IsCancellationRequested && attempt < maxAttempts && isTransientPredicate(ex))
                {
                    var delay = CalculateDelay(attempt, opt);

                    if (onRetry != null)
                    {
                        try
                        {
                            await onRetry(attempt, maxAttempts, delay, ex);
                        }
                        catch
                        {
                            // Avoid masking original operation exception with callback failure
                        }
                    }

                    await Task.Delay(delay, cancellationToken);
                }
            }
        }

        /// <summary>
        /// Executes an asynchronous void operation with retry logic for transient failures.
        /// </summary>
        /// <param name="action">The asynchronous action to execute.</param>
        /// <param name="options">Retry configuration options.</param>
        /// <param name="onRetry">Optional callback invoked before each retry.</param>
        /// <param name="isTransient">Optional predicate to determine if an exception is transient.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        public static async Task ExecuteWithRetryAsync(
            Func<CancellationToken, Task> action,
            RetryOptions? options = null,
            Func<int, int, TimeSpan, Exception?, Task>? onRetry = null,
            Func<Exception, bool>? isTransient = null,
            CancellationToken cancellationToken = default)
        {
            await ExecuteWithRetryAsync(async ct =>
            {
                await action(ct);
                return true;
            }, options, onRetry, isTransient, cancellationToken);
        }

        /// <summary>
        /// Computes the exponential backoff delay for the given attempt.
        /// </summary>
        /// <param name="attempt">The current attempt number (1-based).</param>
        /// <param name="options">The retry options specifying initial delay, multiplier, and max delay.</param>
        /// <returns>The calculated backoff TimeSpan.</returns>
        public static TimeSpan CalculateDelay(int attempt, RetryOptions options)
        {
            var multiplier = Math.Pow(options.BackoffMultiplier, Math.Max(0, attempt - 1));
            var calculatedMs = options.InitialDelay.TotalMilliseconds * multiplier;
            var cappedMs = Math.Min(calculatedMs, options.MaxDelay.TotalMilliseconds);
            return TimeSpan.FromMilliseconds(cappedMs);
        }

        /// <summary>
        /// Evaluates whether an exception represents a transient failure that should be retried.
        /// </summary>
        /// <param name="ex">The exception to evaluate.</param>
        /// <returns>True if the exception is transient; otherwise false.</returns>
        public static bool IsTransientDefault(Exception ex)
        {
            if (ex is OperationCanceledException or TaskCanceledException)
            {
                return false;
            }

            if (ex is TimeoutException)
            {
                return true;
            }

            if (ex is HttpRequestException httpEx)
            {
                if (httpEx.StatusCode.HasValue)
                {
                    var code = (int)httpEx.StatusCode.Value;
                    // Retriable HTTP status codes
                    return code is 408 or 429 or 500 or 502 or 503 or 504;
                }
                // Network/DNS/connection failures without a status code are transient
                return true;
            }

            var message = ex.Message;
            if (message.Contains("Failed to fetch", StringComparison.OrdinalIgnoreCase) ||
                message.Contains("TypeError: Failed to fetch", StringComparison.OrdinalIgnoreCase) ||
                message.Contains("connection refused", StringComparison.OrdinalIgnoreCase) ||
                message.Contains("network error", StringComparison.OrdinalIgnoreCase) ||
                message.Contains("temporarily unavailable", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            if (ex.InnerException != null)
            {
                return IsTransientDefault(ex.InnerException);
            }

            return false;
        }
    }
}
