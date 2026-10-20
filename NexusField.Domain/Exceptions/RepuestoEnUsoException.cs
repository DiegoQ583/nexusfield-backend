namespace NexusField.Domain.Exceptions;

/// <summary>
/// Se lanza al intentar eliminar un repuesto que tiene órdenes de trabajo asociadas.
/// El GlobalExceptionHandler lo mapea a HTTP 409 Conflict.
/// </summary>
public class RepuestoEnUsoException(string mensaje) : Exception(mensaje);
