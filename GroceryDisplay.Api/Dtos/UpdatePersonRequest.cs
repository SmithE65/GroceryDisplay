namespace GroceryDisplay.Api.Dtos;

public sealed record UpdatePersonRequest(
    string DisplayName,
    bool? Active,
    short? SortOrder);
