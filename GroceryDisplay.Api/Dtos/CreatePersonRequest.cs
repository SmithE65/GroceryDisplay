namespace GroceryDisplay.Api.Dtos;

public sealed record CreatePersonRequest(
    string PersonId,
    string DisplayName,
    bool? Active,
    short? SortOrder);
