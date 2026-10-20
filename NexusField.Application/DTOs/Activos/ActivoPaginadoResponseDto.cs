namespace NexusField.Application.DTOs.Activos;

public record ActivoPaginadoResponseDto(
    List<ActivoResponseDto> Data,
    int TotalRecords,
    int CurrentPage,
    int TotalPages);
