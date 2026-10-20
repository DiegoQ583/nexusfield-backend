using NexusField.Domain.Enums;
using NexusField.Domain.Interfaces;

namespace NexusField.Domain.Entities;

public class OrdenTrabajo : IAuditableEntity
{
    public int Id { get; set; }
    public required string Titulo { get; set; }
    public required string Descripcion { get; set; }
    public DateTime FechaCreacion { get; set; }
    public DateTime? FechaCompletada { get; set; }
    public decimal HorasTrabajadas { get; set; }
    public decimal CostoTotal { get; set; }
    public string? NotasTecnico { get; set; }
    public string? FotoUrl { get; set; }
    public string? FirmaClienteBase64 { get; set; }
    public required string ClienteHistorico { get; set; }

    // Enums del dominio
    public EstadoOrden Estado { get; set; }
    public Prioridad Prioridad { get; set; }

    // Foreign Keys
    public int ActivoId { get; set; }
    public int? TecnicoId { get; set; }

    // Auditoría
    public DateTime? FechaModificacion { get; set; }
    public int? UsuarioModificacionId { get; set; }

    // Navegación
    public Activo? ActivoFisico { get; set; }
    public Usuario? Tecnico { get; set; }
    public ICollection<OrdenRepuesto> RepuestosUsados { get; set; } = [];
}
