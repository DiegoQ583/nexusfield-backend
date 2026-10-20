namespace NexusField.Application.DTOs.Ordenes;

/// <summary>
/// DTO envolvente para respuestas paginadas de órdenes.
/// Provee metadatos de paginación para que el frontend renderice controles de navegación.
/// </summary>
public record OrdenPaginadaResponseDto(
    List<OrdenDto> Data,
    int TotalRecords,
    int CurrentPage,
    int TotalPages);
