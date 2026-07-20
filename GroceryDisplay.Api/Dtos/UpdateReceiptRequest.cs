namespace GroceryDisplay.Api.Dtos;

public sealed record UpdateReceiptRequest(
    string? PersonId,
    int? AmountCents,
    DateOnly? PurchasedOn,
    string? StoreName,
    string? Note);