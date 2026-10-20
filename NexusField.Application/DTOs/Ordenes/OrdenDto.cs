namespace NexusField.Application.DTOs.Ordenes;

/// <summary>
/// DTO ligero para la vista de listado/tabla de órdenes.
/// Contiene solo los datos visibles en cada fila de la grilla del frontend.
/// </summary>
public record OrdenDto(
    int Id,
    string Titulo,
    string ActivoNombre,
    string TecnicoNombre,
    string Prioridad,
    string Estado,
    DateTime FechaCreacion);
