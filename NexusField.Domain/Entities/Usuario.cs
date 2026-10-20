using NexusField.Domain.Enums;

namespace NexusField.Domain.Entities;

public class Usuario
{
    public int Id { get; set; }
    public required string NombreCompleto { get; set; }
    public required string NombreUsuario { get; set; }
    public required string Correo { get; set; }
    public required string PasswordHash { get; set; }
    public string? Telefono { get; set; }
    public string? Especialidad { get; set; }
    public decimal TarifaPorHora { get; set; }
    public RolUsuario Rol { get; set; }
    public bool Activo { get; set; } = true;
}
