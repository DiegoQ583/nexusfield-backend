namespace NexusField.Application.DTOs.Activos;

public record HistorialActivoDto(
    int OrdenId,
    string Titulo,
    string TecnicoNombre,
    DateTime? FechaCompletada,
    string? NotasTecnico);
