namespace NexusField.Application.DTOs.Inventario;

public record ReservaTecnicoDto(string TecnicoNombre, int Cantidad, int OrdenId);

public record RepuestoDetalleDto(
    int Id,
    string Nombre,
    string? Descripcion,
    decimal Precio,
    int StockTotal,
    int StockReservado,
    int StockDisponible,
    string? FotoUrl,
    bool Activo,
    List<ReservaTecnicoDto> ReservasActivas); // <-- Lista de técnicos con reservas