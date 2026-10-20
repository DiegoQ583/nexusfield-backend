using FluentValidation;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NexusField.Application.DTOs.Usuarios;
using NexusField.Application.Services;

namespace NexusField.API.Controllers;

/// <summary>
/// CRUD de usuarios del sistema. Solo accesible por Despachadores.
/// Todos los endpoints mutativos requieren token CSRF.
/// </summary>
[ApiController]
[Route("api/v1/usuarios")]
[Authorize(Roles = "Despachador")]
public class UsuariosController(UsuarioService usuarioService) : ControllerBase
{
    /// <summary>
    /// Obtiene la lista de usuarios, opcionalmente filtrada por rol.
    /// </summary>
    /// <param name="rol">Filtro opcional: "Despachador" o "Tecnico".</param>
    [HttpGet]
    public async Task<IActionResult> ObtenerTodos([FromQuery] string? rol)
    {
        var usuarios = await usuarioService.ObtenerTodosAsync(rol);
        return Ok(usuarios);
    }

    /// <summary>
    /// Crea un nuevo usuario en el sistema.
    /// </summary>
    [HttpPost]
    [RequireAntiforgeryToken]
    public async Task<IActionResult> Crear(
        [FromBody] CrearUsuarioDto dto,
        [FromServices] IValidator<CrearUsuarioDto> validator)
    {
        await validator.ValidateAndThrowAsync(dto);

        var resultado = await usuarioService.CrearUsuarioAsync(dto);

        return Created($"/api/v1/usuarios/{resultado.Id}", resultado);
    }

    /// <summary>
    /// Actualiza los campos editables de un usuario existente.
    /// </summary>
    [HttpPut("{id}")]
    [RequireAntiforgeryToken]
    public async Task<IActionResult> Actualizar(int id, [FromBody] ActualizarUsuarioDto dto)
    {
        await usuarioService.ActualizarUsuarioAsync(id, dto);
        return Ok();
    }

    /// <summary>
    /// Invierte el estado activo/inactivo de un usuario (soft toggle).
    /// </summary>
    [HttpPatch("{id}/estado")]
    [RequireAntiforgeryToken]
    public async Task<IActionResult> CambiarEstado(int id)
    {
        await usuarioService.CambiarEstadoAsync(id);
        return NoContent();
    }
}
