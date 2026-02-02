using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

public static class UserContext
{
    public static Guid GetUserId(ClaimsPrincipal user)
    {
        /*
        var value = user.FindFirstValue(ClaimTypes.NameIdentifier)
                    ?? user.FindFirstValue(JwtRegisteredClaimNames.Sub);

        return Guid.Parse(value!);
        */

        return DevUser.Id;
    }
}
