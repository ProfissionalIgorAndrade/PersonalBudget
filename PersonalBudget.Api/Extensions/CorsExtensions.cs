namespace PersonalBudget.Api.Extensions;

using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Cors.Infrastructure;

public static class CorsExtensions
{
    public const string PolicyName = "Default";

    public static IServiceCollection AddCorsPolicy(
        this IServiceCollection services)
    {
        services.AddCors();

        // Configured through options so the origins are read when the policy is
        // first used, after every configuration source has been applied.
        services.AddOptions<CorsOptions>()
            .Configure<IConfiguration, IHostEnvironment>((options, configuration, environment) =>
            {
                options.AddPolicy(PolicyName, policy =>
                {
                    policy
                        .AllowAnyHeader()
                        .AllowAnyMethod();

                    if (environment.IsDevelopment())
                    {
                        policy.AllowAnyOrigin();
                        return;
                    }

                    var origins = ReadList(configuration, "Cors:AllowedOrigins")
                        .Select(o => o.TrimEnd('/'))
                        .ToHashSet(StringComparer.OrdinalIgnoreCase);

                    var patterns = ReadList(configuration, "Cors:AllowedOriginPatterns")
                        .Select(ToRegex)
                        .ToArray();

                    policy.SetIsOriginAllowed(origin =>
                        origins.Contains(origin) || patterns.Any(p => p.IsMatch(origin)));
                });
            });

        return services;
    }

    private static string[] ReadList(IConfiguration configuration, string key)
        => (configuration.GetSection(key).Get<string[]>() ?? [])
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(value => value.Trim())
            .ToArray();

    /// <summary>
    /// '*' stands for a single host label fragment (letters, digits, hyphens).
    /// It never matches a dot, so a pattern cannot be satisfied by appending
    /// another domain to an allowed one.
    /// </summary>
    private static Regex ToRegex(string pattern)
    {
        var parts = pattern.TrimEnd('/').Split('*').Select(Regex.Escape);

        return new Regex(
            $"^{string.Join("[A-Za-z0-9-]+", parts)}$",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
    }
}
