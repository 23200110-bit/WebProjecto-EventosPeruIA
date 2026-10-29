using EventosPeruIA.CORE.Core.DTOs.Catalog;
namespace EventosPeruIA.CORE.Core.Interfaces.Services;
public interface IEventCatalogService
{
    Task<IReadOnlyCollection<CategoryResponse>> GetCategoriesAsync(CancellationToken ct=default);
    Task<IReadOnlyCollection<EventCatalogItem>> SearchAsync(EventSearchRequest request,CancellationToken ct=default);
    Task<EventCatalogItem?> GetAsync(int id,CancellationToken ct=default);
}
