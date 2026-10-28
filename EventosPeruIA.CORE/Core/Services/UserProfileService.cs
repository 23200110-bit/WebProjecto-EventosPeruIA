using EventosPeruIA.CORE.Core.DTOs;
using EventosPeruIA.CORE.Core.Entities;
using EventosPeruIA.CORE.Core.Interfaces;

namespace EventosPeruIA.CORE.Core.Services;

public sealed class UserProfileService(IUserProfileRepository repository) : IUserProfileService
{
    public async Task<UserProfileResponse?> GetAsync(int userId, CancellationToken cancellationToken = default)
    {
        var user = await repository.GetByIdAsync(userId, cancellationToken);
        return user is null ? null : Map(user);
    }

    public async Task<UserProfileResponse> UpdateAsync(
        int userId,
        UpdateUserProfileRequest request,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.FirstName) || string.IsNullOrWhiteSpace(request.LastName))
        {
            throw new ArgumentException("First name and last name are required.");
        }

        if ((request.Latitude is null) != (request.Longitude is null))
        {
            throw new ArgumentException("Latitude and longitude must be provided together.");
        }

        var user = await repository.GetByIdAsync(userId, cancellationToken)
            ?? throw new KeyNotFoundException("User profile was not found.");
        var categoryIds = request.InterestCategoryIds.Distinct().ToArray();
        var categories = await repository.GetCategoriesAsync(categoryIds, cancellationToken);

        if (categories.Count != categoryIds.Length)
        {
            throw new ArgumentException("One or more selected interest categories are invalid or inactive.");
        }

        user.Nombres = request.FirstName.Trim();
        user.Apellidos = request.LastName.Trim();
        user.Telefono = Normalize(request.Phone);
        user.FotoUrl = Normalize(request.PhotoUrl);
        user.CiudadUbicacion = Normalize(request.City);
        user.Latitud = request.Latitude;
        user.Longitud = request.Longitude;
        user.FechaActualizacion = DateTime.UtcNow;

        var interests = categories.Select(category => new UsuarioIntereses
        {
            UsuarioId = userId,
            CategoriaId = category.CategoriaId,
            FechaRegistro = DateTime.UtcNow
        });

        await repository.UpdateAsync(user, interests, cancellationToken);
        return Map(user, categories);
    }

    private static UserProfileResponse Map(Usuarios user, IReadOnlyCollection<Categorias>? categories = null) =>
        new()
        {
            UserId = user.UsuarioId,
            FirstName = user.Nombres,
            LastName = user.Apellidos,
            Email = user.Correo,
            Phone = user.Telefono,
            PhotoUrl = user.FotoUrl,
            City = user.CiudadUbicacion,
            Latitude = user.Latitud,
            Longitude = user.Longitud,
            Interests = (categories ?? user.UsuarioIntereses.Select(interest => interest.Categoria).ToArray())
                .Select(category => new InterestResponse { CategoryId = category.CategoriaId, Name = category.Nombre })
                .ToArray()
        };

    private static string? Normalize(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
