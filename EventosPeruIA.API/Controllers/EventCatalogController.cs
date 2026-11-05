using System.ComponentModel.DataAnnotations;
using EventosPeruIA.CORE.Core.DTOs.Catalog;
using EventosPeruIA.CORE.Core.Interfaces.Services;
using Microsoft.AspNetCore.Mvc;

namespace EventosPeruIA.API.Controllers;

[ApiController]
[Route("api/catalog")]
public class EventCatalogController : ControllerBase
{
	private readonly IEventCatalogService service;

	public EventCatalogController(IEventCatalogService service)
	{
		this.service = service;
	}

	[HttpGet("categories")]
	public async Task<IActionResult> Categories(CancellationToken ct) =>
		Ok(await service.GetCategoriesAsync(ct));

	[HttpGet("events")]
	public async Task<IActionResult> Search([FromQuery] EventSearchRequest request, CancellationToken ct)
	{
		try
		{
			return Ok(await service.SearchAsync(request, ct));
		}
		catch (ValidationException e)
		{
			return BadRequest(new { message = e.Message });
		}
	}

	// US-006: Detalle con aforo reactivo en tiempo real
	[HttpGet("events/{id:int}")]
	public async Task<IActionResult> Detail(int id, CancellationToken ct)
	{
		var item = await service.GetDetailWithCapacityAsync(id, ct);
		return item is null ? NotFound(new { message = $"El evento con ID {id} no fue encontrado." }) : Ok(item);
	}
}