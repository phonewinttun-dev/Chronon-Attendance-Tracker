using System;
using System.Threading;
using System.Threading.Tasks;
using ACST.Shared;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Auth.OAuth2.Flows;
using Google.Apis.Auth.OAuth2.Requests;
using Google.Apis.Calendar.v3;
using Google.Apis.Calendar.v3.Data;
using Google.Apis.Services;
using Google.Apis.Util.Store;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace ACST.Domain.Features.GoogleCalendar;

public class GoogleCalendarService : IGoogleCalendarService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<GoogleCalendarService> _logger;
    private readonly string _calendarId;
    private CalendarService? _calendarService;

    public GoogleCalendarService(IConfiguration configuration, ILogger<GoogleCalendarService> logger)
    {
        _configuration = configuration;
        _logger = logger;
        _calendarId = configuration["GoogleCalendar:CalendarId"] ?? "primary";
    }

    #region Helper Methods

    private async Task<CalendarService> GetCalendarServiceAsync()
    {
        if (_calendarService != null)
        {
            return _calendarService;
        }

        var section = _configuration.GetSection("GoogleCalendar");
        var clientId = section["ClientId"] ?? throw new InvalidOperationException("GoogleCalendar:ClientId configuration is missing.");
        var clientSecret = section["ClientSecret"] ?? throw new InvalidOperationException("GoogleCalendar:ClientSecret configuration is missing.");

        var flow = new GoogleAuthorizationCodeFlow(new GoogleAuthorizationCodeFlow.Initializer
        {
            ClientSecrets = new ClientSecrets
            {
                ClientId = clientId,
                ClientSecret = clientSecret
            },
            Scopes = new[] { CalendarService.Scope.Calendar },
            DataStore = new FileDataStore("ChrononGoogleAuthStore")
        });

        var token = await flow.LoadTokenAsync("user", CancellationToken.None);
        if (token == null)
        {
            throw new InvalidOperationException("User is not connected to Google Calendar. Please authorize first.");
        }

        var credential = new UserCredential(flow, "user", token);

        // Handle token expiration and automatic token refreshing
        if (credential.Token.IsStale)
        {
            _logger.LogInformation("Google Calendar access token is expired/stale. Refreshing token.");
            bool refreshed = await credential.RefreshTokenAsync(CancellationToken.None);
            if (!refreshed)
            {
                throw new InvalidOperationException("Failed to refresh Google Calendar token. Please reconnect.");
            }
        }

        _calendarService = new CalendarService(new BaseClientService.Initializer
        {
            HttpClientInitializer = credential,
            ApplicationName = section["ApplicationName"] ?? "Chronon Attendance Tracker"
        });

        return _calendarService;
    }

    #endregion

    #region Connection Management

    public async Task<Result<bool>> IsConnectedAsync()
    {
        try
        {
            var section = _configuration.GetSection("GoogleCalendar");
            var clientId = section["ClientId"];
            var clientSecret = section["ClientSecret"];
            if (string.IsNullOrEmpty(clientId) || string.IsNullOrEmpty(clientSecret))
            {
                return Result<bool>.Success(false, "Google Calendar configuration is missing credentials.");
            }

            var flow = new GoogleAuthorizationCodeFlow(new GoogleAuthorizationCodeFlow.Initializer
            {
                ClientSecrets = new ClientSecrets
                {
                    ClientId = clientId,
                    ClientSecret = clientSecret
                },
                Scopes = new[] { CalendarService.Scope.Calendar },
                DataStore = new FileDataStore("ChrononGoogleAuthStore")
            });

            var token = await flow.LoadTokenAsync("user", CancellationToken.None);
            if (token == null)
            {
                return Result<bool>.Success(false, "No token found in store.");
            }

            var credential = new UserCredential(flow, "user", token);
            if (credential.Token.IsStale)
            {
                try
                {
                    bool refreshed = await credential.RefreshTokenAsync(CancellationToken.None);
                    if (!refreshed)
                    {
                        return Result<bool>.Success(false, "Token is expired/stale and could not be refreshed.");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to refresh token during status check.");
                    return Result<bool>.Success(false, $"Token is expired/stale and refresh failed: {ex.Message}");
                }
            }

            return Result<bool>.Success(true, "Token is present and valid.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking Google Calendar connection status.");
            return Result<bool>.Failure($"Error checking connection status: {ex.Message}");
        }
    }

    public Task<Result<string>> GetAuthorizationUrlAsync(string redirectUri, string? state)
    {
        try
        {
            var section = _configuration.GetSection("GoogleCalendar");
            var clientId = section["ClientId"] ?? throw new InvalidOperationException("GoogleCalendar:ClientId configuration is missing.");
            var clientSecret = section["ClientSecret"] ?? throw new InvalidOperationException("GoogleCalendar:ClientSecret configuration is missing.");

            var flow = new GoogleAuthorizationCodeFlow(new GoogleAuthorizationCodeFlow.Initializer
            {
                ClientSecrets = new ClientSecrets
                {
                    ClientId = clientId,
                    ClientSecret = clientSecret
                },
                Scopes = new[] { CalendarService.Scope.Calendar },
                DataStore = new FileDataStore("ChrononGoogleAuthStore")
            });

            var request = (GoogleAuthorizationCodeRequestUrl)flow.CreateAuthorizationCodeRequest(redirectUri);
            request.AccessType = "offline";
            request.ApprovalPrompt = "force";
            if (!string.IsNullOrEmpty(state))
            {
                request.State = state;
            }

            return Task.FromResult(Result<string>.Success(request.Build().ToString(), "Authorization URL generated successfully."));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate authorization URL.");
            return Task.FromResult(Result<string>.Failure($"Failed to generate authorization URL: {ex.Message}"));
        }
    }

    public async Task<Result> ExchangeCodeAndStoreTokenAsync(string code, string redirectUri)
    {
        try
        {
            var section = _configuration.GetSection("GoogleCalendar");
            var clientId = section["ClientId"] ?? throw new InvalidOperationException("GoogleCalendar:ClientId configuration is missing.");
            var clientSecret = section["ClientSecret"] ?? throw new InvalidOperationException("GoogleCalendar:ClientSecret configuration is missing.");

            var flow = new GoogleAuthorizationCodeFlow(new GoogleAuthorizationCodeFlow.Initializer
            {
                ClientSecrets = new ClientSecrets
                {
                    ClientId = clientId,
                    ClientSecret = clientSecret
                },
                Scopes = new[] { CalendarService.Scope.Calendar },
                DataStore = new FileDataStore("ChrononGoogleAuthStore")
            });

            await flow.ExchangeCodeForTokenAsync("user", code, redirectUri, CancellationToken.None);
            
            // Force re-initialization of calendar service with the new token
            _calendarService = null;

            return Result.Success("Token exchanged and stored successfully.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to exchange authorization code.");
            return Result.Failure($"Failed to exchange authorization code: {ex.Message}");
        }
    }

    public async Task<Result> DisconnectAsync()
    {
        try
        {
            var section = _configuration.GetSection("GoogleCalendar");
            var clientId = section["ClientId"];
            var clientSecret = section["ClientSecret"];
            if (string.IsNullOrEmpty(clientId) || string.IsNullOrEmpty(clientSecret))
            {
                return Result.Failure("Google Calendar configuration is missing credentials.");
            }

            var flow = new GoogleAuthorizationCodeFlow(new GoogleAuthorizationCodeFlow.Initializer
            {
                ClientSecrets = new ClientSecrets
                {
                    ClientId = clientId,
                    ClientSecret = clientSecret
                },
                Scopes = new[] { CalendarService.Scope.Calendar },
                DataStore = new FileDataStore("ChrononGoogleAuthStore")
            });

            await flow.DeleteTokenAsync("user", CancellationToken.None);
            _calendarService = null;

            return Result.Success("Disconnected successfully.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to disconnect Google Calendar.");
            return Result.Failure($"Failed to disconnect Google Calendar: {ex.Message}");
        }
    }

    #endregion

    #region CreateEvent

    public async Task<Result<string>> CreateEventAsync(string title, DateTime startUtc, DateTime endUtc, string description, Guid sessionToken)
    {
        try
        {
            var service = await GetCalendarServiceAsync();

            var ev = new Event
            {
                Summary = title,
                Description = description,
                Start = new EventDateTime
                {
                    DateTimeDateTimeOffset = new DateTimeOffset(startUtc, TimeSpan.Zero)
                },
                End = new EventDateTime
                {
                    DateTimeDateTimeOffset = new DateTimeOffset(endUtc, TimeSpan.Zero)
                }
            };

            var request = service.Events.Insert(ev, _calendarId);
            var createdEvent = await request.ExecuteAsync();

            return Result<string>.Success(createdEvent.Id, "Google Calendar event created successfully.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create Google Calendar event for: {Title}", title);
            return Result<string>.Failure($"Failed to create Google Calendar event: {ex.Message}");
        }
    }

    #endregion

    #region UpdateEvent

    public async Task<Result> UpdateEventStatusAsync(string googleEventId, string status)
    {
        try
        {
            var service = await GetCalendarServiceAsync();
            var ev = await service.Events.Get(_calendarId, googleEventId).ExecuteAsync();

            string googleStatus = status.ToLowerInvariant() switch
            {
                "cancelled" => "cancelled",
                "confirmed" => "confirmed",
                "present" => "confirmed",
                "absent" => "confirmed",
                "late" => "confirmed",
                _ => "confirmed"
            };

            ev.Status = googleStatus;

            await service.Events.Update(ev, _calendarId, googleEventId).ExecuteAsync();
            return Result.Success("Google Calendar event status updated successfully.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update Google Calendar event status for event ID: {EventId}", googleEventId);
            return Result.Failure($"Failed to update Google Calendar event status: {ex.Message}");
        }
    }

    #endregion

    #region DeleteEvent

    public async Task<Result> DeleteEventAsync(string googleEventId)
    {
        try
        {
            var service = await GetCalendarServiceAsync();
            await service.Events.Delete(_calendarId, googleEventId).ExecuteAsync();
            return Result.Success("Google Calendar event deleted successfully.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete Google Calendar event for event ID: {EventId}", googleEventId);
            return Result.Failure($"Failed to delete Google Calendar event: {ex.Message}");
        }
    }

    #endregion
}
