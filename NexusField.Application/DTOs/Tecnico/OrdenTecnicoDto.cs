namespace NexusField.Application.DTOs.Tecnico;

public record OrdenTecnicoDto(
    int Id,
    string Titulo,
    string Descripcion,
    string ActivoNombre,
    string Ubicacion,
    string? LinkGoogleMaps,
    string? TelefonoContacto,
    string Prioridad,
    string Estado,
    List<RepuestoPreAsignadoDto> RepuestosPreAsignados
);
