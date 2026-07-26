using StockSense.Application.DTOs;

namespace StockSense.Application.Interfaces;

public interface IOrderSlipWorkflowService
{
    Task<OperationResult<OrderSlipPreviewDto>> PreviewAsync(
        string locationId,
        CancellationToken cancellationToken = default);

    Task<OperationResult<CreateDraftOrderSlipsResult>> CreateDraftsAsync(
        CreateOrderSlipDraftsCommand command,
        CancellationToken cancellationToken = default);

    Task<OperationResult<OrderSlipDto>> ApproveAsync(
        OrderSlipTransitionCommand command,
        CancellationToken cancellationToken = default);

    Task<OperationResult<OrderSlipDto>> MarkOrderedAsync(
        OrderSlipTransitionCommand command,
        CancellationToken cancellationToken = default);

    Task<OperationResult<OrderSlipDto>> CancelAsync(
        CancelOrderSlipCommand command,
        CancellationToken cancellationToken = default);

    Task<OperationResult<OrderSlipReceiptResult>> ReceiveAsync(
        ReceiveOrderSlipCommand command,
        CancellationToken cancellationToken = default);
}
