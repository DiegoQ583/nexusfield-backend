using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using NexusField.API.Hubs;
using NexusField.Application.DTOs.Ordenes;
using NexusField.Application.Services;
using NexusField.Domain.Enums;
namespace NexusField.API.Controllers;

[ApiController]
[Route("api/v1/ordenes")]
public class OrdenesController(
    OrdenTrabajoService ordenService,
    IValidator<CrearOrdenDto> crearOrdenValidator,
    IHubContext<NotificacionesHub> hubContext) : ControllerBase
{

    [HttpGet]
    [Authorize(Roles = "Despachador")]
    public async Task<IActionResult> ObtenerPaginadas(
        [FromQuery] int page = 1, 
        [FromQuery] int pageSize = 10, 
        [FromQuery] string? q = null, 
        [FromQuery] int? tecnicoId = null, 
        [FromQuery] string? estado = null,
        [FromQuery] string? prioridad = null,
        [FromQuery] string? filtroTiempo = null) // <-- NUEVO PARÁMETRO
    {
        var resultado = await ordenService.ObtenerPaginadasAsync(page, pageSize, q, tecnicoId, estado, prioridad, filtroTiempo);
        return Ok(resultado);
    }

    [HttpGet("{id}")]
    [Authorize(Roles = "Despachador,Tecnico")]
    public async Task<IActionResult> ObtenerDetalle(int id)
    {
        var detalle = await ordenService.ObtenerDetalleAsync(id);
        return Ok(detalle);
    }

    [HttpPost]
    [Authorize(Roles = "Despachador")]
    [RequireAntiforgeryToken]
    public async Task<IActionResult> CrearOrden([FromBody] CrearOrdenDto dto)
    {
        // 1. Validación de entrada con prevención XSS (FluentValidation). Si falla, lanza ValidationException → capturada por GlobalExceptionHandler → 400.
        await crearOrdenValidator.ValidateAndThrowAsync(dto);

        // 2. Lógica transaccional delegada al servicio (SRP: controlador thin).
        var id = await ordenService.CrearOrdenAsync(dto);

        // 3. Notificación en tiempo real a todos los clientes conectados.
        await hubContext.Clients.All.SendAsync("NuevaNotificacion", $"Nueva orden creada: {dto.Titulo}");

        return Created($"/api/v1/ordenes/{id}", new { id });
    }


    [HttpPatch("{id}/reasignar")]
    [Authorize(Roles = "Despachador")]
    [RequireAntiforgeryToken]
    public async Task<IActionResult> ReasignarOrden(int id, [FromBody] ReasignarOrdenDto dto)
    {
        await ordenService.ReasignarOrdenAsync(id, dto);

        await hubContext.Clients.All.SendAsync("ActualizarDashboard");
        await hubContext.Clients.All.SendAsync("NuevaNotificacion", $"La orden #{id} ha sido reasignada.");

        return NoContent();
    }
}
