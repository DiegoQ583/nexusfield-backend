namespace NexusField.Application.DTOs.Dashboard;

public record GraficoPuntoDto(string Etiqueta, int Cantidad);

public record DashboardMetricsDto(
    int OrdenesPendientes,
    int OrdenesCompletadas,
    int TecnicosActivos,
    decimal CostoTotal,
    List<GraficoPuntoDto> Grafico,
    DistribucionPrioridadDto Prioridades);
