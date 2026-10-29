using System.ComponentModel.DataAnnotations;
using EventosPeruIA.CORE.Core.DTOs.Catalog;
using EventosPeruIA.CORE.Core.Interfaces.Repositories;
using EventosPeruIA.CORE.Core.Interfaces.Services;
namespace EventosPeruIA.CORE.Core.Services;
public class EventCatalogService:IEventCatalogService
{
    private readonly IEventCatalogRepository repository;
    public EventCatalogService(IEventCatalogRepository repository)
    {
        this.repository = repository;
    }

    public Task<IReadOnlyCollection<CategoryResponse>> GetCategoriesAsync(CancellationToken ct=default)=>repository.GetCategoriesAsync(ct);
    public Task<IReadOnlyCollection<EventCatalogItem>> SearchAsync(EventSearchRequest request,CancellationToken ct=default)
    {
        Validator.ValidateObject(request,new ValidationContext(request),true);
        if(request.From>request.To) throw new ValidationException("La fecha inicial no puede ser posterior a la fecha final.");
        request.Query=request.Query?.Trim();request.City=request.City?.Trim();
        return repository.SearchAsync(request,ct);
    }
    public Task<EventCatalogItem?> GetAsync(int id,CancellationToken ct=default)=>repository.GetAsync(id,ct);
}
