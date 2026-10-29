using EventosPeruIA.CORE.Core.DTOs.Catalog;
using EventosPeruIA.CORE.Core.Entities;
using EventosPeruIA.CORE.Core.Interfaces.Repositories;
using EventosPeruIA.CORE.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
namespace EventosPeruIA.CORE.Infrastructure.Repositories;
public class EventCatalogRepository:IEventCatalogRepository
{
    private readonly PostgresContext context;
    public EventCatalogRepository(PostgresContext context)
    {
        this.context = context;
    }

    public async Task<IReadOnlyCollection<CategoryResponse>> GetCategoriesAsync(CancellationToken ct=default)=>
        await context.Categorias.AsNoTracking().Where(c=>c.Activo).OrderBy(c=>c.Orden).ThenBy(c=>c.Nombre)
            .Select(c=>new CategoryResponse { Id = c.CategoriaId, Name = c.Nombre }).ToArrayAsync(ct);
    private static IQueryable<EventCatalogItem> Project(IQueryable<Eventos> events)=>events.Select(e => new EventCatalogItem
        {
            Id = e.EventoId, Name = e.Nombre, Description = e.Descripcion,
            CategoryId = e.CategoriaId, Category = e.Categoria.Nombre,
            City = e.Ciudad, Address = e.Direccion, Venue = e.NombreLugar,
            Organizer = e.Organizador.Nombres + " " + e.Organizador.Apellidos,
            StartDate = e.FechaInicio, EndDate = e.FechaFin, Price = e.Precio,
            Currency = e.Moneda, Capacity = e.Aforo,
            AvailableSpots = e.Aforo - e.Entradas.Count(t => t.Estado != "Anulada"),
            Status = e.Estado, ImageUrl = e.ImagenUrl
        });
    public async Task<IReadOnlyCollection<EventCatalogItem>> SearchAsync(EventSearchRequest request,CancellationToken ct=default)
    {
        var now=DateTime.UtcNow;
        var query=context.Eventos.AsNoTracking().Where(e=>e.Estado=="Publicado" && e.EstadoModeracion=="Aprobado" && e.FechaInicio>now);
        if(!string.IsNullOrWhiteSpace(request.Query))
        {
            var term=request.Query.ToLowerInvariant();
            query=query.Where(e=>e.Nombre.ToLower().Contains(term)||e.Descripcion.ToLower().Contains(term));
        }
        if(request.CategoryId is int category) query=query.Where(e=>e.CategoriaId==category);
        if(!string.IsNullOrWhiteSpace(request.City))
        { var city=request.City.ToLowerInvariant();query=query.Where(e=>e.Ciudad.ToLower()==city); }
        if(request.From is { } from) {var date=from.UtcDateTime;query=query.Where(e=>e.FechaInicio>=date);}
        if(request.To is { } to) {var date=to.UtcDateTime;query=query.Where(e=>e.FechaInicio<=date);}
        return await Project(query.OrderBy(e=>e.FechaInicio).ThenBy(e=>e.EventoId)
            .Skip((request.Page-1)*request.PageSize).Take(request.PageSize)).ToArrayAsync(ct);
    }
    public Task<EventCatalogItem?> GetAsync(int id,CancellationToken ct=default)=>
        Project(context.Eventos.AsNoTracking().Where(e=>e.EventoId==id && e.EstadoModeracion=="Aprobado" &&
            (e.Estado=="Publicado"||e.Estado=="Finalizado"||e.Estado=="Cancelado"))).SingleOrDefaultAsync(ct);

    //Parte de Santiago

	public async Task<int> GetSoldTicketsCountAsync(int eventId, CancellationToken ct = default)
	{
		return await context.Entradas
			.CountAsync(t => t.EventoId == eventId &&
							(t.Estado == "Valida" || t.Estado == "Utilizada"), ct);
	}
}
