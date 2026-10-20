using NexusField.Application.DTOs.Ordenes;
using NexusField.Application.DTOs.Tecnico;
using NexusField.Application.Interfaces;
using NexusField.Domain.Entities;
using NexusField.Domain.Enums;
using NexusField.Domain.Exceptions;

namespace NexusField.Application.Services;

public class TecnicoAppService(
    IOrdenTrabajoRepository ordenRepo,
    IRepuestoRepository repuestoRepo,
    IUnitOfWork unitOfWork,
    ICurrentUserService currentUserService,
    IFileStorageService fileStorageService)
{
    public async Task<List<OrdenTecnicoDto>> ObtenerMisOrdenesActivasAsync()
    {
        var tecnicoId = currentUserService.UsuarioId
            ?? throw new UnauthorizedAccessException("El técnico no está autenticado.");

        var ordenes = await ordenRepo.ObtenerOrdenesActivasPorTecnicoAsync(tecnicoId);

        return ordenes.Select(o => new OrdenTecnicoDto(
            o.Id,
            o.Titulo,
            o.Descripcion,
            o.ActivoFisico?.Nombre ?? "Sin activo",
            o.ActivoFisico?.Ubicacion ?? "Sin ubicación",
            o.ActivoFisico?.LinkGoogleMaps,
            o.ActivoFisico?.TelefonoContacto,
            o.Prioridad.ToString(),
            o.Estado.ToString(),
            o.RepuestosUsados.Select(ru => new RepuestoPreAsignadoDto(
                ru.RepuestoId,
                ru.Repuesto?.Nombre ?? "Desconocido",
                ru.Cantidad
            )).ToList()
        )).ToList();
    }

    public async Task<List<CatalogoOfflineDto>> ObtenerCatalogoOfflineAsync()
    {
        var repuestos = await repuestoRepo.ObtenerCatalogoLigeroAsync();

        return
        [
            .. repuestos.Select(r => new CatalogoOfflineDto(
                r.Id,
                r.Nombre,
                r.Precio,
                r.StockDisponible)) // <-- Le pasamos el stock calculado
        ];
    }

    public async Task<string> IniciarOrdenAsync(int ordenId)
    {
        var tecnicoId = currentUserService.UsuarioId
            ?? throw new UnauthorizedAccessException("El técnico no está autenticado.");

        var orden = await ordenRepo.ObtenerPorIdAsync(ordenId)
            ?? throw new KeyNotFoundException($"No se encontró la orden con Id {ordenId}.");

        if (orden.TecnicoId != tecnicoId)
        {
            throw new UnauthorizedAccessException("No tienes permisos para iniciar esta orden.");
        }

        if (orden.Estado != EstadoOrden.Pendiente && orden.Estado != EstadoOrden.Pausada)
        {
            throw new InvalidOperationException($"No se puede iniciar una orden en estado {orden.Estado}.");
        }

        orden.Estado = EstadoOrden.EnProgreso;

        ordenRepo.Actualizar(orden);
        await unitOfWork.CommitAsync();

        return orden.Titulo;
    }

    public async Task<string> PausarOrdenAsync(int ordenId, PausarOrdenDto dto)
    {
        var tecnicoId = currentUserService.UsuarioId
            ?? throw new UnauthorizedAccessException("El técnico no está autenticado.");

        var orden = await ordenRepo.ObtenerPorIdAsync(ordenId)
            ?? throw new KeyNotFoundException($"No se encontró la orden con Id {ordenId}.");

        if (orden.TecnicoId != tecnicoId)
        {
            throw new UnauthorizedAccessException("No tienes permisos para pausar esta orden.");
        }

        if (orden.Estado != EstadoOrden.EnProgreso)
        {
            throw new InvalidOperationException($"No se puede pausar una orden en estado {orden.Estado}.");
        }

        orden.Estado = EstadoOrden.Pausada;
        orden.NotasTecnico += $"\n[PAUSA]: {dto.MotivoPausa}";

        ordenRepo.Actualizar(orden);
        await unitOfWork.CommitAsync();

        return orden.Titulo;
    }

    public async Task CompletarOrdenAsync(CompletarOrdenDto dto)
    {
        var tecnicoId = currentUserService.UsuarioId
            ?? throw new UnauthorizedAccessException("El técnico no está autenticado.");

        await unitOfWork.BeginTransactionAsync();

        try
        {
            var orden = await ordenRepo.ObtenerParaCompletarAsync(dto.OrdenId)
                ?? throw new KeyNotFoundException("Orden no encontrada.");

            if (orden.TecnicoId != tecnicoId)
            {
                throw new UnauthorizedAccessException("No tienes permiso para completar esta orden.");
            }

            if (orden.Estado == EstadoOrden.Completada || orden.Estado == EstadoOrden.Cancelada)
            {
                throw new InvalidOperationException("La orden ya está cerrada y no puede ser modificada.");
            }

            // Paso A (Rollback Lógico de Reservas)
            if (orden.RepuestosUsados.Any())
            {
                foreach (var ordenRepuesto in orden.RepuestosUsados)
                {
                    var repuesto = await repuestoRepo.ObtenerPorIdAsync(ordenRepuesto.RepuestoId);
                    if (repuesto != null)
                    {
                        repuesto.StockReservado -= ordenRepuesto.Cantidad;
                        if (repuesto.StockReservado < 0) repuesto.StockReservado = 0;
                        repuestoRepo.Actualizar(repuesto);
                    }
                }
                orden.RepuestosUsados.Clear();
            }

            // Paso B (Descuento Real y Costo de Repuestos)
            var repuestosIds = dto.Repuestos.Select(r => r.RepuestoId).Distinct().ToList();
            var repuestosDb = await repuestoRepo.ObtenerVariosPorIdAsync(repuestosIds);
            var repuestosDict = repuestosDb.ToDictionary(r => r.Id);

            decimal costoRepuestos = 0;

            foreach (var reserva in dto.Repuestos)
            {
                if (!repuestosDict.TryGetValue(reserva.RepuestoId, out var repuesto) || !repuesto.Activo)
                {
                    throw new InvalidOperationException($"Repuesto inactivo o no encontrado: {reserva.RepuestoId}.");
                }

                if (reserva.Cantidad > repuesto.StockTotal)
                {
                    throw new StockInsuficienteException(repuesto.Nombre);
                }

                repuesto.StockTotal -= reserva.Cantidad;
                repuestoRepo.Actualizar(repuesto);

                costoRepuestos += reserva.Cantidad * repuesto.Precio;
                orden.RepuestosUsados.Add(new OrdenRepuesto
                {
                    RepuestoId = repuesto.Id,
                    Cantidad = reserva.Cantidad,
                    PrecioHistorico = repuesto.Precio
                });
            }

            // Paso C (Cálculo Financiero y Cierre)
            decimal costoManoObra = dto.HorasTrabajadas * orden.Tecnico!.TarifaPorHora;
            orden.CostoTotal = costoRepuestos + costoManoObra;
            orden.HorasTrabajadas = dto.HorasTrabajadas;
            orden.NotasTecnico = dto.NotasTecnico;
            orden.Estado = EstadoOrden.Completada;
            orden.FechaCompletada = DateTime.UtcNow;

            // Paso D (Evidencias Físicas: Galería de Fotos y Firma)
            if (dto.Fotos != null && dto.Fotos.Count > 0)
            {
                var urlsGuardadas = new List<string>();
                foreach (var foto in dto.Fotos)
                {
                    var url = await fileStorageService.GuardarArchivoAsync(foto, "images/evidencias");
                    urlsGuardadas.Add(url);
                }
                // Guardamos todas las URLs separadas por ';'
                orden.FotoUrl = string.Join(";", urlsGuardadas);
            }

            if (dto.Firma is not null)
            {
                orden.FirmaClienteBase64 = await fileStorageService.GuardarArchivoAsync(dto.Firma, "images/firmas");
            }

            ordenRepo.Actualizar(orden);
            await unitOfWork.CommitAsync();
        }
        catch
        {
            await unitOfWork.RollbackAsync();
            throw;
        }
    }

    public async Task RechazarOrdenAsync(int ordenId, RechazarOrdenDto dto)
    {
        var tecnicoId = currentUserService.UsuarioId
            ?? throw new UnauthorizedAccessException("El técnico no está autenticado.");

        var orden = await ordenRepo.ObtenerPorIdAsync(ordenId)
            ?? throw new KeyNotFoundException($"No se encontró la orden con Id {ordenId}.");

        if (orden.TecnicoId != tecnicoId)
        {
            throw new UnauthorizedAccessException("No tienes permiso para rechazar esta orden.");
        }

        if (orden.Estado != EstadoOrden.Pendiente)
        {
            throw new InvalidOperationException($"Solo se pueden rechazar órdenes Pendientes. La orden está en estado {orden.Estado}.");
        }

        orden.TecnicoId = null;
        orden.NotasTecnico = $"[RECHAZADA]: {dto.MotivoRechazo}";
        orden.Estado = EstadoOrden.Pendiente;

        ordenRepo.Actualizar(orden);
        await unitOfWork.CommitAsync();
    }

    public async Task<OrdenPaginadaResponseDto> ObtenerHistorialPaginadoAsync(int page, int pageSize, string? searchTerm, string? estado, string? filtroTiempo)
    {
        var tecnicoId = currentUserService.UsuarioId
            ?? throw new UnauthorizedAccessException("El técnico no está autenticado.");

        var (ordenes, total) = await ordenRepo.ObtenerPaginadasAsync(page, pageSize, searchTerm, tecnicoId, estado, null, filtroTiempo);

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

    public async Task<string> ReanudarOrdenAsync(int ordenId)
    {
        var tecnicoId = currentUserService.UsuarioId
            ?? throw new UnauthorizedAccessException("Usuario no autenticado.");

        var orden = await ordenRepo.ObtenerPorIdAsync(ordenId)
            ?? throw new KeyNotFoundException($"Orden con Id {ordenId} no encontrada.");

        if (orden.TecnicoId != tecnicoId)
            throw new UnauthorizedAccessException("No tienes permisos para reanudar esta orden.");

        if (orden.Estado != EstadoOrden.Pausada)
            throw new InvalidOperationException($"Solo se pueden reanudar órdenes en estado Pausada. Estado actual: {orden.Estado}");

        orden.Estado = EstadoOrden.EnProgreso;
        ordenRepo.Actualizar(orden);
        await unitOfWork.CommitAsync();

        return orden.Titulo;
    }
}
