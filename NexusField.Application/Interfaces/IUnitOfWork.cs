namespace NexusField.Application.Interfaces;

/// <summary>
/// Contrato del Unit of Work para orquestar transacciones atómicas.
/// REGLA DE ORO: Los repositorios agregan/modifican entidades en memoria.
/// Solo el UnitOfWork persiste los cambios (SaveChanges + Commit).
/// </summary>
public interface IUnitOfWork
{
    /// <summary>
    /// Inicia una transacción explícita en la base de datos.
    /// </summary>
    Task BeginTransactionAsync();

    /// <summary>
    /// Persiste todos los cambios pendientes y confirma la transacción.
    /// </summary>
    Task CommitAsync();

    /// <summary>
    /// Revierte la transacción y descarta todos los cambios pendientes.
    /// </summary>
    Task RollbackAsync();
}
