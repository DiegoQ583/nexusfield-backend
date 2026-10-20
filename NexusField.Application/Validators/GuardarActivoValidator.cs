using FluentValidation;
using NexusField.Application.DTOs.Activos;

namespace NexusField.Application.Validators;

public class GuardarActivoValidator : AbstractValidator<GuardarActivoDto>
{
    private const string AntiXssPattern = @"^[^<>]*$";
    private const string AntiXssMessage = "Caracteres no permitidos (Prevención XSS).";

    public GuardarActivoValidator()
    {
        RuleFor(x => x.Nombre)
            .NotEmpty().WithMessage("El nombre es obligatorio.")
            .Matches(AntiXssPattern).WithMessage(AntiXssMessage);

        RuleFor(x => x.Descripcion)
            .Matches(AntiXssPattern).WithMessage(AntiXssMessage)
            .When(x => x.Descripcion is not null);

        RuleFor(x => x.Ubicacion)
            .NotEmpty().WithMessage("La ubicación es obligatoria.")
            .Matches(AntiXssPattern).WithMessage(AntiXssMessage);

        RuleFor(x => x.ClienteActual)
            .NotEmpty().WithMessage("El cliente actual es obligatorio.")
            .Matches(AntiXssPattern).WithMessage(AntiXssMessage);
    }
}
