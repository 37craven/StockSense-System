using System.Reflection;
using Microsoft.JSInterop;
using StockSense.Client.Components;

namespace StockSense.Tests;

public sealed class AssistanceChatLifecycleTests
{
    public static TheoryData<Exception, bool> NavigationExceptions => new()
    {
        { new OperationCanceledException(), true },
        { new TaskCanceledException(), true },
        { new JSDisconnectedException("The JavaScript runtime disconnected."), true },
        { new ObjectDisposedException("component"), true },
        { new HttpRequestException("The assistance endpoint failed."), false },
        { new InvalidOperationException("Application defect"), false }
    };

    [Theory]
    [MemberData(nameof(NavigationExceptions))]
    public void Expected_navigation_interruptions_are_limited_to_teardown_failures(
        Exception exception,
        bool expected)
    {
        var method = typeof(AssistanceChat).GetMethod(
            "IsExpectedNavigationInterruption",
            BindingFlags.Static | BindingFlags.NonPublic);

        Assert.NotNull(method);
        Assert.Equal(expected, method.Invoke(null, [exception]));
    }
}
