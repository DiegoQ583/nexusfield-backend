namespace NexusField.API.Middlewares;

/// <summary>
/// Middleware que inyecta la cabecera Content-Security-Policy en TODAS las respuestas HTTP.
/// Mitiga ataques XSS al restringir las fuentes permitidas de scripts, estilos, imágenes, etc.
/// Se ubica justo después del ExceptionHandler para que incluso las respuestas de error la lleven.
/// </summary>
public class CspMiddleware(RequestDelegate next)
{
    private const string CspValue =
        "default-src 'self'; " +
        "script-src 'self'; " +
        "style-src 'self' 'unsafe-inline'; " +
        "img-src 'self' data:; " +
        "font-src 'self'; " +
        "connect-src 'self' http://localhost:5173 https://localhost:7165;";

    public async Task InvokeAsync(HttpContext context)
    {
        // Excluimos las rutas de documentación para que Scalar pueda cargar su interfaz (JS/CSS externos)
        if (!context.Request.Path.StartsWithSegments("/scalar") &&
            !context.Request.Path.StartsWithSegments("/openapi"))
        {
            context.Response.Headers.Append("Content-Security-Policy",
                "default-src 'self'; script-src 'self'; style-src 'self' 'unsafe-inline'; img-src 'self' data:; font-src 'self'; connect-src 'self' http://localhost:5173 https://localhost:7165;");
        }

        await next(context);
    }
}
