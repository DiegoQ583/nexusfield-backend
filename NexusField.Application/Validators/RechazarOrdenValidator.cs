using FluentValidation;
using NexusField.Application.DTOs.Tecnico;

namespace NexusField.Application.Validators;

public class RechazarOrdenValidator : AbstractValidator<RechazarOrdenDto>
{
    public RechazarOrdenValidator()
    {
        RuleFor(x => x.MotivoRechazo)
            .NotEmpty().WithMessage("El motivo de rechazo es obligatorio.")
            .Matches(@"^[^<>]*$").WithMessage("Caracteres no permitidos (Prevención XSS).");
    }
}
