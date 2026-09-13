using System.Net.Http.Json;
using System.Text.Json;

namespace PersonalBudget.Integration.Tests.Helpers;

public static class AuthHelper
{
    private static readonly JsonSerializerOptions JsonOpts =
        new() { PropertyNameCaseInsensitive = true };

    /// <summary>
    /// Creates a fresh user via POST /api/authentication/signin and returns a
    /// Bearer token ready for use in subsequent requests.
    /// </summary>
    public static async Task<string> SignInAsync(HttpClient client, string? email = null, string? password = null)
    {
        email    ??= $"test_{Guid.NewGuid():N}@test.com";
        password ??= "Test@12345";

        var response = await client.PostAsJsonAsync("/api/authentication/signin", new
        {
            name     = "Test User",
            email,
            password
        });

        response.EnsureSuccessStatusCode();

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        var token = body
            .GetProperty("data")
            .GetProperty("token")
            .GetString()!;

        return token;
    }

    public static void SetBearer(this HttpClient client, string token)
    {
        client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
    }
}
