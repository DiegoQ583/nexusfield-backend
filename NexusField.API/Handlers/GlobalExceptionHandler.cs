using FluentValidation;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using NexusField.Domain.Exceptions;

namespace NexusField.API.Handlers;

public class GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        // Pattern Matching avanzado para extraer StatusCode, Title y Detail
        var (statusCode, title, detail) = exception switch
        {
            CuentaBloqueadaException ex => (StatusCodes.Status429TooManyRequests, ex.Message, "Espere el tiempo indicado antes de reintentar."),
            CredencialesInvalidasException ex => (StatusCodes.Status401Unauthorized, "Credenciales inválidas", ex.Message),
            UsuarioDuplicadoException ex => (StatusCodes.Status400BadRequest, "Datos duplicados", ex.Message),
            RepuestoEnUsoException ex => (StatusCodes.Status409Conflict, "Conflicto de Integridad", ex.Message),
            KeyNotFoundException ex => (StatusCodes.Status404NotFound, "Recurso no encontrado", ex.Message),
            ValidationException => (StatusCodes.Status400BadRequest, "Error de validación", "Revise los datos enviados."),
            InvalidOperationException ex when ex.Message.Contains("anti-forgery") => (StatusCodes.Status400BadRequest, "Error de Seguridad (CSRF)", "El token de seguridad es inválido o no fue enviado."),
            InvalidOperationException ex => (StatusCodes.Status400BadRequest, "Operación no permitida", ex.Message),
            StockInsuficienteException ex => (StatusCodes.Status400BadRequest, "Stock insuficiente", ex.Message),
            UnauthorizedAccessException ex => (StatusCodes.Status403Forbidden, "Acceso no autorizado", ex.Message),
            _ => (StatusCodes.Status500InternalServerError, "Error interno del servidor", "Ocurrió un error inesperado. Contacte soporte.")
        };

        // Logging estratégico
        if (statusCode == StatusCodes.Status500InternalServerError)
            logger.LogError(exception, "Error crítico no controlado en {Path}", httpContext.Request.Path);
        else
            logger.LogWarning("Excepción de dominio ({StatusCode}) en {Path}: {Message}", statusCode, httpContext.Request.Path, exception.Message);

        // Construcción del estándar RFC 7807
        var problemDetails = new ProblemDetails
        {
            Status = statusCode,
            Title = title,
            Detail = detail,
            Instance = httpContext.Request.Path
        };

        // Inyección de errores de FluentValidation si aplica
        if (exception is ValidationException validationException)
        {
            problemDetails.Extensions["errors"] = validationException.Errors
                .GroupBy(e => e.PropertyName)
                .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray());
        }

        httpContext.Response.StatusCode = statusCode;
        await httpContext.Response.WriteAsJsonAsync(problemDetails, cancellationToken);

        return true; // Le dice a .NET que la excepción ya fue manejada y detiene la propagación
    }
}