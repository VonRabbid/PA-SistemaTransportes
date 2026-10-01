namespace SistemaTransportes.Domain.Exceptions;

public class AsientoNoDisponibleException : Exception
{
    public AsientoNoDisponibleException() { }

    public AsientoNoDisponibleException(string message) : base(message) { }

    public AsientoNoDisponibleException(string message, Exception innerException) : base(message, innerException) { }
}
