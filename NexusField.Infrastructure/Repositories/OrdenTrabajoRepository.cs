using Microsoft.EntityFrameworkCore;
using NexusField.Application.DTOs.Dashboard;
using NexusField.Application.Interfaces;
using NexusField.Domain.Entities;
using NexusField.Domain.Enums;
using NexusField.Infrastructure.Data;

namespace NexusField.Infrastructure.Repositories;

/// <summary>
/// Implementación EF Core del repositorio de órdenes de trabajo.
/// Todas las consultas usan AsNoTracking (solo lectura para el dashboard).
/// </summary>
public class OrdenTrabajoRepository(NexusDbContext context) : IOrdenTrabajoRepository
{
    /// <summary>
    /// Cuenta órdenes filtradas por estado y opcionalmente por fecha de creación.
    /// Si <paramref name="desde"/> es null, cuenta todas las órdenes con ese estado.
    /// </summary>
    public async Task<int> ContarPorEstadoAsync(EstadoOrden estado, DateTime? desde)
    {
        var query = context.OrdenesTrabajo
            .AsNoTracking()
            .Where(o => o.Estado == estado);

        if (desde is not null)
        {
            query = query.Where(o => o.FechaCreacion >= desde.Value);
        }

        return await query.CountAsync();
    }

    /// <summary>
    /// Suma el costo total de órdenes completadas desde la fecha indicada.
    /// Retorna 0 si no hay órdenes en el rango.
    /// </summary>
    public async Task<decimal> SumarCostoTotalAsync(DateTime desde)
    {
        return await context.OrdenesTrabajo
            .AsNoTracking()
            .Where(o => o.Estado == EstadoOrden.Completada && o.FechaCompletada >= desde)
            .SumAsync(o => o.CostoTotal);
    }

    /// <summary>
    /// Obtiene únicamente las fechas de las órdenes completadas desde la fecha indicada.
    /// El procesamiento y agrupación (Zero-Filling) se delega a la capa Application.
    /// </summary>
    public async Task<List<DateTime>> ObtenerFechasCompletadasAsync(DateTime desde)
    {
        return await context.OrdenesTrabajo
            .AsNoTracking()
            .Where(o => o.Estado == EstadoOrden.Completada && o.FechaCompletada >= desde)
            .Select(o => o.FechaCompletada!.Value) // Solo extraemos la fecha cruda
            .ToListAsync();
    }

    /// <summary>
    /// Obtiene una página de órdenes aplicando filtros dinámicos encadenados.
    /// Los filtros se construyen condicionalmente para que SQL Server solo filtre cuando hay valor.
    /// Ordena por FechaCreacion descendente (las más recientes primero).
    /// </summary>
    public async Task<(List<OrdenTrabajo> Ordenes, int Total)> ObtenerPaginadasAsync(
        int page, int pageSize, string? searchTerm, int? tecnicoId, string? estados, string? prioridad, string? filtroTiempo)
    {
        var query = context.OrdenesTrabajo
            .Include(o => o.ActivoFisico)
            .Include(o => o.Tecnico)
            .AsNoTracking()
            .AsQueryable();

        // 1. Filtro por búsqueda de texto
        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            query = query.Where(o => o.Titulo.Contains(searchTerm) || 
                                     (o.ActivoFisico != null && o.ActivoFisico.Nombre.Contains(searchTerm)));
        }

        // 2. Filtro por Técnico
        if (tecnicoId.HasValue)
        {
            query = query.Where(o => o.TecnicoId == tecnicoId.Value);
        }

        // 3. Filtro por Estado Inteligente (Entiende pestañas y múltiples estados)
        if (!string.IsNullOrWhiteSpace(estados))
        {
            var estadosNormalizado = estados.Trim().ToLower();

            // Si el frontend envía la pestaña "Activas"
            if (estadosNormalizado == "activas")
            {
                query = query.Where(o => o.Estado == EstadoOrden.Pendiente || 
                                         o.Estado == EstadoOrden.EnProgreso || 
                                         o.Estado == EstadoOrden.Pausada);
            }
            // Si el frontend envía la pestaña "Historial"
            else if (estadosNormalizado == "historial")
            {
                query = query.Where(o => o.Estado == EstadoOrden.Completada || 
                                         o.Estado == EstadoOrden.Cancelada);
            }
            // Si envía estados específicos o varios separados por coma (ignorando "todos" / "todas")
            else if (estadosNormalizado != "todas" && estadosNormalizado != "todos")
            {
                var estadosLista = estados.Split(',')
                    .Select(e => {
                        var limpio = e.Trim().Replace(" ", "");
                        return Enum.TryParse<EstadoOrden>(limpio, true, out var parsed) ? parsed : (EstadoOrden?)null;
                    })
                    .Where(e => e.HasValue)
                    .Select(e => e!.Value)
                    .ToList();

                if (estadosLista.Any())
                {
                    query = query.Where(o => estadosLista.Contains(o.Estado));
                }
            }
        }

        // 4. NUEVO: Filtro por Prioridad (Blindado contra "todas")
        if (!string.IsNullOrWhiteSpace(prioridad) && !prioridad.Equals("todas", StringComparison.OrdinalIgnoreCase))
        {
            if (Enum.TryParse<Prioridad>(prioridad.Trim(), true, out var p))
            {
                query = query.Where(o => o.Prioridad == p);
            }
        }

        // 5. NUEVO: Filtro por Tiempo (Sincronizado con la lógica del Dashboard)
        if (!string.IsNullOrWhiteSpace(filtroTiempo) && !filtroTiempo.Equals("todos", StringComparison.OrdinalIgnoreCase))
        {
            var filtroNormalizado = filtroTiempo.Trim().ToLower();
            DateTime? fechaInicio = filtroNormalizado switch
            {
                "hoy" => DateTime.UtcNow.Date,
                "semana" => DateTime.UtcNow.Date.AddDays(-7),
                "mes" => DateTime.UtcNow.Date.AddDays(-30),
                _ => null
            };

            if (fechaInicio.HasValue)
            {
                query = query.Where(o => o.FechaCreacion >= fechaInicio.Value);
            }
        }

        int total = await query.CountAsync();

        var ordenes = await query
            .OrderByDescending(o => o.FechaCreacion)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (ordenes, total);
    }

    /// <summary>
    /// Obtiene el detalle completo de una orden incluyendo todas sus relaciones:
    /// ActivoFisico, Técnico, y RepuestosUsados con su Repuesto asociado (ThenInclude).
    /// </summary>
    public async Task<OrdenTrabajo?> ObtenerDetallePorIdAsync(int id)
    {
        return await context.OrdenesTrabajo
            .AsNoTracking()
            .Include(o => o.ActivoFisico)
            .Include(o => o.Tecnico)
            .Include(o => o.RepuestosUsados)
                .ThenInclude(ru => ru.Repuesto)
            .FirstOrDefaultAsync(o => o.Id == id);
    }

    /// <summary>
    /// Registra una nueva orden en el change tracker del DbContext.
    /// NO llama a SaveChanges — eso lo hace el UnitOfWork.
    /// </summary>
    public void Agregar(OrdenTrabajo orden)
    {
        context.OrdenesTrabajo.Add(orden);
    }

    /// <summary>
    /// Verifica si el técnico especificado tiene órdenes en estado Pendiente o EnProgreso.
    /// </summary>
    public async Task<bool> TieneOrdenesActivasAsync(int tecnicoId)
    {
        return await context.OrdenesTrabajo
            .AsNoTracking()
            .AnyAsync(o => o.TecnicoId == tecnicoId && (o.Estado == EstadoOrden.Pendiente || o.Estado == EstadoOrden.EnProgreso));
    }

    public async Task<DistribucionPrioridadDto> ObtenerDistribucionPrioridadesAsync()
    {
        // Contamos las órdenes que NO están completadas ni canceladas (es decir, el trabajo pendiente real)
        var altas = await context.OrdenesTrabajo.CountAsync(o => o.Estado != EstadoOrden.Completada && o.Estado != EstadoOrden.Cancelada && o.Prioridad == Prioridad.Alta);
        var medias = await context.OrdenesTrabajo.CountAsync(o => o.Estado != EstadoOrden.Completada && o.Estado != EstadoOrden.Cancelada && o.Prioridad == Prioridad.Media);
        var bajas = await context.OrdenesTrabajo.CountAsync(o => o.Estado != EstadoOrden.Completada && o.Estado != EstadoOrden.Cancelada && o.Prioridad == Prioridad.Baja);

        return new DistribucionPrioridadDto(altas, medias, bajas);
    }

    public async Task<List<OrdenTrabajo>> ObtenerOrdenesActivasPorTecnicoAsync(int tecnicoId)
    {
        return await context.OrdenesTrabajo
            .AsNoTracking()
            .Include(o => o.ActivoFisico)
            .Include(o => o.RepuestosUsados)
                .ThenInclude(ru => ru.Repuesto)
            .Where(o => o.TecnicoId == tecnicoId &&
                            (o.Estado == EstadoOrden.Pendiente ||
                             o.Estado == EstadoOrden.EnProgreso ||
                             o.Estado == EstadoOrden.Pausada))
            .OrderByDescending(o => o.Prioridad)
            .ToListAsync();
    }

    public async Task<OrdenTrabajo?> ObtenerPorIdAsync(int id)
    {
        return await context.OrdenesTrabajo.FirstOrDefaultAsync(o => o.Id == id);
    }

    public async Task<OrdenTrabajo?> ObtenerParaCompletarAsync(int id)
    {
        // VITAL: NO usar AsNoTracking() porque el servicio mutará la orden, los repuestos
        // y calculará el costo en base a la tarifa del técnico, requiriendo persistencia (SaveChanges).
        return await context.OrdenesTrabajo
            .Include(o => o.RepuestosUsados)
            .Include(o => o.Tecnico)
            .FirstOrDefaultAsync(o => o.Id == id);
    }

    public async Task<List<OrdenTrabajo>> ObtenerHistorialPorActivoAsync(int activoId)
    {
        return await context.OrdenesTrabajo
            .Where(o => o.ActivoId == activoId && o.Estado == EstadoOrden.Completada)
            .Include(o => o.Tecnico)
            .OrderByDescending(o => o.FechaCompletada)
            .Take(5)
            .AsNoTracking()
            .ToListAsync();
    }

    public void Actualizar(OrdenTrabajo orden)
    {
        context.OrdenesTrabajo.Update(orden);
    }
}
