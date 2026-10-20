using FluentValidation;
using NexusField.Application.DTOs.Usuarios;

namespace NexusField.Application.Validators;

/// <summary>
/// Validador de FluentValidation para la creación de usuarios.
/// Rechaza datos malformados antes de que lleguen a la lógica de negocio.
/// </summary>
public class CrearUsuarioValidator : AbstractValidator<CrearUsuarioDto>
{
    public CrearUsuarioValidator()
    {
        RuleFor(x => x.NombreCompleto)
            .NotEmpty().WithMessage("El nombre completo es obligatorio.")
            .MaximumLength(150).WithMessage("El nombre no puede exceder 150 caracteres.");

        RuleFor(x => x.NombreUsuario)
            .NotEmpty().WithMessage("El nombre de usuario es obligatorio.")
            .MaximumLength(50).WithMessage("El nombre de usuario no puede exceder 50 caracteres.")
            .Must(username => !username.Contains(' ')).WithMessage("El nombre de usuario no puede contener espacios.");

        RuleFor(x => x.Correo)
            .NotEmpty().WithMessage("El correo es obligatorio.")
            .EmailAddress().WithMessage("El correo no tiene un formato válido.")
            .MaximumLength(150).WithMessage("El correo no puede exceder 150 caracteres.");

        RuleFor(x => x.Clave)
            .NotEmpty().WithMessage("La contraseña es obligatoria.")
            .MinimumLength(6).WithMessage("La contraseña debe tener al menos 6 caracteres.");

        RuleFor(x => x.TarifaPorHora)
            .GreaterThanOrEqualTo(0).WithMessage("La tarifa por hora no puede ser negativa.");

        RuleFor(x => x.Rol)
            .IsInEnum().WithMessage("El rol especificado no es válido.");
    }
}
