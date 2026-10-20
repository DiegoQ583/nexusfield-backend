using NexusField.Domain.Entities;

namespace NexusField.Application.Interfaces;

public interface IUsuarioRepository
{
    Task<Usuario?> ObtenerPorNombreUsuarioAsync(string username);
    Task<int> ContarTecnicosActivosAsync();
    Task<List<Usuario>> ObtenerTodosAsync(string? rolFiltro);
    Task<bool> ExisteUsuarioOCorreoAsync(string username, string correo);
    Task<Usuario?> ObtenerPorIdAsync(int id);
    Task AgregarAsync(Usuario usuario);
    Task ActualizarAsync(Usuario usuario);
}

