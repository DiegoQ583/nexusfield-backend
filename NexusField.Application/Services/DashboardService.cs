using NexusField.Application.DTOs.Dashboard;
using NexusField.Application.Interfaces;
using NexusField.Domain.Enums;

namespace NexusField.Application.Services;

/// <summary>
/// Servicio de lógica pura para el Dashboard del Despachador.
/// Orquesta las consultas a los repositorios y devuelve un DTO consolidado.
/// Usa Task.WhenAll para ejecutar las 4 queries de forma concurrente.
/// </summary>
public class DashboardService(
    IOrdenTrabajoRepository ordenTrabajoRepository,
    IUsuarioRepository usuarioRepository)
{
    public async Task<DashboardMetricsDto> ObtenerMetricasAsync(string filtro)
    {
        string filtroNormalizado = filtro.ToLower();
        DateTime fechaInicio = filtroNormalizado switch
        {
            "hoy" => DateTime.UtcNow.Date,
            "semana" => DateTime.UtcNow.Date.AddDays(-7),
            "mes" => DateTime.UtcNow.Date.AddDays(-30),
            _ => DateTime.UtcNow.Date.AddDays(-30)
        };

        // 1. Consultas a BD
        var pendientes = await ordenTrabajoRepository.ContarPorEstadoAsync(EstadoOrden.Pendiente, fechaInicio);
        var completadas = await ordenTrabajoRepository.ContarPorEstadoAsync(EstadoOrden.Completada, fechaInicio);
        var tecnicos = await usuarioRepository.ContarTecnicosActivosAsync();
        var costoTotal = await ordenTrabajoRepository.SumarCostoTotalAsync(fechaInicio);

        // NUEVA LLAMADA: Obtenemos las prioridades
        var prioridades = await ordenTrabajoRepository.ObtenerDistribucionPrioridadesAsync();

        var fechasCrudas = await ordenTrabajoRepository.ObtenerFechasCompletadasAsync(fechaInicio);

        // 2. Procesamiento del Gráfico en Memoria
        List<GraficoPuntoDto> grafico = GenerarGrafico(filtroNormalizado, fechasCrudas);

        // Pasamos el nuevo objeto 'prioridades' al final
        return new DashboardMetricsDto(pendientes, completadas, tecnicos, costoTotal, grafico, prioridades);
    }

    private static List<GraficoPuntoDto> GenerarGrafico(string filtro, List<DateTime> fechas)
    {
        return filtro switch
        {
            "hoy" => GenerarGraficoHoy(fechas),
            "semana" => GenerarGraficoSemana(fechas),
            _ => GenerarGraficoMes(fechas)
        };
    }

    private static List<GraficoPuntoDto> GenerarGraficoHoy(List<DateTime> fechas)
    {
        int[] conteos = new int[5]; // 5 Bloques lógicos
        foreach (var fecha in fechas)
        {
            // Convertimos a hora local por si la BD guarda en UTC
            int hora = fecha.ToLocalTime().Hour;

            if (hora < 8) conteos[0]++;         // Madrugada (00:00 - 07:59)
            else if (hora < 12) conteos[1]++;   // Mañana (08:00 - 11:59)
            else if (hora < 16) conteos[2]++;   // Tarde (12:00 - 15:59)
            else if (hora < 20) conteos[3]++;   // Fin de turno (16:00 - 19:59)
            else conteos[4]++;                  // Noche (20:00 - 23:59)
        }

        return new List<GraficoPuntoDto>
        {
            new("00:00-08:00", conteos[0]),
            new("08:00-12:00", conteos[1]),
            new("12:00-16:00", conteos[2]),
            new("16:00-20:00", conteos[3]),
            new("20:00-24:00", conteos[4])
        };
    }

    private static List<GraficoPuntoDto> GenerarGraficoSemana(List<DateTime> fechas)
    {
        int[] conteos = new int[7];
        foreach (var fecha in fechas)
        {
            // DayOfWeek.Sunday es 0. Lo convertimos para que Lunes sea 0 y Domingo 6.
            int indice = fecha.DayOfWeek == DayOfWeek.Sunday ? 6 : (int)fecha.DayOfWeek - 1;
            conteos[indice]++;
        }

        string[] etiquetas = ["Lun", "Mar", "Mié", "Jue", "Vie", "Sáb", "Dom"];
        return conteos.Select((cantidad, i) => new GraficoPuntoDto(etiquetas[i], cantidad)).ToList();
    }

    private static List<GraficoPuntoDto> GenerarGraficoMes(List<DateTime> fechas)
    {
        int[] conteos = new int[5]; // Máximo 5 semanas en un mes
        foreach (var fecha in fechas)
        {
            int indice = (fecha.Day - 1) / 7;
            if (indice > 4) indice = 4; // Los días 29, 30 y 31 caen en la semana 5
            conteos[indice]++;
        }

        return conteos.Select((cantidad, i) => new GraficoPuntoDto($"Semana {i + 1}", cantidad)).ToList();
    }
}
