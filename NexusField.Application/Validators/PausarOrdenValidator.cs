using FluentValidation;
using NexusField.Application.DTOs.Tecnico;

namespace NexusField.Application.Validators;

public class PausarOrdenValidator : AbstractValidator<PausarOrdenDto>
{
    public PausarOrdenValidator()
    {
        RuleFor(x => x.MotivoPausa)
            .NotEmpty().WithMessage("El motivo de la pausa es obligatorio.")
            .Matches(@"^[^<>]*$").WithMessage("Caracteres no permitidos (Prevención XSS).");
    }
}
