using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Mvc;
using NexusField.Application.DTOs.Inventario;
using NexusField.Application.Services;

namespace NexusField.API.Controllers;

/// Endpoints CRUD para el módulo de Inventario (Repuestos).
/// Soporta subida de archivos (multipart/form-data) para fotos de repuestos.
/// Solo accesible para Despachadores.
[ApiController]
[Route("api/v1/repuestos")]
[Authorize(Roles = "Despachador")]
public class RepuestosController(
    RepuestoService repuestoService,
    IValidator<GuardarRepuestoDto> validator) : ControllerBase
{
    /// Lista todos los repuestos (activos e inactivos) con stock disponible calculado.
    [HttpGet]
    public async Task<IActionResult> ObtenerTodos()
    {
        var repuestos = await repuestoService.ObtenerTodosAsync();

        return Ok(repuestos);
    }

    /// Obtiene el detalle de un repuesto con la trazabilidad de qué técnicos lo tienen reservado.
    [HttpGet("{id}")]
    public async Task<IActionResult> ObtenerPorId(int id)
    {
        var repuesto = await repuestoService.ObtenerPorIdAsync(id);
        return Ok(repuesto);
    }

    /// Crea un nuevo repuesto con foto opcional (multipart/form-data).
    [HttpPost]
    [RequireAntiforgeryToken]
    [Consumes("multipart/form-data")] // Le dice a Swagger/Scalar y a .NET que acepta archivos
    public async Task<IActionResult> Crear([FromForm] GuardarRepuestoDto dto) // <-- CRÍTICO: [FromForm]
    {
        await validator.ValidateAndThrowAsync(dto);

        var id = await repuestoService.CrearAsync(dto);

        return Created($"/api/v1/repuestos/{id}", new { id });
    }
    
    /// Actualiza un repuesto existente. Si incluye foto nueva, reemplaza la anterior.
        [HttpPut("{id}")]
    [RequireAntiforgeryToken]
    [Consumes("multipart/form-data")] // Le dice a .NET que el PUT también recibe archivos
    public async Task<IActionResult> Actualizar(int id, [FromForm] GuardarRepuestoDto dto) // <-- CRÍTICO: [FromForm]
    {
        await validator.ValidateAndThrowAsync(dto);

        await repuestoService.ActualizarAsync(id, dto);

        return Ok();
    }

    /// Elimina un repuesto. Si tiene órdenes asociadas, ejecuta soft delete.
    /// Si no tiene órdenes, ejecuta hard delete + borrado de foto.
    [HttpDelete("{id}")]
    [RequireAntiforgeryToken]
    public async Task<IActionResult> Eliminar(int id)
    {
        await repuestoService.EliminarAsync(id);

        return NoContent();
    }
}
