using NexusField.Application.DTOs.Activos;
using NexusField.Application.Interfaces;
using NexusField.Domain.Entities;

namespace NexusField.Application.Services;

public class ActivoService(IActivoRepository activoRepository, IOrdenTrabajoRepository ordenRepo)
{
    public async Task<ActivoPaginadoResponseDto> ObtenerPaginadosAsync(int page, int pageSize, string? searchTerm)
    {
        var (activos, total) = await activoRepository.ObtenerPaginadosAsync(page, pageSize, searchTerm);
        var totalPages = (int)Math.Ceiling(total / (double)pageSize);

        var data = activos.Select(a => new ActivoResponseDto(
            a.Id,
            a.Nombre,
            a.Descripcion,
            a.Ubicacion,
            a.ClienteActual,
            a.Estado,
            a.Activo_,
            a.LinkGoogleMaps,
            a.TelefonoContacto)).ToList();

        return new ActivoPaginadoResponseDto(data, total, page, totalPages);
    }

    public async Task<List<ActivoLigeroDto>> BuscarActivosAsync(string query)
    {
        var activos = await activoRepository.BuscarPorTerminoAsync(query);
        return activos.Select(a => new ActivoLigeroDto(a.Id, a.Nombre)).ToList();
    }

    public async Task<int> CrearActivoAsync(GuardarActivoDto dto)
    {
        var activo = new Activo
        {
            Nombre = dto.Nombre,
            Descripcion = dto.Descripcion,
            Ubicacion = dto.Ubicacion,
            ClienteActual = dto.ClienteActual,
            Estado = dto.Estado,
            Activo_ = true,
            LinkGoogleMaps = dto.LinkGoogleMaps,
            TelefonoContacto = dto.TelefonoContacto
        };

        activoRepository.Agregar(activo);
        await activoRepository.SaveChangesAsync();

        return activo.Id;
    }

    public async Task ActualizarActivoAsync(int id, GuardarActivoDto dto)
    {
        var activo = await activoRepository.ObtenerPorIdAsync(id)
            ?? throw new KeyNotFoundException($"No se encontró un activo con el Id {id}.");

        activo.Nombre = dto.Nombre;
        activo.Descripcion = dto.Descripcion;
        activo.Ubicacion = dto.Ubicacion;
        activo.ClienteActual = dto.ClienteActual;
        activo.Estado = dto.Estado;
        activo.LinkGoogleMaps = dto.LinkGoogleMaps;
        activo.TelefonoContacto = dto.TelefonoContacto;

        activoRepository.Actualizar(activo);
        await activoRepository.SaveChangesAsync();
    }

    public async Task CambiarEstadoAsync(int id)
    {
        var activo = await activoRepository.ObtenerPorIdAsync(id)
            ?? throw new KeyNotFoundException($"No se encontró un activo con el Id {id}.");

        activo.Activo_ = !activo.Activo_;

        activoRepository.Actualizar(activo);
        await activoRepository.SaveChangesAsync();
    }

    public async Task<List<HistorialActivoDto>> ObtenerHistorialAsync(int activoId)
    {
        var ordenes = await ordenRepo.ObtenerHistorialPorActivoAsync(activoId);

        return ordenes.Select(o => new HistorialActivoDto(
            o.Id,
            o.Titulo,
            o.Tecnico?.NombreCompleto ?? "Sin asignar",
            o.FechaCompletada,
            o.NotasTecnico
        )).ToList();
    }
}
