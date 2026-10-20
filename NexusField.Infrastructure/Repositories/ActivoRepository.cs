using Microsoft.EntityFrameworkCore;
using NexusField.Application.Interfaces;
using NexusField.Domain.Entities;
using NexusField.Infrastructure.Data;

namespace NexusField.Infrastructure.Repositories;

/// <summary>
/// Implementación EF Core del repositorio de activos físicos.
/// Solo lectura por ahora (Sprint 2: valida existencia al crear órdenes).
/// </summary>
public class ActivoRepository(NexusDbContext context) : IActivoRepository
{
    public async Task<(List<Activo> Activos, int Total)> ObtenerPaginadosAsync(int page, int pageSize, string? searchTerm)
    {
        var query = context.Activos.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            query = query.Where(a => a.Nombre.Contains(searchTerm) || a.Ubicacion.Contains(searchTerm) || a.ClienteActual.Contains(searchTerm));
        }

        var total = await query.CountAsync();
        var activos = await query
            .OrderBy(a => a.Nombre)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (activos, total);
    }

    public async Task<List<Activo>> BuscarPorTerminoAsync(string query)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return [];
        }

        return await context.Activos
            .AsNoTracking()
            .Where(a => a.Activo_ == true && (a.Nombre.Contains(query) || a.Ubicacion.Contains(query)))
            .OrderBy(a => a.Nombre)
            .Take(10)
            .ToListAsync();
    }

    public async Task<Activo?> ObtenerPorIdAsync(int id)
    {
        return await context.Activos.FindAsync(id);
    }

    public void Agregar(Activo activo)
    {
        context.Activos.Add(activo);
    }

    public void Actualizar(Activo activo)
    {
        context.Activos.Update(activo);
    }

    public async Task SaveChangesAsync()
    {
        await context.SaveChangesAsync();
    }
}
