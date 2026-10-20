namespace NexusField.Domain.Entities;

public class OrdenRepuesto
{
    // Llave compuesta (OrdenId + RepuestoId) configurada en el DbContext via Fluent API
    public int OrdenId { get; set; }
    public int RepuestoId { get; set; }
    public int Cantidad { get; set; }

    /// <summary>
    /// Snapshot del precio del repuesto al momento de usarse en la orden.
    /// Garantiza integridad histórica aunque el precio del repuesto cambie después.
    /// </summary>
    public decimal PrecioHistorico { get; set; }

    // Navegación
    public OrdenTrabajo? Orden { get; set; }
    public Repuesto? Repuesto { get; set; }
}
