using GroceryDisplay.Api.Data;
using GroceryDisplay.Api.Data.Entities;
using GroceryDisplay.Api.Dtos;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GroceryDisplay.Api.Endpoints;

public static class ReceiptEndpoints
{
    public static void MapReceiptEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/receipts")
            .WithTags("Receipts");

        group.MapGet("/", GetReceipts)
            .WithName("GetReceipts")
            .Produces<List<Receipt>>(StatusCodes.Status200OK);

        group.MapGet("/{receiptId:long}", GetReceiptById)
            .WithName("GetReceiptById")
            .Produces<Receipt>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound);

        group.MapPost("/", CreateReceipt)
            .WithName("CreateReceipt")
            .Produces<Receipt>(StatusCodes.Status201Created);

        group.MapPatch("/{receiptId:long}", UpdateReceipt)
            .WithName("UpdateReceipt")
            .Produces<Receipt>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound);

        group.MapPost("/{receiptId:long}/void", VoidReceipt)
            .WithName("VoidReceipt")
            .Produces<Receipt>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound);
    }

    private static async Task<IResult> GetReceipts(
        [FromQuery] int? year,
        [FromQuery] int? take,
        [FromQuery] int? skip,
        [FromQuery] bool? includeVoided,
        [FromServices] GroceryDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var selectedYear = year ?? DateTime.UtcNow.Year;
        var maxTake = Math.Clamp(take ?? 5, 1, 100);

        var start = new DateOnly(selectedYear, 1, 1);
        var end = new DateOnly(selectedYear + 1, 1, 1);

        var query = dbContext.Receipts
            .AsNoTracking()
            .Where(r => r.PurchasedOn >= start && r.PurchasedOn < end);

        if (includeVoided != true)
        {
            query = query.Where(r => !r.IsVoided);
        }

        var receipts = await query
            .OrderByDescending(r => r.PurchasedOn)
            .ThenByDescending(r => r.ReceiptId)
            .Skip(skip ?? 0)
            .Take(maxTake)
            .Select(r => ToResponse(r))
            .ToListAsync(cancellationToken);

        return Results.Ok(receipts);
    }

    private static async Task<IResult> GetReceiptById(
        [FromRoute] long receiptId,
        [FromServices] GroceryDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var receipt = await dbContext.Receipts
            .AsNoTracking()
            .Where(r => r.ReceiptId == receiptId)
            .Select(r => ToResponse(r))
            .SingleOrDefaultAsync(cancellationToken);

        if (receipt is null)
        {
            return Results.NotFound();
        }

        return Results.Ok(receipt);
    }

    private static async Task<IResult> CreateReceipt(
        [FromBody] CreateReceiptRequest request,
        [FromServices] GroceryDbContext dbContext,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        var validationError = ValidateCreateReceipt(request);

        if (validationError is not null)
        {
            return validationError;
        }

        var personId = request.PersonId.Trim();
        var clientEntryId = NormalizeOptionalText(request.ClientEntryId);

        if (clientEntryId is not null)
        {
            var existingReceipt = await dbContext.Receipts
                .AsNoTracking()
                .Where(r => r.PersonId == personId && r.ClientEntryId == clientEntryId)
                .Select(r => ToResponse(r))
                .SingleOrDefaultAsync(cancellationToken);

            if (existingReceipt is not null)
            {
                return Results.Ok(existingReceipt);
            }
        }

        var personExists = await dbContext.People
            .AsNoTracking()
            .AnyAsync(p => p.PersonId == personId, cancellationToken);

        if (!personExists)
        {
            return Results.BadRequest(new ErrorResponse($"Unknown or inactive personId: {personId}"));
        }

        var now = DateTimeOffset.UtcNow;
        var actor = GetActor(httpContext);

        var receipt = new Receipt
        {
            PersonId = personId,
            AmountCents = request.AmountCents,
            PurchasedOn = request.PurchasedOn,
            StoreName = NormalizeOptionalText(request.StoreName),
            Note = NormalizeOptionalText(request.Note),
            ClientEntryId = clientEntryId,
            CreatedAt = now,
            CreatedBy = actor,
            UpdatedAt = null,
            UpdatedBy = null
        };

        dbContext.Receipts.Add(receipt);
        await dbContext.SaveChangesAsync(cancellationToken);

        var response = ToResponse(receipt);
        return Results.CreatedAtRoute("GetReceiptById", new { receiptId = receipt.ReceiptId }, response);
    }

    private static async Task<IResult> UpdateReceipt(
        [FromRoute] long receiptId,
        [FromBody] UpdateReceiptRequest request,
        [FromServices] GroceryDbContext dbContext,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        var validationError = ValidateUpdateReceipt(request);

        if (validationError is not null)
        {
            return validationError;
        }

        var receipt = await dbContext.Receipts
            .SingleOrDefaultAsync(r => r.ReceiptId == receiptId, cancellationToken);

        if (receipt is null)
        {
            return Results.NotFound();
        }

        if (receipt.IsVoided)
        {
            return Results.BadRequest(new ErrorResponse("Cannot update a voided receipt."));
        }

        if (!string.IsNullOrWhiteSpace(request.PersonId))
        {
            var personId = request.PersonId.Trim();

            var personExists = await dbContext.People
                .AsNoTracking()
                .AnyAsync(p => p.PersonId == personId, cancellationToken);

            if (!personExists)
            {
                return Results.BadRequest(new ErrorResponse($"Unknown or inactive personId: {personId}"));
            }

            receipt.PersonId = personId;
        }

        if (request.AmountCents is not null)
        {
            receipt.AmountCents = request.AmountCents.Value;
        }

        if (request.PurchasedOn is not null)
        {
            receipt.PurchasedOn = request.PurchasedOn.Value;
        }

        if (request.StoreName is not null)
        {
            receipt.StoreName = NormalizeOptionalText(request.StoreName);
        }

        if (request.Note is not null)
        {
            receipt.Note = NormalizeOptionalText(request.Note);
        }

        receipt.UpdatedAt = DateTimeOffset.UtcNow;
        receipt.UpdatedBy = GetActor(httpContext);

        await dbContext.SaveChangesAsync(cancellationToken);

        var response = ToResponse(receipt);
        return Results.Ok(response);
    }

    public static async Task<IResult> VoidReceipt(
        [FromRoute] long receiptId,
        [FromBody] VoidReceiptRequest request,
        [FromServices] GroceryDbContext dbContext,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Reason))
        {
            return Results.BadRequest(new ErrorResponse("Void reason is required."));
        }

        var receipt = await dbContext.Receipts
            .SingleOrDefaultAsync(r => r.ReceiptId == receiptId, cancellationToken);

        if (receipt is null)
        {
            return Results.NotFound();
        }

        if (receipt.IsVoided)
        {
            return Results.Ok(ToResponse(receipt));
        }

        receipt.VoidedAt = DateTimeOffset.UtcNow;
        receipt.VoidedBy = GetActor(httpContext);
        receipt.VoidReason = NormalizeOptionalText(request.Reason);
        receipt.UpdatedAt = receipt.VoidedAt;
        receipt.UpdatedBy = receipt.VoidedBy;

        await dbContext.SaveChangesAsync(cancellationToken);

        var response = ToResponse(receipt);
        return Results.Ok(response);
    }

    private static string GetActor(HttpContext httpContext)
    {
        return httpContext.User.Identity?.Name ?? "api";
    }

    public static string? NormalizeOptionalText(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return null;
        }

        return text.Trim();
    }

    private static ReceiptResponse ToResponse(Receipt receipt)
    {
        return new ReceiptResponse(
            ReceiptId: receipt.ReceiptId,
            PersonId: receipt.PersonId,
            AmountCents: receipt.AmountCents,
            PurchasedOn: receipt.PurchasedOn,
            StoreName: receipt.StoreName,
            Note: receipt.Note,
            ClientEntryId: receipt.ClientEntryId,
            CreatedAt: receipt.CreatedAt,
            CreatedBy: receipt.CreatedBy,
            UpdatedAt: receipt.UpdatedAt,
            UpdatedBy: receipt.UpdatedBy,
            VoidedAt: receipt.VoidedAt,
            VoidedBy: receipt.VoidedBy,
            VoidReason: receipt.VoidReason,
            IsVoided: receipt.IsVoided
        );
    }

    private static IResult? ValidateCreateReceipt(CreateReceiptRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.PersonId))
        {
            return Results.BadRequest(new ErrorResponse("personId is required."));
        }

        if (request.AmountCents <= 0)
        {
            return Results.BadRequest(new ErrorResponse("AmountCents must be greater than zero."));
        }

        return null;
    }

    private static IResult? ValidateUpdateReceipt(UpdateReceiptRequest request)
    {
        if (request.AmountCents is <= 0)
        {
            return Results.BadRequest(new ErrorResponse("AmountCents must be greater than zero."));
        }

        return null;
    }
}
