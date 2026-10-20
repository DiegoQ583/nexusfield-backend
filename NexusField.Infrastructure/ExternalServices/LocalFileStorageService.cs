using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using NexusField.Application.Interfaces;

namespace NexusField.Infrastructure.ExternalServices;

/// <summary>
/// Implementación local del servicio de almacenamiento de archivos.
/// Guarda archivos físicos en wwwroot del servidor.
/// En producción, podría reemplazarse por Azure Blob Storage o S3 sin tocar Application.
/// </summary>
public class LocalFileStorageService(IWebHostEnvironment env) : IFileStorageService
{
    /// <summary>
    /// Guarda un archivo en wwwroot/{carpeta} con nombre único (GUID + extensión original).
    /// Crea la carpeta si no existe. Retorna la ruta relativa para servir desde UseStaticFiles.
    /// </summary>
    public async Task<string> GuardarArchivoAsync(IFormFile archivo, string carpeta)
    {
        // Construir ruta física: wwwroot/images/repuestos/
        var carpetaFisica = Path.Combine(env.WebRootPath, carpeta);

        if (!Directory.Exists(carpetaFisica))
        {
            Directory.CreateDirectory(carpetaFisica);
        }

        // Nombre único para evitar colisiones: guid + extensión original.
        var extension = Path.GetExtension(archivo.FileName);
        var nombreArchivo = $"{Guid.NewGuid()}{extension}";
        var rutaCompleta = Path.Combine(carpetaFisica, nombreArchivo);

        // Escribir el archivo al disco.
        await using var stream = new FileStream(rutaCompleta, FileMode.Create);
        await archivo.CopyToAsync(stream);

        // Retornar ruta relativa para URLs (ej. "/images/repuestos/guid.jpg").
        return $"/{carpeta}/{nombreArchivo}";
    }

    /// <summary>
    /// Borra un archivo del disco dado su ruta relativa.
    /// Silencioso si el archivo no existe (idempotente).
    /// </summary>
    public void BorrarArchivo(string ruta)
    {
        // La ruta llega como "/images/repuestos/guid.jpg", quitamos el "/" inicial.
        var rutaRelativa = ruta.TrimStart('/');
        var rutaCompleta = Path.Combine(env.WebRootPath, rutaRelativa);

        if (File.Exists(rutaCompleta))
        {
            File.Delete(rutaCompleta);
        }
    }

    /// <summary>
    /// Guarda un archivo a partir de un string Base64.
    /// Limpia automáticamente prefijos como "data:image/png;base64," si existen.
    /// </summary>
    public async Task<string> GuardarBase64Async(string base64String, string carpeta, string extension)
    {
        if (string.IsNullOrWhiteSpace(base64String))
        {
            throw new ArgumentException("El string Base64 no puede estar vacío.", nameof(base64String));
        }

        try
        {
            // 1. Limpiar prefijo data:image/xxx;base64,
            var dataLimpia = base64String;
            var commaIndex = base64String.IndexOf(',');
            if (commaIndex > -1)
            {
                dataLimpia = base64String[(commaIndex + 1)..];
            }

            // 2. Convertir a bytes
            var bytes = Convert.FromBase64String(dataLimpia);

            // 3. Crear directorio si no existe
            var carpetaFisica = Path.Combine(env.WebRootPath, carpeta);
            if (!Directory.Exists(carpetaFisica))
            {
                Directory.CreateDirectory(carpetaFisica);
            }

            // 4. Generar nombre único
            var nombreArchivo = $"{Guid.NewGuid()}{extension}";
            var rutaCompleta = Path.Combine(carpetaFisica, nombreArchivo);

            // 5. Escribir bytes al disco
            await File.WriteAllBytesAsync(rutaCompleta, bytes);

            // 6. Retornar ruta relativa
            return $"/{carpeta}/{nombreArchivo}";
        }
        catch (FormatException ex)
        {
            throw new ArgumentException("El formato del string Base64 es inválido.", nameof(base64String), ex);
        }
    }
}
