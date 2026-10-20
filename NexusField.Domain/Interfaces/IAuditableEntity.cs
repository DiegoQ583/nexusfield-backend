namespace NexusField.Domain.Interfaces;

public interface IAuditableEntity
{
    DateTime FechaCreacion { get; set; }
    DateTime? FechaModificacion { get; set; }
    int? UsuarioModificacionId { get; set; }
}
