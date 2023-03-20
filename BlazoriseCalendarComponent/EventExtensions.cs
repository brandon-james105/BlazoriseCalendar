using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace BlazoriseCalendarComponent
{
    public static class EventExtensions
    {
        public static async Task DebounceEvent(this IJSRuntime jsRuntime, ElementReference element, string eventName, TimeSpan delay)
        {
            await using var module = await jsRuntime.InvokeAsync<IJSObjectReference>("import", "./events.js");
            await module.InvokeVoidAsync("debounceEvent", element, eventName, (long)delay.TotalMilliseconds);
        }

        public static async Task ThrottleEvent(this IJSRuntime jsRuntime, ElementReference element, string eventName, TimeSpan delay)
        {
            await using var module = await jsRuntime.InvokeAsync<IJSObjectReference>("import", "./events.js");
            await module.InvokeVoidAsync("throttleEvent", element, eventName, (long)delay.TotalMilliseconds);
        }
    }
}
