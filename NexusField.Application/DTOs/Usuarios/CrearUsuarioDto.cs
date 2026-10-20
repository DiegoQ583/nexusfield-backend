using NexusField.Domain.Enums;

namespace NexusField.Application.DTOs.Usuarios;

public record CrearUsuarioDto(
    string NombreCompleto,
    string NombreUsuario,
    string Correo,
    string Clave,
    string? Telefono,
    string? Especialidad,
    decimal TarifaPorHora,
    RolUsuario Rol);
