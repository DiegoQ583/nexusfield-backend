using Microsoft.EntityFrameworkCore;
using NexusField.Application.Interfaces;
using NexusField.Domain.Entities;
using NexusField.Infrastructure.Data;
using NexusField.Application.DTOs.Inventario;
using NexusField.Domain.Enums;

namespace NexusField.Infrastructure.Repositories;

/// <summary>
/// Implementación EF Core del repositorio de repuestos.
/// CRUD completo para el módulo de Inventario.
/// ObtenerPorIdAsync usa tracking (sin AsNoTracking) porque las entidades
/// serán mutadas antes de persistir con SaveChangesAsync.
/// </summary>
public class RepuestoRepository(NexusDbContext context) : IRepuestoRepository
{
    /// <summary>
    /// Obtiene todos los repuestos sin filtrar.
    /// Usa AsNoTracking porque es solo lectura (listado).
    /// </summary>
    public async Task<List<Repuesto>> ObtenerTodosAsync()
    {
        return await context.Repuestos
            .AsNoTracking()
            .OrderBy(r => r.Nombre)
            .ToListAsync();
    }

    /// <summary>
    /// Obtiene un repuesto por Id CON tracking habilitado.
    /// Necesario porque el servicio modificará propiedades antes de SaveChanges.
    /// </summary>
    public async Task<Repuesto?> ObtenerPorIdAsync(int id)
    {
        return await context.Repuestos
            .FirstOrDefaultAsync(r => r.Id == id);
    }

    /// <summary>
    /// Verifica si un repuesto tiene registros en la tabla puente OrdenRepuesto.
    /// Usa AnyAsync para eficiencia (corta en el primer match sin cargar datos).
    /// </summary>
    public async Task<bool> TieneOrdenesAsociadasAsync(int id)
    {
        return await context.OrdenRepuestos
            .AsNoTracking()
            .AnyAsync(or => or.RepuestoId == id);
    }

    /// <summary>
    /// Registra un nuevo repuesto en el change tracker.
    /// NO persiste — eso lo hace SaveChangesAsync.
    /// </summary>
    public void Agregar(Repuesto repuesto)
    {
        context.Repuestos.Add(repuesto);
    }

    /// <summary>
    /// Marca un repuesto como Modified en el change tracker.
    /// </summary>
    public void Actualizar(Repuesto repuesto)
    {
        context.Repuestos.Update(repuesto);
    }

    /// <summary>
    /// Elimina un repuesto del change tracker (Hard Delete).
    /// </summary>
    public void Eliminar(Repuesto repuesto)
    {
        context.Repuestos.Remove(repuesto);
    }

    /// <summary>
    /// Persiste todos los cambios pendientes del DbContext.
    /// </summary>
    public async Task SaveChangesAsync()
    {
        await context.SaveChangesAsync();
    }

    public async Task<List<ReservaTecnicoDto>> ObtenerReservasActivasAsync(int repuestoId)
    {
        return await context.OrdenRepuestos
            .AsNoTracking()
            .Where(or => or.RepuestoId == repuestoId &&
                         or.Orden != null &&
                         (or.Orden.Estado == EstadoOrden.Pendiente ||
                          or.Orden.Estado == EstadoOrden.EnProgreso ||
                          or.Orden.Estado == EstadoOrden.Pausada))
            .Select(or => new ReservaTecnicoDto(
                or.Orden!.Tecnico != null ? or.Orden.Tecnico.NombreCompleto : "Sin asignar",
                or.Cantidad,
                or.OrdenId))
            .ToListAsync();
    }

    public async Task<List<Repuesto>> ObtenerCatalogoLigeroAsync()
    {
        return await context.Repuestos
            .AsNoTracking()
            .Where(r => r.Activo)
            .ToListAsync();
    }

    public async Task<List<Repuesto>> ObtenerVariosPorIdAsync(List<int> ids)
    {
        // VITAL: NO usar AsNoTracking() porque el servicio descontará inventario (StockTotal, Reservado)
        // y requerirá persistencia mediante SaveChanges (UnitOfWork).
        return await context.Repuestos
            .Where(r => ids.Contains(r.Id))
            .ToListAsync();
    }
}
