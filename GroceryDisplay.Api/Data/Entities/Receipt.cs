namespace GroceryDisplay.Api.Data.Entities;

public sealed class Receipt
{
    public long ReceiptId { get; set; }
    public string PersonId { get; set; } = null!;
    public Person Person { get; set; } = null!;

    public int AmountCents { get; set; }
    public DateOnly PurchasedOn { get; set; }

    public string? StoreName { get; set; }
    public string? Note { get; set; }

    public string? ClientEntryId { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
    public string CreatedBy { get; set; } = null!;

    public DateTimeOffset? UpdatedAt { get; set; }
    public string? UpdatedBy { get; set; }

    public DateTimeOffset? VoidedAt { get; set; }
    public string? VoidedBy { get; set; }
    public string? VoidReason { get; set; }
    public bool IsVoided => VoidedAt is not null;
}
