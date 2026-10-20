namespace NexusField.Domain.Exceptions;

public class StockInsuficienteException(string nombreRepuesto) 
    : Exception($"Stock insuficiente para el repuesto: '{nombreRepuesto}'. No hay suficientes unidades disponibles para reservar.")
{
}