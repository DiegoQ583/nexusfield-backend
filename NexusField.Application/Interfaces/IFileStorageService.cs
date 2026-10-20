using Microsoft.AspNetCore.Http;

namespace NexusField.Application.Interfaces;

/// <summary>
/// Contrato para el servicio de almacenamiento de archivos.
/// Abstrae el sistema de archivos para que la capa Application
/// no dependa de IWebHostEnvironment ni de rutas físicas.
/// </summary>
public interface IFileStorageService
{
    /// <summary>
    /// Guarda un archivo en la carpeta indicada y retorna la ruta relativa (ej. "/images/repuestos/guid.jpg").
    /// </summary>
    Task<string> GuardarArchivoAsync(IFormFile archivo, string carpeta);

    /// <summary>
    /// Guarda un archivo a partir de un string en Base64. Retorna la ruta relativa.
    /// Útil para clientes móviles (PWA Offline) que no pueden enviar multipart/form-data.
    /// </summary>
    Task<string> GuardarBase64Async(string base64String, string carpeta, string extension);

    /// <summary>
    /// Borra un archivo del sistema de archivos.
    /// No lanza excepción si el archivo no existe.
    /// </summary>
    void BorrarArchivo(string ruta);
}
