namespace NexusField.Domain.Exceptions;

public class CredencialesInvalidasException : Exception
{
    public CredencialesInvalidasException()
        : base("Usuario o contraseña incorrectos.")
    {
    }
}
