namespace NexusField.Application.DTOs.Usuarios;

public record UsuarioResponseDto(
    int Id,
    string NombreCompleto,
    string NombreUsuario,
    string Correo,
    string? Telefono,
    string? Especialidad,
    decimal TarifaPorHora,
    string Rol,
    bool Activo);
