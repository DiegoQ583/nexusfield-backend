using NexusField.Domain.Enums;

namespace NexusField.Application.DTOs.Ordenes;

/// DTO de entrada para la creación de una nueva orden de trabajo.
/// La validación XSS se aplica en CrearOrdenValidator (FluentValidation).

// Nuevo DTO para especificar la cantidad
public record RepuestoReservaDto(int RepuestoId, int Cantidad);

// DTO actualizado
public record CrearOrdenDto(
    string Titulo,
    string Descripcion,
    int ActivoId,
    int TecnicoId,
    Prioridad Prioridad,
    List<RepuestoReservaDto> Repuestos); // <-- Ahora es una lista de objetos
