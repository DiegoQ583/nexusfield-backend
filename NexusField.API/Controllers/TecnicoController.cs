using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using NexusField.Application.DTOs.Tecnico;
using NexusField.Application.Services;
using NexusField.API.Hubs;

namespace NexusField.API.Controllers;

[ApiController]
[Route("api/v1/tecnico")]
[Authorize(Roles = "Tecnico")]
public class TecnicoController(
    TecnicoAppService tecnicoService,
    IValidator<PausarOrdenDto> pausarOrdenValidator,
    IValidator<CompletarOrdenDto> completarOrdenValidator,
    IValidator<RechazarOrdenDto> rechazarOrdenValidator,
    IHubContext<NotificacionesHub> hubContext) : ControllerBase
{
    [HttpGet("mis-ordenes")]
    public async Task<IActionResult> ObtenerMisOrdenesActivas()
    {
        return Ok(await tecnicoService.ObtenerMisOrdenesActivasAsync());
    }

    [HttpGet("catalogo-offline")]
    public async Task<IActionResult> ObtenerCatalogoOffline()
    {
        return Ok(await tecnicoService.ObtenerCatalogoOfflineAsync());
    }

    [HttpPost("ordenes/{id}/iniciar")]
    [RequireAntiforgeryToken]
    public async Task<IActionResult> IniciarOrden(int id)
    {
        var titulo = await tecnicoService.IniciarOrdenAsync(id);

        await hubContext.Clients.All.SendAsync("ActualizarDashboard");
        await hubContext.Clients.All.SendAsync("NuevaNotificacion", $"El técnico ha iniciado la orden: {titulo}");

        return Ok();
    }

    [HttpPost("ordenes/{id}/pausar")]
    [RequireAntiforgeryToken]
    public async Task<IActionResult> PausarOrden(int id, [FromBody] PausarOrdenDto dto)
    {
        await pausarOrdenValidator.ValidateAndThrowAsync(dto);

        var titulo = await tecnicoService.PausarOrdenAsync(id, dto);

        await hubContext.Clients.All.SendAsync("ActualizarDashboard");
        await hubContext.Clients.All.SendAsync("NuevaNotificacion", $"El técnico ha pausado la orden: {titulo}");

        return Ok();
    }

    [HttpPost("ordenes/{id}/reanudar")]
    [RequireAntiforgeryToken]
    public async Task<IActionResult> ReanudarOrden(int id)
    {
        var titulo = await tecnicoService.ReanudarOrdenAsync(id);

        await hubContext.Clients.All.SendAsync("ActualizarDashboard");
        await hubContext.Clients.All.SendAsync("NuevaNotificacion", $"El técnico ha reanudado la orden: {titulo}");

        return NoContent();
    }

    [HttpPost("ordenes/{id}/completar")]
    [RequireAntiforgeryToken]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> CompletarOrden(int id, [FromForm] CompletarOrdenDto dto)
    {
        if (id != dto.OrdenId)
        {
            return BadRequest(new { mensaje = "El ID de la URL no coincide con el del cuerpo de la petición." });
        }

        await completarOrdenValidator.ValidateAndThrowAsync(dto);
        await tecnicoService.CompletarOrdenAsync(dto);

        await hubContext.Clients.All.SendAsync("ActualizarDashboard");
        await hubContext.Clients.All.SendAsync("NuevaNotificacion", $"La orden #{id} ha sido completada.");

        return Ok(new { mensaje = "Orden completada exitosamente." });
    }

    [HttpPost("ordenes/{id}/rechazar")]
    [RequireAntiforgeryToken]
    public async Task<IActionResult> RechazarOrden(int id, [FromBody] RechazarOrdenDto dto)
    {
        await rechazarOrdenValidator.ValidateAndThrowAsync(dto);

        await tecnicoService.RechazarOrdenAsync(id, dto);

        await hubContext.Clients.All.SendAsync("ActualizarDashboard");
        await hubContext.Clients.All.SendAsync("NuevaNotificacion", $"La orden #{id} ha sido rechazada.");

        return Ok();
    }

    [HttpGet("historial")]
    public async Task<IActionResult> ObtenerHistorial(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? q = null,
        [FromQuery] string? estado = null,
        [FromQuery] string? filtroTiempo = null)
    {
        return Ok(await tecnicoService.ObtenerHistorialPaginadoAsync(page, pageSize, q, estado, filtroTiempo));
    }
}

