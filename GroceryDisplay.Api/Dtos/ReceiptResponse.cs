namespace GroceryDisplay.Api.Dtos;

public sealed record ReceiptResponse(
    long ReceiptId,
    string PersonId,
    int AmountCents,
    DateOnly PurchasedOn,
    string? StoreName,
    string? Note,
    string? ClientEntryId,
    DateTimeOffset CreatedAt,
    string CreatedBy,
    DateTimeOffset? UpdatedAt,
    string? UpdatedBy,
    DateTimeOffset? VoidedAt,
    string? VoidedBy,
    string? VoidReason,
    bool IsVoided);
