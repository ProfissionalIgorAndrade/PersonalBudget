namespace PersonalBudget.Api;

public static class HouseholdHttp
{
    public const string HouseholdIdHeaderName = "X-Household-Id";

    public static Guid? TryGetHouseholdIdHeader(HttpRequest request)
    {
        if (!request.Headers.TryGetValue(HouseholdIdHeaderName, out var vals))
            return null;
        var v = vals.FirstOrDefault();
        return Guid.TryParse(v, out var g) ? g : null;
    }
}
