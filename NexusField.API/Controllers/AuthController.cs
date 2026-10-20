using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using NexusField.API.Providers;
using NexusField.Application.DTOs.Auth;
using NexusField.Application.Services;
using Microsoft.AspNetCore.Antiforgery;

namespace NexusField.API.Controllers;

[ApiController]
[Route("api/v1/auth")]
public class AuthController(
    AuthService authService,
    JwtProvider jwtProvider,
    IValidator<LoginRequestDto> loginValidator) : ControllerBase
{
    [HttpGet("csrf")]
    public IActionResult GetCsrfToken([FromServices] IAntiforgery antiforgery)
    {
        // Genera los tokens. Inyecta la cookie automáticamente.
        var tokens = antiforgery.GetAndStoreTokens(HttpContext);

        // Devuelve el token que el frontend necesita poner en el Header
        return Ok(new { token = tokens.RequestToken });
    }
    [HttpPost("login")]
    [RequireAntiforgeryToken] // CORRECCIÓN: Atributo moderno para APIs
    public async Task<IActionResult> Login([FromBody] LoginRequestDto request)
    {
        // Lanza ValidationException si falla; el GlobalExceptionMiddleware la convierte en 400.
        await loginValidator.ValidateAndThrowAsync(request);

        // Lanza CredencialesInvalidasException si falla; el middleware la convierte en 401.
        var response = await authService.LoginAsync(request);

        var token = jwtProvider.GenerarToken(response);

        Response.Cookies.Append("jwt", token, new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.None,
            Expires = DateTimeOffset.UtcNow.AddHours(8)
        });

        return Ok(response);
    }
    [HttpPost("logout")]
    [RequireAntiforgeryToken] // Protegemos el logout para que un atacante no te cierre la sesión a la fuerza
    public IActionResult Logout()
    {
        // Destruimos la cookie del JWT
        Response.Cookies.Delete("jwt", new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.None
        });

        // Opcional pero recomendado: Destruimos la cookie del CSRF
        Response.Cookies.Delete("XSRF-TOKEN", new CookieOptions
        {
            HttpOnly = false,
            Secure = true,
            SameSite = SameSiteMode.None
        });

        return Ok(new { mensaje = "Sesión cerrada exitosamente." });
    }
}
