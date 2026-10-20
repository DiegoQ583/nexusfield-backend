using Microsoft.AspNetCore.SignalR;

namespace NexusField.API.Hubs;

/// <summary>
/// Hub de SignalR para notificaciones en tiempo real.
/// Punto de extensión para futuros eventos:
/// - Nueva orden asignada a un técnico
/// - Orden completada (actualizar dashboard)
/// - Alertas de stock bajo de repuestos
/// </summary>
public class NotificacionesHub : Hub;
