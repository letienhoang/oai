using Microsoft.JSInterop;

namespace OAI.Web.Services;

public sealed class UserTimeZoneService
{
    private readonly IJSRuntime _jsRuntime;
    private string? _timeZoneId;
    private TimeZoneInfo? _timeZoneInfo;

    public UserTimeZoneService(IJSRuntime jsRuntime)
    {
        _jsRuntime = jsRuntime;
    }

    public async Task<TimeZoneInfo> GetUserTimeZoneAsync()
    {
        if (_timeZoneInfo is not null)
            return _timeZoneInfo;

        _timeZoneId = await _jsRuntime.InvokeAsync<string>("oaiTimeZone.getTimeZone");

        try
        {
            _timeZoneInfo = TimeZoneInfo.FindSystemTimeZoneById(_timeZoneId);
        }
        catch
        {
            _timeZoneInfo = TimeZoneInfo.Utc;
        }

        return _timeZoneInfo;
    }

    public async Task<DateTimeOffset> ToUserLocalTimeAsync(DateTimeOffset utcDateTime)
    {
        var timeZone = await GetUserTimeZoneAsync();
        return TimeZoneInfo.ConvertTime(utcDateTime, timeZone);
    }
}