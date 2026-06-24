using System;
using System.Threading.Tasks;
using ACST.Domain.Features.GoogleCalendar;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace ACST.Domain.Tests.Features.GoogleCalendar;

public class DisabledGoogleCalendarServiceTests
{
    private readonly DisabledGoogleCalendarService _service;

    public DisabledGoogleCalendarServiceTests()
    {
        _service = new DisabledGoogleCalendarService(new NullLogger<DisabledGoogleCalendarService>());
    }

    [Fact]
    public async Task CreateEventAsync_ShouldReturnSuccess_WithMockEventId()
    {
        // Act
        var result = await _service.CreateEventAsync(
            "Test Event",
            DateTime.UtcNow,
            DateTime.UtcNow.AddHours(1),
            "Test Description",
            Guid.NewGuid()
        );

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Data);
        Assert.StartsWith("mock_google_event_", result.Data);
    }

    [Fact]
    public async Task UpdateEventStatusAsync_ShouldReturnSuccess()
    {
        // Act
        var result = await _service.UpdateEventStatusAsync("mock_event_id", "Confirmed");

        // Assert
        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task DeleteEventAsync_ShouldReturnSuccess()
    {
        // Act
        var result = await _service.DeleteEventAsync("mock_event_id");

        // Assert
        Assert.True(result.IsSuccess);
    }
}
