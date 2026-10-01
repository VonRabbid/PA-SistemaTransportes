namespace SistemaTransportes.Domain.Exceptions;

public class VentaValidationException : Exception
{
    public VentaValidationException() { }

    public VentaValidationException(string message) : base(message) { }

    public VentaValidationException(string message, Exception innerException) : base(message, innerException) { }
}
