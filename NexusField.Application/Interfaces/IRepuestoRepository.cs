using NexusField.Domain.Entities;
using NexusField.Application.DTOs.Inventario;

namespace NexusField.Application.Interfaces;

/// <summary>
/// Contrato de acceso a datos para repuestos.
/// Incluye operaciones CRUD completas y consulta de integridad referencial.
/// Para este módulo, el servicio usa SaveChangesAsync directamente
/// (sin UnitOfWork) porque no hay transacciones multi-tabla.
/// </summary>
public interface IRepuestoRepository
{
    /// <summary>
    /// Obtiene todos los repuestos sin filtrar (activos e inactivos).
    /// </summary>
    Task<List<Repuesto>> ObtenerTodosAsync();

    /// <summary>
    /// Obtiene un repuesto por su Id. Retorna null si no existe.
    /// </summary>
    Task<Repuesto?> ObtenerPorIdAsync(int id);

    /// <summary>
    /// Verifica si un repuesto tiene órdenes de trabajo asociadas (OrdenRepuesto).
    /// Determina si se puede hacer Hard Delete o solo Soft Delete.
    /// </summary>
    Task<bool> TieneOrdenesAsociadasAsync(int id);

    /// <summary>
    /// Registra un nuevo repuesto en el change tracker.
    /// </summary>
    void Agregar(Repuesto repuesto);

    /// <summary>
    /// Marca un repuesto como modificado en el contexto de persistencia.
    /// </summary>
    void Actualizar(Repuesto repuesto);

    /// <summary>
    /// Elimina un repuesto del contexto (Hard Delete).
    /// </summary>
    void Eliminar(Repuesto repuesto);

    /// <summary>
    /// Persiste todos los cambios pendientes del DbContext.
    /// </summary>
    Task SaveChangesAsync();

    Task<List<ReservaTecnicoDto>> ObtenerReservasActivasAsync(int repuestoId);

    /// <summary>
    /// Obtiene un catálogo ligero de repuestos activos para uso offline del técnico.
    /// </summary>
    Task<List<Repuesto>> ObtenerCatalogoLigeroAsync();

    /// <summary>
    /// Obtiene varios repuestos por sus IDs, preparado para actualización de stock (con Tracking).
    /// </summary>
    Task<List<Repuesto>> ObtenerVariosPorIdAsync(List<int> ids);
}
