namespace NexusField.Application.DTOs.Inventario;

/// <summary>
/// DTO de salida para representar un repuesto en la vista de inventario.
/// Incluye StockDisponible (calculado) para que el frontend muestre disponibilidad sin calcular.
/// </summary>
public record RepuestoResponseDto(
    int Id,
    string Nombre,
    string? Descripcion,
    decimal Precio,
    int StockTotal,
    int StockReservado,
    int StockDisponible,
    string? FotoUrl,
    bool Activo);
