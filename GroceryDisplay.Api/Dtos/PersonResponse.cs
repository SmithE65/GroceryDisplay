namespace GroceryDisplay.Api.Dtos;

public sealed record PersonResponse(
    string PersonId,
    string DisplayName,
    bool IsActive,
    short SortOrder,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt);