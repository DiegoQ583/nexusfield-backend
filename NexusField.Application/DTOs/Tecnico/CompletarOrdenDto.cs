using Microsoft.AspNetCore.Http;

namespace NexusField.Application.DTOs.Tecnico;

public record CompletarOrdenDto(
    int OrdenId,
    decimal HorasTrabajadas,
    string NotasTecnico,
    List<IFormFile>? Fotos, // <-- AHORA ES UNA LISTA DE ARCHIVOS
    IFormFile? Firma,
    List<RepuestoUsadoDto> Repuestos);