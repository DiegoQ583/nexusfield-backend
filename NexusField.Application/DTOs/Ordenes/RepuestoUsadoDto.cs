namespace NexusField.Application.DTOs.Ordenes;

/// <summary>
/// DTO que representa un repuesto consumido en una orden de trabajo.
/// Usa PrecioHistorico (snapshot) en lugar del precio actual del repuesto.
/// </summary>
public record RepuestoUsadoDto(
    string RepuestoNombre,
    int Cantidad,
    decimal PrecioHistorico);
