using EventosPeruIA.CORE.Core.DTOs.Catalog;
namespace EventosPeruIA.CORE.Core.Interfaces.Repositories;
public interface IEventCatalogRepository
{
    Task<IReadOnlyCollection<CategoryResponse>> GetCategoriesAsync(CancellationToken ct=default);
    Task<IReadOnlyCollection<EventCatalogItem>> SearchAsync(EventSearchRequest request,CancellationToken ct=default);
    Task<EventCatalogItem?> GetAsync(int id,CancellationToken ct=default);
}
