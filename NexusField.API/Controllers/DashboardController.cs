using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NexusField.Application.Services;

namespace NexusField.API.Controllers;

/// <summary>
/// Endpoint del dashboard del despachador.
/// Solo accesible para usuarios con rol Despachador (validado via ClaimTypes.Role en el JWT).
/// </summary>
[ApiController]
[Route("api/v1/dashboard")]
[Authorize(Roles = "Despachador")]
public class DashboardController(DashboardService dashboardService) : ControllerBase
{
    /// <summary>
    /// Obtiene las métricas consolidadas del dashboard.
    /// </summary>
    /// <param name="filtro">Rango temporal: "hoy", "semana" o "mes" (default).</param>
    /// <returns>KPIs + datos del gráfico de tendencia.</returns>
    [HttpGet("metricas")]
    public async Task<IActionResult> ObtenerMetricas([FromQuery] string filtro = "mes")
    {
        var metricas = await dashboardService.ObtenerMetricasAsync(filtro);
        return Ok(metricas);
    }
}
