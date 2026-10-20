using Microsoft.EntityFrameworkCore;
using NexusField.Application.Interfaces;
using NexusField.Domain.Entities;
using NexusField.Domain.Enums;
using NexusField.Infrastructure.Data;

namespace NexusField.Infrastructure.Repositories;

public class UsuarioRepository(NexusDbContext context) : IUsuarioRepository
{
    public async Task<Usuario?> ObtenerPorNombreUsuarioAsync(string username)
    {
        return await context.Usuarios
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.NombreUsuario == username);
    }

    /// <summary>
    /// Cuenta los técnicos con cuenta activa en el sistema.
    /// </summary>
    public async Task<int> ContarTecnicosActivosAsync()
    {
        return await context.Usuarios
            .AsNoTracking()
            .CountAsync(u => u.Rol == RolUsuario.Tecnico && u.Activo);
    }

    /// <summary>
    /// Obtiene todos los usuarios, opcionalmente filtrados por rol.
    /// Si el filtro no se puede parsear al enum, retorna todos.
    /// </summary>
    public async Task<List<Usuario>> ObtenerTodosAsync(string? rolFiltro)
    {
        var query = context.Usuarios.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(rolFiltro) && Enum.TryParse<RolUsuario>(rolFiltro, ignoreCase: true, out var rolParseado))
        {
            query = query.Where(u => u.Rol == rolParseado);
        }

        return await query.OrderBy(u => u.NombreCompleto).ToListAsync();
    }

    /// Verifica si ya existe un usuario con el mismo username O el mismo correo.
    /// Permite validar unicidad antes de insertar (evita depender solo de la FK de BD).
    public async Task<bool> ExisteUsuarioOCorreoAsync(string username, string correo)
    {
        // Compara ignorando mayúsculas/minúsculas para mayor seguridad
        return await context.Usuarios
            .AsNoTracking()
            .AnyAsync(u => u.NombreUsuario.ToLower() == username.ToLower() ||
                           u.Correo.ToLower() == correo.ToLower());
    }

    /// <summary>
    /// Obtiene un usuario por Id CON tracking habilitado para permitir actualizaciones.
    /// </summary>
    public async Task<Usuario?> ObtenerPorIdAsync(int id)
    {
        return await context.Usuarios.FindAsync(id);
    }

    public async Task AgregarAsync(Usuario usuario)
    {
        context.Usuarios.Add(usuario);
        await context.SaveChangesAsync();
    }

    public async Task ActualizarAsync(Usuario usuario)
    {
        context.Usuarios.Update(usuario);
        await context.SaveChangesAsync();
    }
}
