using NexusField.Application.DTOs.Inventario;
using NexusField.Application.Interfaces;
using NexusField.Domain.Entities;
using NexusField.Domain.Exceptions;

namespace NexusField.Application.Services;

/// Servicio de lógica de negocio para el módulo de Inventario (Repuestos).
/// Orquesta CRUD con subida de archivos y validaciones de integridad.
/// No requiere UnitOfWork porque las operaciones son single-table.
public class RepuestoService(
    IRepuestoRepository repuestoRepo,
    IFileStorageService fileStorage)
{
    private const string CarpetaFotos = "images/repuestos";

    /// Obtiene todos los repuestos mapeados a DTOs de respuesta.
    /// Incluye StockDisponible (propiedad calculada de la entidad).
    public async Task<List<RepuestoResponseDto>> ObtenerTodosAsync()
    {
        var repuestos = await repuestoRepo.ObtenerTodosAsync();

        return
        [
            .. repuestos.Select(r => new RepuestoResponseDto(
                r.Id,
                r.Nombre,
                r.Descripcion,
                r.Precio,
                r.StockTotal,
                r.StockReservado,
                r.StockDisponible,
                r.FotoUrl,
                r.Activo))
        ];
    }

    /// Crea un nuevo repuesto. Si incluye foto, la guarda en wwwroot y asigna la URL.
    public async Task<int> CrearAsync(GuardarRepuestoDto dto)
    {
        var repuesto = new Repuesto
        {
            Nombre = dto.Nombre,
            Descripcion = dto.Descripcion,
            Precio = dto.Precio,
            StockTotal = dto.StockTotal,
            StockReservado = 0,
            Activo = true
        };

        // Guardar foto si fue incluida en el request multipart.
        if (dto.Foto is not null)
        {
            repuesto.FotoUrl = await fileStorage.GuardarArchivoAsync(dto.Foto, CarpetaFotos);
        }

        repuestoRepo.Agregar(repuesto);
        await repuestoRepo.SaveChangesAsync();

        return repuesto.Id;
    }

    /// Actualiza un repuesto existente.
    /// Valida que el nuevo StockTotal no sea menor al StockReservado (órdenes en curso).
    /// Si trae foto nueva, borra la anterior del disco y guarda la nueva.
    public async Task ActualizarAsync(int id, GuardarRepuestoDto dto)
    {
        var repuesto = await repuestoRepo.ObtenerPorIdAsync(id)
            ?? throw new KeyNotFoundException($"Repuesto con Id {id} no fue encontrado.");

        // Validación de negocio: no se puede reducir stock por debajo de lo reservado.
        if (dto.StockTotal < repuesto.StockReservado)
        {
            throw new InvalidOperationException(
                $"El stock total ({dto.StockTotal}) no puede ser menor al stock reservado ({repuesto.StockReservado}) " +
                $"porque hay órdenes en curso que dependen de esas unidades.");
        }

        // 1. Si el usuario pidió borrar la foto actual
        if (dto.EliminarFoto && !string.IsNullOrEmpty(repuesto.FotoUrl))
        {
            fileStorage.BorrarArchivo(repuesto.FotoUrl); // Borra físicamente del disco
            repuesto.FotoUrl = null;                    // Limpia la columna en SQL
        }
        // 2. Si el usuario subió una foto nueva para reemplazar la anterior
        else if (dto.Foto != null)
        {
            if (!string.IsNullOrEmpty(repuesto.FotoUrl))
            {
                fileStorage.BorrarArchivo(repuesto.FotoUrl); // Borra la foto vieja del disco
            }
            repuesto.FotoUrl = await fileStorage.GuardarArchivoAsync(dto.Foto, CarpetaFotos);
        }

        // 3. Actualizamos los campos de texto
        repuesto.Nombre = dto.Nombre;
        repuesto.Descripcion = dto.Descripcion;
        repuesto.Precio = dto.Precio;
        repuesto.StockTotal = dto.StockTotal;

        repuestoRepo.Actualizar(repuesto);
        await repuestoRepo.SaveChangesAsync();
    }

    /// Elimina un repuesto con estrategia inteligente:
    /// Si tiene órdenes asociadas → Soft Delete (Activo = false). Lanza excepción informativa.
    /// Si no tiene órdenes → Hard Delete + borrado físico de foto.
    public async Task EliminarAsync(int id)
    {
        var repuesto = await repuestoRepo.ObtenerPorIdAsync(id)
            ?? throw new KeyNotFoundException($"Repuesto con Id {id} no fue encontrado.");

        if (repuesto.StockReservado > 0)
        {
            throw new InvalidOperationException("No se puede eliminar ni desactivar un repuesto que está actualmente reservado en órdenes activas.");
        }

        var tieneOrdenes = await repuestoRepo.TieneOrdenesAsociadasAsync(id);

        if (tieneOrdenes)
        {
            // Soft Delete: mantener la integridad histórica de las órdenes.
            repuesto.Activo = false;
            repuestoRepo.Actualizar(repuesto);
            await repuestoRepo.SaveChangesAsync();

            throw new RepuestoEnUsoException(
                $"El repuesto '{repuesto.Nombre}' tiene órdenes asociadas. Se desactivó (soft delete) en lugar de eliminarse.");
        }

        // Hard Delete: borrar foto física y eliminar el registro.
        if (!string.IsNullOrEmpty(repuesto.FotoUrl))
        {
            fileStorage.BorrarArchivo(repuesto.FotoUrl);
        }

        repuestoRepo.Eliminar(repuesto);
        await repuestoRepo.SaveChangesAsync();
    }

    /// Obtiene el detalle de un repuesto incluyendo la lista de técnicos que lo tienen reservado.
    public async Task<RepuestoDetalleDto> ObtenerPorIdAsync(int id)
    {
        var repuesto = await repuestoRepo.ObtenerPorIdAsync(id)
            ?? throw new KeyNotFoundException($"Repuesto con Id {id} no fue encontrado.");

        // Obtenemos la trazabilidad desde el repositorio
        var reservas = await repuestoRepo.ObtenerReservasActivasAsync(id);

        return new RepuestoDetalleDto(
            repuesto.Id,
            repuesto.Nombre,
            repuesto.Descripcion,
            repuesto.Precio,
            repuesto.StockTotal,
            repuesto.StockReservado,
            repuesto.StockDisponible,
            repuesto.FotoUrl,
            repuesto.Activo,
            reservas);
    }
}
