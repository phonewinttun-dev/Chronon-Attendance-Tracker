using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using ACST.Shared;
using Xunit;

namespace ACST.Domain.Tests.Features.Auth
{
    public class RetryHelperTests
    {
        [Fact]
        public async Task ExecuteWithRetryAsync_SuccessfulOnFirstAttempt_ReturnsResultAndDoesNotRetry()
        {
            // Arrange
            int executionCount = 0;
            int retryCallbackCount = 0;

            // Act
            var result = await RetryHelper.ExecuteWithRetryAsync(
                async ct =>
                {
                    executionCount++;
                    await Task.CompletedTask;
                    return "success";
                },
                new RetryOptions { MaxRetries = 3 },
                onRetry: async (attempt, maxAttempts, delay, ex) =>
                {
                    retryCallbackCount++;
                    await Task.CompletedTask;
                }
            );

            // Assert
            Assert.Equal("success", result);
            Assert.Equal(1, executionCount);
            Assert.Equal(0, retryCallbackCount);
        }

        [Fact]
        public async Task ExecuteWithRetryAsync_TransientException_RetriesAndSucceeds()
        {
            // Arrange
            int executionCount = 0;
            int retryCallbackCount = 0;

            // Act
            var result = await RetryHelper.ExecuteWithRetryAsync(
                async ct =>
                {
                    executionCount++;
                    if (executionCount < 3)
                    {
                        throw new HttpRequestException("Transient network glitch", null, HttpStatusCode.ServiceUnavailable);
                    }
                    await Task.CompletedTask;
                    return "recovered";
                },
                new RetryOptions
                {
                    MaxRetries = 3,
                    InitialDelay = TimeSpan.FromMilliseconds(10),
                    BackoffMultiplier = 1.5
                },
                onRetry: async (attempt, maxAttempts, delay, ex) =>
                {
                    retryCallbackCount++;
                    await Task.CompletedTask;
                }
            );

            // Assert
            Assert.Equal("recovered", result);
            Assert.Equal(3, executionCount);
            Assert.Equal(2, retryCallbackCount);
        }

        [Fact]
        public async Task ExecuteWithRetryAsync_NonTransientException_FastFailsWithoutRetry()
        {
            // Arrange
            int executionCount = 0;
            int retryCallbackCount = 0;

            // Act & Assert
            var ex = await Assert.ThrowsAsync<HttpRequestException>(async () =>
            {
                await RetryHelper.ExecuteWithRetryAsync<string>(
                    async ct =>
                    {
                        executionCount++;
                        await Task.CompletedTask;
                        throw new HttpRequestException("Bad request", null, HttpStatusCode.BadRequest);
                    },
                    new RetryOptions { MaxRetries = 3 },
                    onRetry: async (attempt, maxAttempts, delay, e) =>
                    {
                        retryCallbackCount++;
                        await Task.CompletedTask;
                    }
                );
            });

            Assert.Equal(HttpStatusCode.BadRequest, ex.StatusCode);
            Assert.Equal(1, executionCount);
            Assert.Equal(0, retryCallbackCount);
        }

        [Fact]
        public async Task ExecuteWithRetryAsync_ExceedsMaxRetries_ThrowsLastException()
        {
            // Arrange
            int executionCount = 0;
            int retryCallbackCount = 0;

            // Act & Assert
            var ex = await Assert.ThrowsAsync<HttpRequestException>(async () =>
            {
                await RetryHelper.ExecuteWithRetryAsync<string>(
                    async ct =>
                    {
                        executionCount++;
                        await Task.CompletedTask;
                        throw new HttpRequestException("Server Down", null, HttpStatusCode.BadGateway);
                    },
                    new RetryOptions
                    {
                        MaxRetries = 3,
                        InitialDelay = TimeSpan.FromMilliseconds(10),
                        BackoffMultiplier = 1.5
                    },
                    onRetry: async (attempt, maxAttempts, delay, e) =>
                    {
                        retryCallbackCount++;
                        await Task.CompletedTask;
                    }
                );
            });

            Assert.Equal(HttpStatusCode.BadGateway, ex.StatusCode);
            Assert.Equal(3, executionCount);
            Assert.Equal(2, retryCallbackCount);
        }

        [Fact]
        public async Task ExecuteWithRetryAsync_CancellationTokenCancelled_ThrowsImmediately()
        {
            // Arrange
            using var cts = new CancellationTokenSource();
            cts.Cancel();

            // Act & Assert
            await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
            {
                await RetryHelper.ExecuteWithRetryAsync<string>(
                    async ct =>
                    {
                        await Task.CompletedTask;
                        return "done";
                    },
                    new RetryOptions { MaxRetries = 3 },
                    cancellationToken: cts.Token
                );
            });
        }

        [Fact]
        public void CalculateDelay_ExponentialBackoff_CalculatesCorrectly()
        {
            // Arrange
            var options = new RetryOptions
            {
                InitialDelay = TimeSpan.FromSeconds(1),
                BackoffMultiplier = 2.0,
                MaxDelay = TimeSpan.FromSeconds(10)
            };

            // Act
            var delayAttempt1 = RetryHelper.CalculateDelay(1, options);
            var delayAttempt2 = RetryHelper.CalculateDelay(2, options);
            var delayAttempt3 = RetryHelper.CalculateDelay(3, options);
            var delayAttempt10 = RetryHelper.CalculateDelay(10, options);

            // Assert
            Assert.Equal(TimeSpan.FromSeconds(1), delayAttempt1);
            Assert.Equal(TimeSpan.FromSeconds(2), delayAttempt2);
            Assert.Equal(TimeSpan.FromSeconds(4), delayAttempt3);
            Assert.Equal(TimeSpan.FromSeconds(10), delayAttempt10); // Capped by MaxDelay
        }

        [Theory]
        [InlineData(HttpStatusCode.InternalServerError, true)]
        [InlineData(HttpStatusCode.BadGateway, true)]
        [InlineData(HttpStatusCode.ServiceUnavailable, true)]
        [InlineData(HttpStatusCode.GatewayTimeout, true)]
        [InlineData(HttpStatusCode.RequestTimeout, true)]
        [InlineData(HttpStatusCode.TooManyRequests, true)]
        [InlineData(HttpStatusCode.BadRequest, false)]
        [InlineData(HttpStatusCode.Unauthorized, false)]
        [InlineData(HttpStatusCode.Forbidden, false)]
        [InlineData(HttpStatusCode.NotFound, false)]
        public void IsTransientDefault_HttpStatusCodes_ClassifiesCorrectly(HttpStatusCode statusCode, bool expectedTransient)
        {
            // Arrange
            var ex = new HttpRequestException("HTTP Error", null, statusCode);

            // Act
            var isTransient = RetryHelper.IsTransientDefault(ex);

            // Assert
            Assert.Equal(expectedTransient, isTransient);
        }

        [Fact]
        public void IsTransientDefault_FailedToFetch_ReturnsTrue()
        {
            // Arrange
            var ex = new Exception("TypeError: Failed to fetch");

            // Act
            var isTransient = RetryHelper.IsTransientDefault(ex);

            // Assert
            Assert.True(isTransient);
        }
    }
}
