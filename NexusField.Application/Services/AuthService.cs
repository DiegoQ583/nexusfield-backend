using Microsoft.Extensions.Caching.Memory;
using NexusField.Application.DTOs.Auth;
using NexusField.Application.Interfaces;
using NexusField.Domain.Exceptions;

namespace NexusField.Application.Services;

public class AuthService(IUsuarioRepository usuarioRepository, IMemoryCache cache)
{
    public async Task<LoginResponseDto> LoginAsync(LoginRequestDto request)
    {
        string cacheKeyLock = $"lock_{request.NombreUsuario}";
        string cacheKeyAttempts = $"attempts_{request.NombreUsuario}";

        // 1. Early Return: Verificar si el usuario está bloqueado actualmente
        if (cache.TryGetValue(cacheKeyLock, out _))
        {
            throw new CuentaBloqueadaException("Cuenta bloqueada temporalmente. Intente nuevamente en 1 minuto.");
        }

        var usuario = await usuarioRepository.ObtenerPorNombreUsuarioAsync(request.NombreUsuario);

        // 2. Validar credenciales
        if (usuario is null || !usuario.Activo || !BCrypt.Net.BCrypt.Verify(request.Clave, usuario.PasswordHash))
        {
            // Obtener intentos actuales (si no existe, empieza en 0) y sumar 1
            var intentos = cache.GetOrCreate(cacheKeyAttempts, entry => {
                entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5); // Limpia la memoria si el atacante se rinde
                return 0;
            }) + 1;

            cache.Set(cacheKeyAttempts, intentos);

            // Si llega a 5, bloquear por 1 minuto y reiniciar el contador
            if (intentos >= 6)
            {
                cache.Set(cacheKeyLock, true, TimeSpan.FromMinutes(1));
                cache.Remove(cacheKeyAttempts);
                throw new CuentaBloqueadaException("Ha superado el límite de 5 intentos. Por favor espere 1 minuto para intentarlo de nuevo");
            }

            throw new CredencialesInvalidasException();
        }

        // 3. Login Exitoso: Limpiar el historial de fallos
        cache.Remove(cacheKeyAttempts);

        return new LoginResponseDto(usuario.Id, usuario.NombreCompleto, usuario.NombreUsuario, usuario.Rol.ToString());
    }
}