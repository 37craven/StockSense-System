namespace StockSense.Application.DTOs;

public record OperationResult(
    bool IsSuccess,
    string? ErrorCode = null,
    string? ErrorMessage = null,
    bool IsConcurrencyConflict = false)
{
    public static OperationResult Success() => new(true);

    public static OperationResult Failure(
        string errorCode,
        string errorMessage,
        bool isConcurrencyConflict = false) =>
        new(false, errorCode, errorMessage, isConcurrencyConflict);
}

public sealed record OperationResult<T>(
    bool IsSuccess,
    T? Value = default,
    string? ErrorCode = null,
    string? ErrorMessage = null,
    bool IsConcurrencyConflict = false)
{
    public static OperationResult<T> Success(T value) => new(true, value);

    public static OperationResult<T> Failure(
        string errorCode,
        string errorMessage,
        bool isConcurrencyConflict = false) =>
        new(false, default, errorCode, errorMessage, isConcurrencyConflict);
}
