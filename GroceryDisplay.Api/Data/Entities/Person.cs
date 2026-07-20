namespace GroceryDisplay.Api.Data.Entities;

public sealed class Person
{
    public required string PersonId { get; set; }
    public required string DisplayName { get; set; }
    public bool IsActive { get; set; } = true;
    public short SortOrder { get; set; } = 0;
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
    public List<Receipt> Receipts { get; set; } = [];
}
