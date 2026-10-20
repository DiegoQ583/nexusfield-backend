using FluentValidation;
using NexusField.Application.DTOs.Auth;

namespace NexusField.Application.Validators;

public class LoginRequestValidator : AbstractValidator<LoginRequestDto>
{
    public LoginRequestValidator()
    {
        RuleFor(x => x.NombreUsuario)
            .NotEmpty().WithMessage("El nombre de usuario es obligatorio.");

        RuleFor(x => x.Clave)
            .NotEmpty().WithMessage("La contraseña es obligatoria.");
    }
}
