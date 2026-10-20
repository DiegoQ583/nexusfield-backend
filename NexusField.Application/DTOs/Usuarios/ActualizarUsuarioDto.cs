namespace NexusField.Application.DTOs.Usuarios;

/// <summary>
/// DTO para actualización parcial de usuario.
/// No incluye Username, Password ni Rol — esos campos no se modifican por este flujo.
/// </summary>
public record ActualizarUsuarioDto(
    string NombreCompleto,
    string Correo,
    string? Telefono,
    string? Especialidad,
    decimal TarifaPorHora);
