using GroceryDisplay.Api.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace GroceryDisplay.Api.Data;

public sealed class GroceryDbContext(DbContextOptions<GroceryDbContext> options) : DbContext(options)
{
    public DbSet<Person> People { get; set; } = null!;
    public DbSet<Receipt> Receipts { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("grocery");

        ConfigurePerson(modelBuilder);
        ConfigureReceipt(modelBuilder);
    }

    private static void ConfigurePerson(ModelBuilder modelBuilder)
    {
        var entity = modelBuilder.Entity<Person>();
        entity.ToTable("person", t => t.HasCheckConstraint("ck_person_id_format", "person_id ~ '^[a-z][a-z0-9_-]{1,31}$'"));
        entity.HasKey(e => e.PersonId);

        entity.Property(e => e.PersonId)
            .HasColumnType("text")
            .HasMaxLength(32);

        entity.Property(e => e.DisplayName)
            .HasColumnType("text")
            .IsRequired();

        entity.Property(e => e.IsActive)
            .HasDefaultValue(true);

        entity.Property(e => e.SortOrder)
            .HasDefaultValue(0);

        entity.Property(e => e.CreatedAt)
            .HasColumnType("timestamp with time zone")
            .HasDefaultValueSql("now()");
    }

    private static void ConfigureReceipt(ModelBuilder modelBuilder)
    {
        var entity = modelBuilder.Entity<Receipt>();

        entity.ToTable(
            "receipt", 
            t =>
            {
                t.HasCheckConstraint("ck_receipt_amount_cents_positive", "amount_cents >= 0");
                t.HasCheckConstraint(
                    "ck_receipt_void_consistency",
                    """
                    (
                        voided_at is null
                        and voided_by is null
                        and void_reason is null
                    )
                    or
                    (
                        voided_at is not null
                        and voided_by is not null
                        and void_reason is not null
                    )
                    """);
            });

        entity.HasKey(e => e.ReceiptId);

        entity.Property(e => e.ReceiptId)
            .UseIdentityAlwaysColumn();

        entity.Property(e => e.PersonId)
            .HasColumnType("text")
            .HasMaxLength(32)
            .IsRequired();

        entity.HasOne(e => e.Person)
            .WithMany(p => p.Receipts)
            .HasForeignKey(e => e.PersonId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.Property(e => e.AmountCents)
            .IsRequired();

        entity.Property(e => e.PurchasedOn)
            .HasColumnType("date")
            .IsRequired();

        entity.Property(e => e.StoreName)
            .HasColumnType("text");

        entity.Property(e => e.Note)
            .HasColumnType("text");

        entity.Property(e => e.ClientEntryId)
            .HasColumnType("text")
            .HasMaxLength(64);

        entity.Property(e => e.CreatedAt)
            .HasColumnType("timestamp with time zone")
            .HasDefaultValueSql("now()");

        entity.Property(e => e.CreatedBy)
            .HasColumnType("text")
            .HasDefaultValue("api")
            .IsRequired();

        entity.Property(e => e.UpdatedAt)
            .HasColumnType("timestamp with time zone");

        entity.Property(e => e.UpdatedBy)
            .HasColumnType("text");

        entity.Property(e => e.VoidedAt)
            .HasColumnType("timestamp with time zone");

        entity.Property(e => e.VoidedBy)
            .HasColumnType("text");

        entity.Property(e => e.VoidReason)
            .HasColumnType("text");

        entity.HasIndex(e => e.PurchasedOn)
            .HasDatabaseName("ix_receipt_purchased_on");

        entity.HasIndex(e => new {e.PersonId, e.PurchasedOn })
            .HasDatabaseName("ix_receipt_person_purchased_on");

        entity.HasIndex(e => new { e.PurchasedOn, e.PersonId })
            .HasDatabaseName("ix_receipt_active_year_lookup")
            .HasFilter("voided_at is null");

        entity.HasIndex(e => e.ClientEntryId)
            .HasDatabaseName("ix_receipt_client_entry_id")
            .IsUnique();
    }
}
