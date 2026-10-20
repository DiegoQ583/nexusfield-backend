using NexusField.Domain.Entities;

namespace NexusField.Application.Interfaces;

/// <summary>
/// Contrato de acceso a datos para activos físicos.
/// Solo lectura por ahora (Sprint 2: necesario para validar existencia al crear órdenes).
/// </summary>
public interface IActivoRepository
{
    /// <summary>
    /// Obtiene una página de activos. Retorna una tupla con la lista y el total.
    /// </summary>
    Task<(List<Activo> Activos, int Total)> ObtenerPaginadosAsync(int page, int pageSize, string? searchTerm);

    /// <summary>
    /// Busca activos por coincidencia de término. Ideal para combobox.
    /// </summary>
    Task<List<Activo>> BuscarPorTerminoAsync(string query);

    /// <summary>
    /// Obtiene un activo por su Id. Retorna null si no existe.
    /// </summary>
    Task<Activo?> ObtenerPorIdAsync(int id);

    /// <summary>
    /// Registra un nuevo activo en el contexto.
    /// </summary>
    void Agregar(Activo activo);

    /// <summary>
    /// Marca un activo como actualizado.
    /// </summary>
    void Actualizar(Activo activo);

    /// <summary>
    /// Guarda todos los cambios pendientes en la base de datos.
    /// </summary>
    Task SaveChangesAsync();
}
