namespace StockSense.Application.Exceptions;

/// <summary>
/// A checkout rule failure whose message is intentionally safe and actionable for users.
/// </summary>
public sealed class WorkOrderConflictException : InvalidOperationException
{
    public WorkOrderConflictException(string message) : base(message) { }

    public WorkOrderConflictException(string message, Exception innerException)
        : base(message, innerException) { }
}
