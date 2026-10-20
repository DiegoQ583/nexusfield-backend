using Microsoft.EntityFrameworkCore.Storage;
using NexusField.Application.Interfaces;

namespace NexusField.Infrastructure.Data;

/// <summary>
/// Implementación del patrón Unit of Work sobre EF Core.
/// Centraliza la persistencia (SaveChanges) y la gestión de transacciones.
/// Los repositorios NUNCA llaman a SaveChanges; solo mutan entidades en memoria.
/// </summary>
public class UnitOfWork(NexusDbContext context) : IUnitOfWork
{
    private IDbContextTransaction? _transaction;

    /// <summary>
    /// Inicia una transacción explícita. Necesaria para operaciones multi-tabla
    /// (ej. crear orden + reservar stock de repuestos atómicamente).
    /// </summary>
    public async Task BeginTransactionAsync()
    {
        _transaction = await context.Database.BeginTransactionAsync();
    }

    /// <summary>
    /// Persiste todos los cambios pendientes del DbContext y confirma la transacción.
    /// El orden es crítico: SaveChanges primero (genera IDs), Commit después (hace durables los cambios).
    /// </summary>
    public async Task CommitAsync()
    {
        await context.SaveChangesAsync();

        if (_transaction is not null)
        {
            await _transaction.CommitAsync();
            await _transaction.DisposeAsync();
            _transaction = null;
        }
    }

    /// <summary>
    /// Revierte la transacción y descarta todos los cambios pendientes.
    /// Se invoca en el catch del servicio para garantizar atomicidad ante cualquier fallo.
    /// </summary>
    public async Task RollbackAsync()
    {
        if (_transaction is not null)
        {
            await _transaction.RollbackAsync();
            await _transaction.DisposeAsync();
            _transaction = null;
        }
    }
}
