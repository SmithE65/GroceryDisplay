namespace GroceryDisplay.Api.Dtos;

public sealed record CreateReceiptRequest(
    string PersonId,
    int AmountCents,
    DateOnly PurchasedOn,
    string? StoreName,
    string? Note,
    string? ClientEntryId);
