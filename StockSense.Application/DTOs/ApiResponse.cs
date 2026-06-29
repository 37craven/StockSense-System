namespace StockSense.Application.DTOs;

public static class ApiResponse
{
    public static object Error(string message) => new { error = message };

    public static object NotFound(string resource = "Resource") => new { error = $"{resource} not found." };

    public static object Success(string message, object? data = null) =>
        data is null ? new { message } : new { message, data };
}
