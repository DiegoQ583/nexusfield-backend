using FluentValidation;
using NexusField.Application.DTOs.Ordenes;

namespace NexusField.Application.Validators;

/// <summary>
/// Validador de entrada para la creación de órdenes de trabajo.
/// Previene XSS rechazando caracteres &lt; y &gt; en campos de texto libre.
/// </summary>
public class CrearOrdenValidator : AbstractValidator<CrearOrdenDto>
{
    /// <summary>
    /// Regex que rechaza etiquetas HTML: prohíbe los caracteres &lt; y &gt;
    /// para prevenir ataques XSS (Cross-Site Scripting).
    /// </summary>
    private const string AntiXssPattern = @"^[^<>]*$";
    private const string AntiXssMessage = "Caracteres no permitidos (Prevención XSS).";

    public CrearOrdenValidator()
    {
        RuleFor(x => x.Titulo)
            .NotEmpty().WithMessage("El título es obligatorio.")
            .MaximumLength(200).WithMessage("El título no puede exceder 200 caracteres.")
            .Matches(AntiXssPattern).WithMessage(AntiXssMessage);

        RuleFor(x => x.Descripcion)
            .NotEmpty().WithMessage("La descripción es obligatoria.")
            .MaximumLength(1000).WithMessage("La descripción no puede exceder 1000 caracteres.")
            .Matches(AntiXssPattern).WithMessage(AntiXssMessage);

        RuleFor(x => x.ActivoId)
            .GreaterThan(0).WithMessage("Debe seleccionar un activo válido.");

        RuleFor(x => x.TecnicoId)
            .GreaterThan(0).WithMessage("Debe seleccionar un técnico válido.");

        RuleFor(x => x.Prioridad)
            .IsInEnum().WithMessage("La prioridad seleccionada no es válida.");
    }
}
