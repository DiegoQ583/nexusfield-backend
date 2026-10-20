using Microsoft.AspNetCore.Http;

namespace NexusField.Application.DTOs.Inventario;

/// DTO de entrada para crear o actualizar un repuesto.
/// Se recibe como multipart/form-data para soportar subida de archivos (IFormFile).

public record GuardarRepuestoDto(
    string Nombre, 
    string? Descripcion, 
    decimal Precio, 
    int StockTotal, 
    IFormFile? Foto,
    bool EliminarFoto = false); // <-- NUEVA PROPIEDAD
