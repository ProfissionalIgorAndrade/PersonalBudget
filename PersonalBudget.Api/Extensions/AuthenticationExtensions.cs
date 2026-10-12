namespace PersonalBudget.Api.Extensions;

using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using PersonalBudget.Api.Security;
using System.Security.Cryptography;
using System.Text;

public static class AuthenticationExtensions
{
    // HMAC-SHA256 needs a key of at least 256 bits.
    private const int MinimumKeyBytes = 32;

    // Local runs need no setup: Development gets a throwaway key per process.
    private static readonly string DevelopmentKey =
        Convert.ToBase64String(RandomNumberGenerator.GetBytes(48));

    public static IServiceCollection AddJwtAuthentication(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddOptions<JwtSettings>()
            .Bind(configuration.GetSection("Jwt"))
            .PostConfigure<IHostEnvironment>((settings, environment) =>
            {
                if (environment.IsDevelopment() && string.IsNullOrWhiteSpace(settings.Key))
                    settings.Key = DevelopmentKey;
            })
            .Validate(
                settings => Encoding.UTF8.GetByteCount(settings.Key) >= MinimumKeyBytes,
                $"Jwt:Key is missing or shorter than {MinimumKeyBytes} bytes. " +
                "Set it outside the repository: the Jwt__Key environment variable " +
                "(fly secrets set Jwt__Key=...) or dotnet user-secrets.")
            .ValidateOnStart();

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer();

        // Built from the options instead of the raw configuration so the key is
        // read after every configuration source has been applied.
        services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
            .Configure<IOptions<JwtSettings>>((options, jwtOptions) =>
            {
                var jwt = jwtOptions.Value;

                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = jwt.Issuer,
                    ValidAudience = jwt.Audience,
                    IssuerSigningKey = new SymmetricSecurityKey(
                        Encoding.UTF8.GetBytes(jwt.Key))
                };
            });

        services.AddAuthorization();

        services.AddSingleton<JwtTokenGenerator>();

        return services;
    }
}
