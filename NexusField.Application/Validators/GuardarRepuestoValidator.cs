using FluentValidation;
using NexusField.Application.DTOs.Inventario;

namespace NexusField.Application.Validators;

/// <summary>
/// Validador de entrada para crear/actualizar repuestos.
/// Previene XSS en el nombre y valida restricciones de negocio (precio > 0, stock >= 0).
/// </summary>
public class GuardarRepuestoValidator : AbstractValidator<GuardarRepuestoDto>
{
    private const string AntiXssPattern = @"^[^<>]*$";
    private const string AntiXssMessage = "Caracteres no permitidos (Prevención XSS).";

    public GuardarRepuestoValidator()
    {
        RuleFor(x => x.Nombre)
            .NotEmpty().WithMessage("El nombre del repuesto es obligatorio.")
            .MaximumLength(200).WithMessage("El nombre no puede exceder 200 caracteres.")
            .Matches(AntiXssPattern).WithMessage(AntiXssMessage);

        RuleFor(x => x.Descripcion)
            .MaximumLength(500).WithMessage("La descripción no puede exceder 500 caracteres.")
            .Matches(AntiXssPattern).WithMessage(AntiXssMessage)
            .When(x => x.Descripcion is not null);

        RuleFor(x => x.Precio)
            .GreaterThan(0).WithMessage("El precio debe ser mayor a 0.");

        RuleFor(x => x.StockTotal)
            .GreaterThanOrEqualTo(0).WithMessage("El stock total no puede ser negativo.");
    }
}
