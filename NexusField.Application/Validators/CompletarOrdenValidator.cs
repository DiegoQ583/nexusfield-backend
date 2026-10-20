using FluentValidation;
using NexusField.Application.DTOs.Tecnico;

namespace NexusField.Application.Validators;

public class CompletarOrdenValidator : AbstractValidator<CompletarOrdenDto>
{
    public CompletarOrdenValidator()
    {
        RuleFor(x => x.OrdenId)
            .GreaterThan(0).WithMessage("El Id de la orden debe ser mayor a 0");

        RuleFor(x => x.HorasTrabajadas)
            .GreaterThan(0).WithMessage("Las horas trabajadas deben ser mayores a 0");

        RuleFor(x => x.NotasTecnico)
            .NotEmpty().WithMessage("Las notas del técnico son obligatorias.")
            .Matches(@"^[^<>]*$").WithMessage("Caracteres no permitidos (Prevención XSS)");

        RuleFor(x => x.Repuestos)
            .NotNull().WithMessage("La lista de repuestos no puede ser nula");

        RuleForEach(x => x.Repuestos).ChildRules(repuesto =>
        {
            repuesto.RuleFor(r => r.RepuestoId)
                .GreaterThan(0).WithMessage("El Id del repuesto debe ser mayor a 0");
            
            repuesto.RuleFor(r => r.Cantidad)
                .GreaterThan(0).WithMessage("La cantidad utilizada debe ser mayor a 0");
        });
        RuleFor(x => x.Fotos)
            .Must(f => f == null || f.Count <= 5)
            .WithMessage("No se pueden subir más de 5 fotografías de evidencia");
    }
}
