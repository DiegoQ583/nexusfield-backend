using NexusField.Application.DTOs.Dashboard;
using NexusField.Domain.Entities;
using NexusField.Domain.Enums;

namespace NexusField.Application.Interfaces;

/// Contrato de acceso a datos para órdenes de trabajo.
/// Definido en Application para respetar la Regla de Dependencia (Clean Architecture).
/// La implementación concreta vive en Infrastructure.
public interface IOrdenTrabajoRepository
{
    /// Cuenta órdenes por estado, opcionalmente filtradas desde una fecha.
    Task<int> ContarPorEstadoAsync(EstadoOrden estado, DateTime? desde);

    /// Suma el costo total de órdenes completadas desde la fecha indicada.
    Task<decimal> SumarCostoTotalAsync(DateTime desde);

    /// Obtiene datos agrupados por día para el gráfico de órdenes completadas.
    Task<List<DateTime>> ObtenerFechasCompletadasAsync(DateTime desde);

    /// Obtiene una página de órdenes con filtros dinámicos opcionales.
    /// Retorna una tupla con la lista paginada y el conteo total (para calcular páginas en el servicio).
    // Cambia EstadoOrden? estado por string? estados
    Task<(List<OrdenTrabajo> Ordenes, int Total)> ObtenerPaginadasAsync(
        int page, int pageSize, string? searchTerm, int? tecnicoId, string? estados, string? prioridad, string? filtroTiempo);

    /// Obtiene una orden con todas sus relaciones cargadas (Activo, Técnico, Repuestos).
    /// Retorna null si no existe.
    Task<OrdenTrabajo?> ObtenerDetallePorIdAsync(int id);

    /// Registra una nueva orden en el change tracker del contexto.
    /// NO persiste cambios — eso lo hace el UnitOfWork.
    void Agregar(OrdenTrabajo orden);

    /// Verifica si el técnico especificado tiene órdenes en estado Pendiente o EnProgreso.
    Task<bool> TieneOrdenesActivasAsync(int tecnicoId);

    /// Obtiene las órdenes activas (Pendiente o Pausada) asignadas a un técnico.
    Task<List<OrdenTrabajo>> ObtenerOrdenesActivasPorTecnicoAsync(int tecnicoId);

    /// Obtiene una orden de trabajo por su Id.
    Task<OrdenTrabajo?> ObtenerPorIdAsync(int id);

    /// Obtiene una orden de trabajo preparada para ser completada (con Tracking habilitado).
    Task<OrdenTrabajo?> ObtenerParaCompletarAsync(int id);

    /// Obtiene el historial de órdenes completadas para un activo específico.
    Task<List<OrdenTrabajo>> ObtenerHistorialPorActivoAsync(int activoId);

    /// Actualiza una orden de trabajo existente.
    void Actualizar(OrdenTrabajo orden);

    Task<DistribucionPrioridadDto> ObtenerDistribucionPrioridadesAsync();
}
