using NexusField.Domain.Interfaces;

namespace NexusField.Domain.Entities;

public class Activo : IAuditableEntity
{
    public int Id { get; set; }
    public required string Nombre { get; set; }
    public required string Ubicacion { get; set; }
    public string? Descripcion { get; set; }
    public required string ClienteActual { get; set; }
    public string Estado { get; set; } = "Operativo";
    public bool Activo_ { get; set; } = true;
    public string? LinkGoogleMaps { get; set; }
    public string? TelefonoContacto { get; set; }

    // Auditoría
    public DateTime FechaCreacion { get; set; }
    public DateTime? FechaModificacion { get; set; }
    public int? UsuarioModificacionId { get; set; }
}
