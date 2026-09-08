/// <summary>
/// Payload for an authenticated user changing their own password.
/// The target user is taken from the JWT, never from the request body.
/// </summary>
public record ChangePasswordRequest(
    string CurrentPassword,
    string NewPassword);
