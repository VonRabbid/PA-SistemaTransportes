namespace SistemaTransportes.Domain.Exceptions;

public class CajaNoActivaException : Exception
{
    public CajaNoActivaException() { }

    public CajaNoActivaException(string message) : base(message) { }

    public CajaNoActivaException(string message, Exception innerException) : base(message, innerException) { }
}
