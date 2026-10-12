namespace PersonalBudget.Api.Extensions;

using System.Globalization;
using System.Threading.RateLimiting;
using PersonalBudget.Api.Contracts;

public static class RateLimitingExtensions
{
    public const string AuthPolicy = "auth";

    private const int DefaultPermitLimit = 5;
    private const int DefaultWindowSeconds = 60;

    public static IServiceCollection AddAuthRateLimiting(
        this IServiceCollection services)
    {
        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

            options.OnRejected = async (context, cancellationToken) =>
            {
                if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
                {
                    context.HttpContext.Response.Headers.RetryAfter =
                        ((int)Math.Ceiling(retryAfter.TotalSeconds)).ToString(CultureInfo.InvariantCulture);
                }

                await context.HttpContext.Response.WriteAsJsonAsync(
                    ApiResponse<object?>.Fail("Muitas tentativas. Aguarde um instante e tente novamente."),
                    cancellationToken);
            };

            options.AddPolicy(AuthPolicy, httpContext =>
            {
                // Read per request so the limits follow the final configuration.
                var configuration = httpContext.RequestServices.GetRequiredService<IConfiguration>();
                var permitLimit = configuration.GetValue("RateLimiting:Auth:PermitLimit", DefaultPermitLimit);
                var windowSeconds = configuration.GetValue("RateLimiting:Auth:WindowSeconds", DefaultWindowSeconds);

                return RateLimitPartition.GetFixedWindowLimiter(
                    GetClientKey(httpContext),
                    _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = Math.Max(1, permitLimit),
                        Window = TimeSpan.FromSeconds(Math.Max(1, windowSeconds)),
                        QueueLimit = 0
                    });
            });
        });

        return services;
    }

    /// <summary>
    /// Behind the Fly proxy every request arrives from the proxy's address, so
    /// the connection IP alone would put all users in one bucket. Fly reports
    /// the real client in the Fly-Client-IP header.
    /// </summary>
    private static string GetClientKey(HttpContext httpContext)
    {
        var flyClientIp = httpContext.Request.Headers["Fly-Client-IP"].ToString();
        if (!string.IsNullOrWhiteSpace(flyClientIp))
            return flyClientIp.Trim();

        return httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
    }
}
