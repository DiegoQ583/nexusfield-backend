namespace NexusField.Application.DTOs.Activos;

public record ActivoResponseDto(
    int Id,
    string Nombre,
    string? Descripcion,
    string Ubicacion,
    string ClienteActual,
    string Estado,
    bool Activo,
    string? LinkGoogleMaps,
    string? TelefonoContacto);
