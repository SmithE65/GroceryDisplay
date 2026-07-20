using Microsoft.Extensions.Options;
using System.Security.Cryptography;

namespace GroceryDisplay.Api;

public static class ApiKeyEndpointFilterExtensions
{
    private const string ApiKeyHeaderName = "X-Api-Key";

    public static RouteHandlerBuilder RequireApiKey(
        this RouteHandlerBuilder builder,
        params string[] requiredScopes)
    {
        builder.AddEndpointFilter(CreateFilter(requiredScopes));
        return builder;
    }

    private static Func<EndpointFilterInvocationContext, EndpointFilterDelegate, ValueTask<object?>> CreateFilter(string[] requiredScopes)
    {
        return async (context, next) =>
        {
            var httpContext = context.HttpContext;

            var options = httpContext.RequestServices
                .GetRequiredService<IOptions<ApiKeyOptions>>()
                .Value;

            if (!httpContext.Request.Headers.TryGetValue(ApiKeyHeaderName, out var apiKeyHeaderValues))
            {
                return Results.Unauthorized();
            }

            var suppliedKey = apiKeyHeaderValues.FirstOrDefault();

            if (string.IsNullOrWhiteSpace(suppliedKey))
            {
                return Results.Unauthorized();
            }

            var scopes = GetScopesForKey(options, suppliedKey);

            var allowed = requiredScopes.Any(requiredScope => scopes.Contains(requiredScope, StringComparer.OrdinalIgnoreCase));

            if (!allowed)
            {
                return Results.Forbid();
            }

            httpContext.Items["ApiKeyScopes"] = scopes;

            return await next(context);
        };
    }

    private static string[] GetScopesForKey(ApiKeyOptions options, string suppliedKey)
    {
        if (IsConfiguredKey(suppliedKey, options.Admin))
        {
            return [ApiScopes.Read, ApiScopes.Write, ApiScopes.Admin];
        }

        if (IsConfiguredKey(suppliedKey, options.Write))
        {
            return [ApiScopes.Read, ApiScopes.Write];
        }

        if (IsConfiguredKey(suppliedKey, options.Read))
        {
            return [ApiScopes.Read];
        }

        return [];
    }

    private static bool IsConfiguredKey(string suppliedKey, string? configuredKey)
    {
        return !string.IsNullOrWhiteSpace(suppliedKey)
            && CryptographicEquals(suppliedKey, configuredKey);
    }

    private static bool CryptographicEquals(string a, string? b)
    {
        if (a is null || b is null)
        {
            return false;
        }
        var aBytes = System.Text.Encoding.UTF8.GetBytes(a);
        var bBytes = System.Text.Encoding.UTF8.GetBytes(b);
        return CryptographicOperations.FixedTimeEquals(aBytes, bBytes);
    }
}

public static class ApiScopes
{
    public const string Read = "read";
    public const string Write = "write";
    public const string Admin = "admin";
}

public class ApiKeyOptions
{
    public const string SectionName = "ApiKeys";

    public string? Read { get; init; }
    public string? Write { get; init; }
    public string? Admin { get; init; }
}
