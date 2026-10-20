namespace NexusField.Application.DTOs.Ordenes;

/// <summary>
/// DTO completo para la vista de detalle de una orden de trabajo.
/// Incluye datos del activo, técnico, repuestos usados y evidencia (foto/firma).
/// </summary>
public record OrdenDetalleDto(
    int Id,
    string Titulo,
    string Descripcion,
    string ActivoNombre,
    string Ubicacion,
    string? LinkGoogleMaps,
    string? TelefonoContacto,
    string ClienteHistorico,
    string TecnicoNombre,
    string Prioridad,
    string Estado,
    DateTime FechaCreacion,
    DateTime? FechaCompletada,
    decimal HorasTrabajadas,
    decimal CostoTotal,
    decimal TarifaTecnico,
    decimal SubtotalManoObra,
    decimal SubtotalRepuestos,
    string? NotasTecnico,
    List<string> FotosUrl,
    string? FirmaClienteBase64,
    List<RepuestoUsadoDto> Repuestos);
