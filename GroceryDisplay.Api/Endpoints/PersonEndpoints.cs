using GroceryDisplay.Api.Data;
using GroceryDisplay.Api.Data.Entities;
using GroceryDisplay.Api.Dtos;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GroceryDisplay.Api.Endpoints;

public static class PersonEndpoints
{
    public static void MapPersonEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/people")
            .WithTags("People");

        group.MapGet("/", GetPeople)
            .WithName("GetPeople")
            .Produces<List<Person>>(StatusCodes.Status200OK);

        group.MapGet("/{personId}", GetPersonById)
            .WithName("GetPersonById")
            .Produces<Person>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound);

        group.MapPost("/", CreatePerson)
            .WithName("CreatePerson")
            .Produces<Person>(StatusCodes.Status201Created)
            .Produces(StatusCodes.Status400BadRequest);

        group.MapPatch("/{personId}", UpdatePerson)
            .WithName("UpdatePerson")
            .Produces<Person>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status404NotFound);

        group.MapPost("/{personId}/activate", ActivatePerson)
            .WithName("ActivatePerson")
            .Produces<Person>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound);

        group.MapPost("/{personId}/deactivate", DeactivatePerson)
            .WithName("DeactivatePerson")
            .Produces<Person>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound);
    }

    private static async Task<IResult> GetPeople(
        [FromQuery] bool? includeInactive,
        [FromServices] GroceryDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var query = dbContext.People.AsNoTracking();

        if (includeInactive != true)
        {
            query = query.Where(p => p.IsActive);
        }

        var people = await query
            .OrderBy(p => p.SortOrder)
            .ThenBy(p => p.DisplayName)
            .Select(p => ToResponse(p))
            .ToListAsync(cancellationToken);

        return Results.Ok(people);
    }

    private static async Task<IResult> GetPersonById(
        [FromRoute] string personId,
        [FromServices] GroceryDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var normalizedPersonId = NormalizePersonId(personId);

        var person = await dbContext.People
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.PersonId == normalizedPersonId, cancellationToken);

        if (person is null)
        {
            return Results.NotFound();
        }

        return Results.Ok(ToResponse(person));
    }

    private static async Task<IResult> CreatePerson(
        [FromBody] CreatePersonRequest request,
        [FromServices] GroceryDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var validationResult = ValidateCreatePerson(request);

        if (validationResult is not null)
        {
            return validationResult;
        }

        var normalizedPersonId = NormalizePersonId(request.PersonId);

        var existingPerson = await dbContext.People
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.PersonId == normalizedPersonId, cancellationToken);

        if (existingPerson is not null)
        {
            return Results.Conflict(new ErrorResponse($"A person with personId '{normalizedPersonId}' already exists."));
        }

        var newPerson = new Person
        {
            PersonId = normalizedPersonId,
            DisplayName = request.DisplayName,
            IsActive = true,
            SortOrder = 0,
            CreatedAt = DateTimeOffset.UtcNow
        };

        dbContext.People.Add(newPerson);
        await dbContext.SaveChangesAsync(cancellationToken);

        return Results.Created($"/api/v1/people/{normalizedPersonId}", ToResponse(newPerson));
    }

    private static async Task<IResult> UpdatePerson(
        [FromRoute] string personId,
        [FromBody] UpdatePersonRequest request,
        [FromServices] GroceryDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var validationError = ValidateUpdatePerson(request);

        if (validationError is not null)
        {
            return validationError;
        }

        var normalizedPersonId = NormalizePersonId(personId);

        var person = await dbContext.People
            .FirstOrDefaultAsync(p => p.PersonId == normalizedPersonId, cancellationToken);

        if (person is null)
        {
            return Results.NotFound();
        }

        if (request.DisplayName is not null)
        {
            person.DisplayName = request.DisplayName.Trim();
        }

        if (request.SortOrder is not null)
        {
            person.SortOrder = request.SortOrder.Value;
        }

        if (request.Active is not null)
        {
            person.IsActive = request.Active.Value;
        }

        person.UpdatedAt = DateTimeOffset.UtcNow;

        await dbContext.SaveChangesAsync(cancellationToken);
        return Results.Ok(ToResponse(person));
    }

    private static Task<IResult> ActivatePerson(
        [FromRoute] string personId,
        [FromServices] GroceryDbContext dbContext,
        CancellationToken cancellationToken)
    {
        return SetPersonActiveState(
            personId,
            active: true,
            dbContext,
            cancellationToken);
    }

    private static Task<IResult> DeactivatePerson(
        [FromRoute] string personId,
        [FromServices] GroceryDbContext dbContext,
        CancellationToken cancellationToken)
    {
        return SetPersonActiveState(
            personId,
            active: false,
            dbContext,
            cancellationToken);
    }

    private static async Task<IResult> SetPersonActiveState(
        string personId,
        bool active,
        GroceryDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var normalizedPersonId = NormalizePersonId(personId);

        var person = await dbContext.People
            .FirstOrDefaultAsync(p => p.PersonId == normalizedPersonId, cancellationToken);

        if (person is null)
        {
            return Results.NotFound();
        }

        person.IsActive = active;
        person.UpdatedAt = DateTimeOffset.UtcNow;

        await dbContext.SaveChangesAsync(cancellationToken);

        return Results.Ok(ToResponse(person));
    }

    private static IResult? ValidateCreatePerson(CreatePersonRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.PersonId))
        {
            return Results.BadRequest(new ErrorResponse("personId is required."));
        }

        if (!IsValidPersonId(request.PersonId))
        {
            return Results.BadRequest(new ErrorResponse("personId is invalid. It must be 2-32 characters long, start with a lowercase letter, and only contain lowercase letters, digits, hyphens, or underscores."));
        }

        if (string.IsNullOrWhiteSpace(request.DisplayName))
        {
            return Results.BadRequest(new ErrorResponse("displayName is required."));
        }

        return null;
    }

    private static IResult? ValidateUpdatePerson(UpdatePersonRequest request)
    {
        if (request.DisplayName is null && string.IsNullOrWhiteSpace(request.DisplayName))
        {
            return Results.BadRequest(new ErrorResponse("displayName cannot be blank."));
        }

        return null;
    }

    private static bool IsValidPersonId(string personId)
    {
        var normalized = NormalizePersonId(personId);

        if (normalized.Length is < 2 or > 32)
        {
            return false;
        }

        if (!char.IsAsciiLetterLower(normalized[0]))
        {
            return false;
        }

        foreach (var c in normalized)
        {
            var valid =
                char.IsAsciiLetterLower(c) ||
                char.IsAsciiDigit(c) ||
                c is '-' or '_';

            if (!valid)
            {
                return false;
            }
        }

        return true;
    }

    private static string NormalizePersonId(string personId) => personId.Trim().ToLowerInvariant();

    private static PersonResponse ToResponse(Person person)
    {
        return new PersonResponse(
            PersonId: person.PersonId,
            DisplayName: person.DisplayName,
            IsActive: person.IsActive,
            SortOrder: person.SortOrder,
            CreatedAt: person.CreatedAt,
            UpdatedAt: person.UpdatedAt
        );
    }
}
