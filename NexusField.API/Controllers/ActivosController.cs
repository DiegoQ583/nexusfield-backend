using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Mvc;
using NexusField.Application.DTOs.Activos;
using NexusField.Application.Services;

namespace NexusField.API.Controllers;

[ApiController]
[Route("api/v1/activos")]
[Authorize(Roles = "Despachador")]
public class ActivosController(
    ActivoService activoService,
    IValidator<GuardarActivoDto> validator) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> ObtenerPaginados(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? q = null)
    {
        return Ok(await activoService.ObtenerPaginadosAsync(page, pageSize, q));
    }

    [HttpGet("buscar")]
    public async Task<IActionResult> Buscar([FromQuery] string? q) // Agregamos el '?' para hacerlo opcional
    {
        // Si el frontend envía vacío, devolvemos una lista vacía sin molestar a la base de datos
        if (string.IsNullOrWhiteSpace(q))
        {
            return Ok(new List<ActivoLigeroDto>()); 
        }

        return Ok(await activoService.BuscarActivosAsync(q));
    }

    [HttpPost]
    [RequireAntiforgeryToken]
    public async Task<IActionResult> Crear([FromBody] GuardarActivoDto dto)
    {
        await validator.ValidateAndThrowAsync(dto);

        var id = await activoService.CrearActivoAsync(dto);

        return Created($"/api/v1/activos/{id}", new { id });
    }

    [HttpPut("{id}")]
    [RequireAntiforgeryToken]
    public async Task<IActionResult> Actualizar(int id, [FromBody] GuardarActivoDto dto)
    {
        await validator.ValidateAndThrowAsync(dto);

        await activoService.ActualizarActivoAsync(id, dto);

        return Ok();
    }

    [HttpPatch("{id}/estado")]
    [RequireAntiforgeryToken]
    public async Task<IActionResult> CambiarEstado(int id)
    {
        await activoService.CambiarEstadoAsync(id);

        return NoContent();
    }

    [HttpGet("{id}/historial")]
    public async Task<IActionResult> ObtenerHistorial(int id)
    {
        return Ok(await activoService.ObtenerHistorialAsync(id));
    }
}
