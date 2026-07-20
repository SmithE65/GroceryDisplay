using GroceryDisplay.Api;
using GroceryDisplay.Api.Data;
using GroceryDisplay.Api.Data.Entities;
using GroceryDisplay.Api.Endpoints;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.Services.AddOpenApi();

builder.Services.AddDbContext<GroceryDbContext>(options =>
{
    options.UseNpgsql(builder.Configuration.GetConnectionString("grocerydb"))
        .UseSnakeCaseNamingConvention();
});

builder.Services.Configure<ApiKeyOptions>(builder.Configuration.GetSection(ApiKeyOptions.SectionName));

var app = builder.Build();

app.MapDefaultEndpoints();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwaggerUI(options =>
    {
        options.RoutePrefix = "swagger";
        options.SwaggerEndpoint("/openapi/v1.json", "GroceryDisplay API v1");
    });

    using var scope = app.Services.CreateScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<GroceryDbContext>();
    await dbContext.Database.MigrateAsync();
    await SeedDevelopmentDataAsync(dbContext);
}

app.MapPersonEndpoints();
app.MapReceiptEndpoints();

app.UseHttpsRedirection();

app.Run();

static async Task SeedDevelopmentDataAsync(GroceryDbContext db)
{
    if (!await db.People.AnyAsync())
    {
        db.People.AddRange(
            new Person
            {
                PersonId = "eric",
                DisplayName = "Eric",
                IsActive = true,
                SortOrder = 1,
                CreatedAt = DateTimeOffset.UtcNow
            },
            new Person
            {
                PersonId = "brother",
                DisplayName = "Brother",
                IsActive = true,
                SortOrder = 2,
                CreatedAt = DateTimeOffset.UtcNow
            });

        await db.SaveChangesAsync();
    }
}