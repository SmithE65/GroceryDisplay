namespace GroceryDisplay.Api.Dtos;

public sealed record DashboardPersonResponse(
    string PersonId,
    string DisplayName,
    int ReceiptCount,
    long TotalCents,
    DateOnly? LastPurchaseOn,
    bool IsNextBuyer);
