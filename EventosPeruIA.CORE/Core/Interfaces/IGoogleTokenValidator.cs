namespace EventosPeruIA.CORE.Core.Interfaces;

public sealed record GoogleIdentity(string Subject, string Email, string GivenName, string FamilyName);

public interface IGoogleTokenValidator
{
    /// <summary>Devuelve la identidad si el ID token es válido para este cliente y el correo está verificado; si no, null.</summary>
    Task<GoogleIdentity?> ValidateAsync(string idToken, CancellationToken ct);
}
