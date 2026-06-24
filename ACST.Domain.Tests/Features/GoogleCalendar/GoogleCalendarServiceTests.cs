using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ACST.Domain.Features.GoogleCalendar;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace ACST.Domain.Tests.Features.GoogleCalendar;

public class GoogleCalendarServiceTests
{
    [Fact]
    public async Task CreateEventAsync_WithMissingConfiguration_ShouldReturnSystemError()
    {
        // Arrange
        var inMemorySettings = new Dictionary<string, string?>
        {
            // Empty settings
        };

        IConfiguration configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(inMemorySettings)
            .Build();

        var service = new GoogleCalendarService(configuration, new NullLogger<GoogleCalendarService>());

        // Act
        var result = await service.CreateEventAsync(
            "Test Event",
            DateTime.UtcNow,
            DateTime.UtcNow.AddHours(1),
            "Test Description",
            Guid.NewGuid()
        );

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Contains("GoogleCalendar:ClientId configuration is missing", result.Message);
    }

    [Fact]
    public async Task UpdateEventStatusAsync_WithMissingConfiguration_ShouldReturnSystemError()
    {
        // Arrange
        var inMemorySettings = new Dictionary<string, string?>
        {
            // Empty settings
        };

        IConfiguration configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(inMemorySettings)
            .Build();

        var service = new GoogleCalendarService(configuration, new NullLogger<GoogleCalendarService>());

        // Act
        var result = await service.UpdateEventStatusAsync("event_id", "Confirmed");

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Contains("GoogleCalendar:ClientId configuration is missing", result.Message);
    }

    [Fact]
    public async Task DeleteEventAsync_WithMissingConfiguration_ShouldReturnSystemError()
    {
        // Arrange
        var inMemorySettings = new Dictionary<string, string?>
        {
            // Empty settings
        };

        IConfiguration configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(inMemorySettings)
            .Build();

        var service = new GoogleCalendarService(configuration, new NullLogger<GoogleCalendarService>());

        // Act
        var result = await service.DeleteEventAsync("event_id");

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Contains("GoogleCalendar:ClientId configuration is missing", result.Message);
    }
}
