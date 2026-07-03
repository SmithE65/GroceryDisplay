namespace GroceryDisplay.Api.Dtos;

public sealed record ReceiptResponse(
    long ReceiptId,
    string PersonId,
    int AmountCents,
    DateOnly PurchasedOn,
    string? StoreName,
    string? Note,
    bool Voided);
