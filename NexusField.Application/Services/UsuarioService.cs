using NexusField.Application.DTOs.Usuarios;
using NexusField.Application.Interfaces;
using NexusField.Domain.Entities;
using NexusField.Domain.Exceptions;

namespace NexusField.Application.Services;

/// Servicio de lógica de negocio para la gestión CRUD de usuarios.
/// Orquesta validaciones de dominio, hashing de passwords y mapeo DTO ↔ Entidad.
public class UsuarioService(
    IUsuarioRepository usuarioRepository,
    IOrdenTrabajoRepository ordenRepo)
{
    /// Obtiene todos los usuarios, opcionalmente filtrados por rol.
    /// Mapea cada entidad a DTO para no exponer el PasswordHash.
    public async Task<List<UsuarioResponseDto>> ObtenerTodosAsync(string? rolFiltro)
    {
        var usuarios = await usuarioRepository.ObtenerTodosAsync(rolFiltro);

        return usuarios
            .Select(u => new UsuarioResponseDto(
                u.Id,
                u.NombreCompleto,
                u.NombreUsuario,
                u.Correo,
                u.Telefono,
                u.Especialidad,
                u.TarifaPorHora,
                u.Rol.ToString(),
                u.Activo))
            .ToList();
    }

    /// <summary>
    /// Crea un nuevo usuario validando unicidad de username y correo.
    /// El password se hashea con BCrypt (cost factor 11) antes de persistirse.
    /// </summary>
    public async Task<UsuarioResponseDto> CrearUsuarioAsync(CrearUsuarioDto dto)
    {
        // 1. Validación estricta de duplicados ANTES de tocar la base de datos
        bool existe = await usuarioRepository.ExisteUsuarioOCorreoAsync(dto.NombreUsuario, dto.Correo);
        if (existe)
        {
            throw new UsuarioDuplicadoException("El nombre de usuario o el correo electrónico ya están registrados en el sistema.");
        }

        // 2. Mapeo y creación (resto de tu código...)
        var usuario = new Usuario
        {
            NombreCompleto = dto.NombreCompleto,
            NombreUsuario = dto.NombreUsuario,
            Correo = dto.Correo,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Clave, 11),
            Telefono = dto.Telefono,
            Especialidad = dto.Especialidad,
            TarifaPorHora = dto.TarifaPorHora,
            Rol = dto.Rol,
            Activo = true
        };

        // 3. Persistir
        await usuarioRepository.AgregarAsync(usuario);

        // 4. Retornar DTO (el Id ya fue asignado por EF Core tras el SaveChanges)
        return new UsuarioResponseDto(
            usuario.Id,
            usuario.NombreCompleto,
            usuario.NombreUsuario,
            usuario.Correo,
            usuario.Telefono,
            usuario.Especialidad,
            usuario.TarifaPorHora,
            usuario.Rol.ToString(),
            usuario.Activo);
    }

    /// <summary>
    /// Actualiza los campos editables de un usuario existente.
    /// No modifica: Username, Password ni Rol (por diseño).
    /// </summary>
    public async Task ActualizarUsuarioAsync(int id, ActualizarUsuarioDto dto)
    {
        var usuario = await usuarioRepository.ObtenerPorIdAsync(id)
            ?? throw new KeyNotFoundException($"No se encontró un usuario con el Id {id}.");

        // Actualizar solo los campos permitidos
        usuario.NombreCompleto = dto.NombreCompleto;
        usuario.Correo = dto.Correo;
        usuario.Telefono = dto.Telefono;
        usuario.Especialidad = dto.Especialidad;
        usuario.TarifaPorHora = dto.TarifaPorHora;

        await usuarioRepository.ActualizarAsync(usuario);
    }

    /// <summary>
    /// Invierte el estado activo/inactivo de un usuario (soft toggle).
    /// </summary>
    public async Task CambiarEstadoAsync(int id)
    {
        var usuario = await usuarioRepository.ObtenerPorIdAsync(id)
            ?? throw new KeyNotFoundException($"No se encontró un usuario con el Id {id}.");

        if (usuario.Activo)
        {
            var tieneOrdenesActivas = await ordenRepo.TieneOrdenesActivasAsync(id);
            if (tieneOrdenesActivas)
            {
                throw new InvalidOperationException("No se puede desactivar un técnico que tiene órdenes en progreso o pendientes. Reasigne las órdenes primero.");
            }
        }

        usuario.Activo = !usuario.Activo;

        await usuarioRepository.ActualizarAsync(usuario);
    }
}
