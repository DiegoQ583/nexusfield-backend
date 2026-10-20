namespace NexusField.Application.DTOs.Activos;

public record GuardarActivoDto(
    string Nombre,
    string? Descripcion,
    string Ubicacion,
    string ClienteActual,
    string Estado,
    string? LinkGoogleMaps,
    string? TelefonoContacto);
