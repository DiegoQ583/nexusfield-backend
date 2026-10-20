using NexusField.Domain.Interfaces;

namespace NexusField.Domain.Entities;

public class Repuesto : IAuditableEntity
{
    public int Id { get; set; }
    public required string Nombre { get; set; }
    public string? Descripcion { get; set; }
    public decimal Precio { get; set; }
    public int StockTotal { get; set; }
    public int StockReservado { get; set; }
    public string? FotoUrl { get; set; }
    public bool Activo { get; set; } = true;

    /// <summary>
    /// Propiedad calculada: stock disponible = total menos lo ya reservado para órdenes en curso.
    /// No se persiste en BD; EF Core la ignora por ser de solo lectura (sin setter).
    /// </summary>
    public int StockDisponible => StockTotal - StockReservado;

    // Auditoría
    public DateTime FechaCreacion { get; set; }
    public DateTime? FechaModificacion { get; set; }
    public int? UsuarioModificacionId { get; set; }
}
