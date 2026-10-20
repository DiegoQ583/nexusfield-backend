using NexusField.Application.DTOs.Ordenes;
using NexusField.Application.Interfaces;
using NexusField.Domain.Entities;
using NexusField.Domain.Enums;
using NexusField.Domain.Exceptions;

namespace NexusField.Application.Services;

/// <summary>
/// Servicio de lógica de negocio para el módulo de Órdenes de Trabajo.
/// Orquesta consultas al repositorio, mapea entidades de dominio a DTOs,
/// y maneja la creación transaccional con el patrón UnitOfWork.
/// No tiene dependencia alguna de EF Core o IQueryable.
/// </summary>
public class OrdenTrabajoService(
    IOrdenTrabajoRepository ordenRepo,
    IUnitOfWork unitOfWork,
    IActivoRepository activoRepo,
    IRepuestoRepository repuestoRepo)
{
    /// <summary>
    /// Obtiene una página de órdenes con filtros opcionales.
    /// Calcula TotalPages y mapea cada entidad a OrdenDto manejando nulos en navegación.
    /// </summary>
    public async Task<OrdenPaginadaResponseDto> ObtenerPaginadasAsync(
        int page, int pageSize, string? searchTerm, int? tecnicoId, string? estado, string? prioridad, string? filtroTiempo) // <-- Recibe filtroTiempo
    {
        // Pasa el filtroTiempo al repositorio
        var (ordenes, total) = await ordenRepo.ObtenerPaginadasAsync(page, pageSize, searchTerm, tecnicoId, estado, prioridad, filtroTiempo); 

        var totalPages = (int)Math.Ceiling(total / (double)pageSize);

        List<OrdenDto> data =
        [
            .. ordenes.Select(o => new OrdenDto(
                o.Id,
                o.Titulo,
                o.ActivoFisico?.Nombre ?? "Sin activo",
                o.Tecnico?.NombreCompleto ?? "Sin asignar",
                o.Prioridad.ToString(),
                o.Estado.ToString(),
                o.FechaCreacion))
        ];

        return new OrdenPaginadaResponseDto(data, total, page, totalPages);
    }

    /// <summary>
    /// Obtiene el detalle completo de una orden incluyendo repuestos usados.
    /// Lanza excepción si la orden no existe (capturada por GlobalExceptionHandler → 404).
    /// </summary>
    public async Task<OrdenDetalleDto> ObtenerDetalleAsync(int id)
    {
        var orden = await ordenRepo.ObtenerDetallePorIdAsync(id)
            ?? throw new KeyNotFoundException($"Orden de trabajo con Id {id} no fue encontrada.");

        List<RepuestoUsadoDto> repuestos =
        [
            .. orden.RepuestosUsados.Select(ru => new RepuestoUsadoDto(
                ru.Repuesto?.Nombre ?? "Repuesto eliminado",
                ru.Cantidad,
                ru.PrecioHistorico))
        ];

        // Cálculos financieros
        decimal tarifaTecnico = orden.Tecnico?.TarifaPorHora ?? 0;
        decimal subtotalManoObra = orden.HorasTrabajadas * tarifaTecnico;
        decimal subtotalRepuestos = repuestos.Sum(r => r.Cantidad * r.PrecioHistorico);

        var fotosGaleria = !string.IsNullOrWhiteSpace(orden.FotoUrl)
            ? orden.FotoUrl.Split(';', StringSplitOptions.RemoveEmptyEntries).ToList()
            : new List<string>();

        return new OrdenDetalleDto(
            orden.Id,
            orden.Titulo,
            orden.Descripcion,
            orden.ActivoFisico?.Nombre ?? "Sin activo",
            orden.ActivoFisico?.Ubicacion ?? "Sin ubicación",
            orden.ActivoFisico?.LinkGoogleMaps,
            orden.ActivoFisico?.TelefonoContacto,
            orden.ClienteHistorico,
            orden.Tecnico?.NombreCompleto ?? "Sin asignar",
            orden.Prioridad.ToString(),
            orden.Estado.ToString(),
            orden.FechaCreacion,
            orden.FechaCompletada,
            orden.HorasTrabajadas,
            orden.CostoTotal,
            tarifaTecnico,
            subtotalManoObra,
            subtotalRepuestos,
            orden.NotasTecnico,
            fotosGaleria,
            orden.FirmaClienteBase64,
            repuestos);
    }

    public async Task<int> CrearOrdenAsync(CrearOrdenDto dto)
    {
        await unitOfWork.BeginTransactionAsync();

        try
        {
            var activo = await activoRepo.ObtenerPorIdAsync(dto.ActivoId)
                ?? throw new KeyNotFoundException("Activo no encontrado.");

            if (!activo.Activo_)
                throw new InvalidOperationException("No se puede crear una orden para un activo dado de baja.");

            var orden = new OrdenTrabajo
            {
                Titulo = dto.Titulo,
                Descripcion = dto.Descripcion,
                ActivoId = dto.ActivoId,
                TecnicoId = dto.TecnicoId,
                Prioridad = dto.Prioridad,
                Estado = EstadoOrden.Pendiente,
                ClienteHistorico = activo.ClienteActual
            };

            // NUEVA LÓGICA: Iteramos sobre los objetos con cantidad
            if (dto.Repuestos != null && dto.Repuestos.Any())
            {
                foreach (var reserva in dto.Repuestos)
                {
                    var repuesto = await repuestoRepo.ObtenerPorIdAsync(reserva.RepuestoId)
                        ?? throw new KeyNotFoundException($"Repuesto con ID {reserva.RepuestoId} no encontrado.");

                    if (!repuesto.Activo)
                        throw new InvalidOperationException($"El repuesto {repuesto.Nombre} está dado de baja.");

                    // NUEVO: Validación de Stock Disponible
                    if (reserva.Cantidad <= 0)
                        throw new InvalidOperationException($"La cantidad para {repuesto.Nombre} debe ser mayor a 0.");

                    if (reserva.Cantidad > repuesto.StockDisponible)
                        throw new StockInsuficienteException(repuesto.Nombre); // <-- Bloquea si no hay suficiente

                    // Sumamos la cantidad solicitada al stock reservado
                    repuesto.StockReservado += reserva.Cantidad;
                    repuestoRepo.Actualizar(repuesto);

                    orden.RepuestosUsados.Add(new OrdenRepuesto
                    {
                        RepuestoId = reserva.RepuestoId,
                        Cantidad = reserva.Cantidad, // <-- Usamos la cantidad del DTO
                        PrecioHistorico = repuesto.Precio
                    });
                }
            }

            ordenRepo.Agregar(orden);
            await unitOfWork.CommitAsync();

            return orden.Id;
        }
        catch
        {
            // Rollback total: descarta TODOS los cambios pendientes en el DbContext.
            await unitOfWork.RollbackAsync();
            throw;
        }
    }

    public async Task ReasignarOrdenAsync(int ordenId, ReasignarOrdenDto dto)
    {
        await unitOfWork.BeginTransactionAsync();

        try
        {
            var orden = await ordenRepo.ObtenerPorIdAsync(ordenId)
                ?? throw new KeyNotFoundException($"Orden de trabajo con Id {ordenId} no fue encontrada.");

            if (orden.Estado != EstadoOrden.Pendiente)
                throw new InvalidOperationException("Solo se pueden reasignar órdenes en estado Pendiente.");

            orden.TecnicoId = dto.NuevoTecnicoId;

            ordenRepo.Actualizar(orden);
            await unitOfWork.CommitAsync();
        }
        catch
        {
            await unitOfWork.RollbackAsync();
            throw;
        }
    }
}
